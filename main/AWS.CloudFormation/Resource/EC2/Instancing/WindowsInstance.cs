using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Configuration.Packages;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Wait;
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
        public const int NetBiosMaxLength = 15;


        
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
            this.AddBlockDeviceMapping("/dev/sda1", volumeSize, volumeType);
        }


        public WindowsInstance(Template template, string name, InstanceTypes instanceType, string imageId, Subnet subnet, bool rename, DefinitionType definitionType)
            : this(template, name, instanceType, imageId, rename, definitionType)
        {
            if (subnet != null)
            {
                this.Subnet = subnet;
            }

        }
        public WindowsInstance(Template template, string name, InstanceTypes instanceType, string imageId, Subnet subnet, bool rename)
            : this(template, name, instanceType, imageId, subnet, rename, DefinitionType.Instance)
        {
        }

        public WindowsInstance(Template template, string name, InstanceTypes instanceType, string imageId, bool rename,
            DefinitionType definitionType): base(template, name, instanceType, imageId, OperatingSystem.Windows, true, definitionType)
        {
            if (name.Length > NetBiosMaxLength)
            {
                throw new InvalidOperationException($"Name length is limited to {NetBiosMaxLength} characters.");
            }
            if (rename)
            {
                this.Rename();
            }

            this.DisableFirewall();
        }

        public WindowsInstance(Template template, string name, InstanceTypes instanceType, string imageId, bool rename)
            : this(template, name, instanceType, imageId, rename, DefinitionType.Instance)
        {
        }

        private void DisableFirewall()
        {
                var setup = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("setup");
                var disableFirewallCommand = setup.Commands.AddCommand<Command>("a-disable-win-fw");
                disableFirewallCommand.WaitAfterCompletion = 0.ToString();
                disableFirewallCommand.Command = "powershell.exe -Command \"Get-NetFirewallProfile | Set-NetFirewallProfile -Enabled False\"";
        }



        private void Rename()
        {
            if (OperatingSystem == OperatingSystem.Windows)
            {
                var renameConfig = this.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName).GetConfig(DefaultConfigSetRenameConfig);
                var renameCommandConfig = renameConfig.Commands.AddCommand<Command>(DefaultConfigSetRenameConfigRenamePowerShellCommand);
                renameCommandConfig.Command = new PowershellFnJoin($"\"Rename-Computer -NewName {this.LogicalId.ToUpper()} -Restart\"");
                renameCommandConfig.WaitAfterCompletion = "forever";
                renameCommandConfig.Test =
                    $"if \"%COMPUTERNAME%\"==\"{this.LogicalId.ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";
            }
        }

        protected internal virtual void OnAddedToDomain(string domainName)
        {

            var nodeJson = this.GetChefNodeJsonContent();
            nodeJson.Add("domain", domainName);
        }

        private void AddChrome()
        {
            //https://s3.amazonaws.com/gtbb/googlechromestandaloneenterprise.msi
            var config =  this.Metadata.Init.ConfigSets.GetConfigSet("chrome").GetConfig("chrome");
            config.Packages.AddPackage("msi", "chrome", "https://s3.amazonaws.com/gtbb/googlechromestandaloneenterprise.msi");

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

        public WaitCondition AddChefExec(string s3bucketName, string cookbookFileName,string recipeList)
        {
            var chefConfig = this.GetChefConfig(s3bucketName, cookbookFileName);
            var chefCommandConfig = chefConfig.Commands.AddCommand<Command>(recipeList.Replace(':','-'));
            throw new NotImplementedException();
            //chefCommandConfig.Command.SetFnJoin($"C:/opscode/chef/bin/chef-client.bat -z -o {recipeList} -c c:/chef/{cookbookFileName}/client.rb");
            //WaitCondition chefComplete = new WaitCondition(this.Template,
            //    $"waitCondition{this.LogicalId}{cookbookFileName}{recipeList}".Replace(".",string.Empty).Replace(":",string.Empty),
            //    new TimeSpan(4,0,0));
            //chefConfig.Commands.AddCommand<Command>(chefComplete);
            //return chefComplete;

        }


        //public WaitCondition AddPackage(string s3BucketName, PackageBase package)
        //{
            
        //    var cookbookFileName = $"{package.CookbookName}.tar.gz";
        //    var chefComplete = this.AddChefExec(s3BucketName, cookbookFileName, package.RecipeName);
        //    BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(this, this.GetAvailableDevice());
        //    blockDeviceMapping.Ebs.SnapshotId = package.SnapshotId;
        //    this.AddBlockDeviceMapping(blockDeviceMapping);
        //    return chefComplete;
        //}
        public T AddPackage<T>() where T :PackageBase<ConfigSet>, new()
        {
            T package = new T();
            return package;
        }




    }
}
