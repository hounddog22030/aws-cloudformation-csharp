using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Configuration.Packages;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Instancing
{
    public class WindowsInstance : Instance
    {
        public const string DefaultConfigSetName = "config";
        public const string DefaultConfigSetRenameConfig = "rename";
        public const string DefaultConfigSetJoinConfig = "join";
        public const string DefaultConfigSetRenameConfigRenamePowerShellCommand = "1-execute-powershell-script-RenameComputer";
        public const string DefaultConfigSetRenameConfigJoinDomain = "b-join-domain";
        public const string InstallChefConfigSetName = "InstallChefConfigSet";
        public const string InstallChefConfigName = "InstallChefConfig";

        public WindowsInstance(Template template,
                                string name,
                                InstanceTypes instanceType,
                                string imageId,
                                Subnet subnet,
                                bool rename,
                                Ebs.VolumeTypes volumeType,
                                uint volumeSize)
            : this(template, name, instanceType, imageId, subnet, rename)
        {
            this.AddBlockDeviceMapping("/dev/sda1", volumeSize, volumeType.ToString());

        }

        public WindowsInstance(Template template,
                                string name,
                                InstanceTypes instanceType,
                                string imageId,
                                Subnet subnet,
                                bool rename)
            : this(template, name, instanceType, imageId, rename)
        {
            this.Subnet = subnet;
        }

        public WindowsInstance(Template template,
                                string name,
                                InstanceTypes instanceType,
                                string imageId,
                                bool rename)
            : base(template, name, instanceType, imageId, OperatingSystem.Windows, true)
        {
            var nodeJson = this.GetChefNodeJsonContent();
            nodeJson.Add("nothing", "nothing");
            //xvd[f - z]
            _availableDevices = new List<string>();
            for (char c = 'f'; c < 'z'; c++)
            {
                _availableDevices.Add($"xvd{c}");
            }


            if (rename)
            {
                this.Rename();
            }

            this.MakeIpAddressStatic();

        }

        protected void MakeIpAddressStatic()
        {
            var configSetConfig = this.Metadata.Init.ConfigSets.GetConfigSet("config");
            var setup = configSetConfig.GetConfig("setup");

            var setupFiles = setup.Files;

            setupFiles.GetFile("c:\\cfn\\scripts\\Set-StaticIP.ps1")
                .Content.SetFnJoin(
                    "$netip = Get-NetIPConfiguration;",
                    "$ipconfig = Get-NetIPAddress | ?{$_.IpAddress -eq $netip.IPv4Address.IpAddress};",
                    "Get-NetAdapter | Set-NetIPInterface -DHCP Disabled;",
                    "Get-NetAdapter | New-NetIPAddress -AddressFamily IPv4 -IPAddress $netip.IPv4Address.IpAddress -PrefixLength $ipconfig.PrefixLength -DefaultGateway $netip.IPv4DefaultGateway.NextHop;",
                    "Get-NetAdapter | Set-DnsClientServerAddress -ServerAddresses $netip.DNSServer.ServerAddresses;",
                    "\n");

            var setStaticIpCommand = configSetConfig.GetConfig("setup").Commands.AddCommand<PowerShellCommand>("a-set-static-ip");
            setStaticIpCommand.WaitAfterCompletion = 15.ToString();
            setStaticIpCommand.Command.AddCommandLine("-ExecutionPolicy RemoteSigned -Command \"c:\\cfn\\scripts\\Set-StaticIP.ps1\"");
        }

        [JsonIgnore]
        public ParameterBase DomainDnsName { get; protected internal set; }
        [JsonIgnore]
        public ParameterBase DomainNetBiosName { get; protected internal set; }


        private void Rename()
        {
            if (OperatingSystem == OperatingSystem.Windows)
            {
                var renameConfig = this.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName).GetConfig(DefaultConfigSetRenameConfig);
                var renameCommandConfig = renameConfig.Commands.AddCommand<PowerShellCommand>(DefaultConfigSetRenameConfigRenamePowerShellCommand);
                renameCommandConfig.Command.AddCommandLine("\"Rename-Computer -NewName ",
                                                            this.LogicalId,
                                                            " -Restart\"");
                renameCommandConfig.WaitAfterCompletion = "forever";
            }
        }

        protected internal virtual void OnAddedToDomain(string domainName)
        {

            var nodeJson = this.GetChefNodeJsonContent();
            nodeJson.Add("domain", domainName);
        }

        private Config GetChefConfig(string s3bucketName, string cookbookFileName)
        {
            if (!this.Metadata.Authentication.ContainsKey("S3AccessCreds"))
            {
                var appSettingsReader = new AppSettingsReader();
                string accessKeyString = (string)appSettingsReader.GetValue("S3AccessKey", typeof(string));
                string secretKeyString = (string)appSettingsReader.GetValue("S3SecretKey", typeof(string));
                var auth = this.Metadata.Authentication.Add("S3AccessCreds", new S3Authentication(accessKeyString, secretKeyString, new string[] { s3bucketName }));
                auth.Type = "S3";
                var chefConfigContent = GetChefNodeJsonContent();
                var s3FileNode = chefConfigContent.Add("s3_file");
                s3FileNode.Add("key", accessKeyString);
                s3FileNode.Add("secret", secretKeyString);
            }

            var chefConfig = this.Metadata.Init.ConfigSets.GetConfigSet(cookbookFileName).GetConfig(cookbookFileName);

            if (!chefConfig.Packages.ContainsKey("msi") || (chefConfig.Packages.ContainsKey("msi") && !((CloudFormationDictionary) chefConfig.Packages["msi"]).ContainsKey("chef")))
            {
                chefConfig.Packages.AddPackage("msi", "chef", "https://opscode-omnibus-packages.s3.amazonaws.com/windows/2012r2/i386/chef-client-12.6.0-1-x86.msi");
            }

            var sourcesKey = $"c:/chef/{cookbookFileName}/";
            if (!chefConfig.Sources.ContainsKey(sourcesKey))
            {
                chefConfig.Sources.Add(sourcesKey, $"https://{s3bucketName}.s3.amazonaws.com/{cookbookFileName}");
            }

            var clientRbFileKey = $"c:/chef/{cookbookFileName}/client.rb";
            if (!chefConfig.Files.ContainsKey(clientRbFileKey))
            {
                chefConfig.Files.GetFile(clientRbFileKey).Content.SetFnJoin($"cache_path 'c:/chef'\ncookbook_path 'c:/chef/{cookbookFileName}/cookbooks'\nlocal_mode true\njson_attribs 'c:/chef/node.json'\n");
            }

            return chefConfig;
        }

        public void AddChefExec(string s3bucketName, string cookbookFileName,string recipeList)
        {
            var chefConfig = this.GetChefConfig(s3bucketName, cookbookFileName);
            var chefCommandConfig = chefConfig.Commands.AddCommand<Command>(recipeList.Replace(':','-'));
            //chefCommandConfig.Test = "IF EXIST \"C:/Program Files/Microsoft SQL Server/MSSQL12.MSSQLSERVER/MSSQL/Binn/sqlservr.exe\" EXIT 1";
            chefCommandConfig.Command.SetFnJoin($"C:/opscode/chef/bin/chef-client.bat -z -o {recipeList} -c c:/chef/{cookbookFileName}/client.rb");
        }

        public ConfigFileContent GetChefNodeJsonContent()
        {

            var chefConfig = this.Metadata.Init.ConfigSets.GetConfigSet(InstallChefConfigSetName).GetConfig(InstallChefConfigSetName);
            var nodeJson = chefConfig.Files.GetFile("c:/chef/node.json");
            return nodeJson.Content;
        }

        public void AddPackage(string s3BucketName, PackageBase package)
        {
            var cookbookFileName = $"{package.CookbookName}.tar.gz";
            this.AddChefExec(s3BucketName, cookbookFileName, package.CookbookName);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(this, this.GetAvailableDevice());
            blockDeviceMapping.Ebs.SnapshotId = package.SnapshotId;
            this.AddBlockDeviceMapping(blockDeviceMapping);
        }

        readonly List<string> _availableDevices;

        protected string GetAvailableDevice()
        {
            var returnValue = _availableDevices.First();
            _availableDevices.Remove(returnValue);
            return returnValue;
        }

        public void AddDisk(Ebs.VolumeTypes ec2DiskType, int sizeInGigabytes)
        {
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(this,this.GetAvailableDevice());
            blockDeviceMapping.Ebs.VolumeSize = sizeInGigabytes;
            blockDeviceMapping.Ebs.VolumeType = ec2DiskType;
            this.AddBlockDeviceMapping(blockDeviceMapping);
        }
    }
}
