using System.Configuration;
using AWS.CloudFormation.Instance.Metadata.Config;
using AWS.CloudFormation.Instance.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
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
        public const string DefaultConfigSetRenameConfigSetDnsServers = "a-set-dns-servers";
        public const string DefaultConfigSetRenameConfigJoinDomain = "b-join-domain";
        public const string InstallChefConfigSetName = "InstallChefConfigSet";
        public const string InstallChefConfigName = "InstallChefConfig";

        public WindowsInstance( Template template, 
                                string name, 
                                InstanceTypes instanceType, 
                                string imageId, 
                                Subnet subnet) 
            : base(template, name, instanceType, imageId, OperatingSystem.Windows, true)
        {
            this.Vpc = subnet.Vpc;
            this.Subnet = subnet;
            this.Rename();
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
                                                            this.Name,
                                                            " -Restart\"");
                renameCommandConfig.WaitAfterCompletion = "forever";
            }
        }

        protected internal virtual void OnAddedToDomain()
        {
        }

        private Config GetChefConfig(string s3bucketName, string cookbookFileName)
        {
            var chefConfig = this.Metadata.Init.ConfigSets.GetConfigSet(InstallChefConfigSetName).GetConfig(InstallChefConfigName);

            if (!chefConfig.Sources.ContainsKey("c:\\chef\\"))
            {
                var appSettingsReader = new AppSettingsReader();
                string accessKeyString = (string)appSettingsReader.GetValue("S3AccessKey", typeof(string));
                string secretKeyString = (string)appSettingsReader.GetValue("S3SecretKey", typeof(string));

                var auth = this.Metadata.Authentication.Add("S3AccessCreds", new S3Authentication(accessKeyString, secretKeyString, new string[] { s3bucketName }));
                auth.Type = "S3";
                chefConfig.Sources.Add("c:\\chef\\", $"https://{s3bucketName}.s3.amazonaws.com/{cookbookFileName}");
                chefConfig.Packages.AddPackage("msi", "chef", "https://opscode-omnibus-packages.s3.amazonaws.com/windows/2008r2/i386/chefdk-0.10.0-1-x86.msi");
                chefConfig.Files.GetFile("c:\\chef\\client.rb").Content.SetFnJoin("cache_path 'c:/chef'\ncookbook_path 'c:/chef/cookbooks'\nlocal_mode true\njson_attribs 'c:/chef/node.json'\n");

                var chefConfigContent = GetChefNodeJsonContent(s3bucketName, cookbookFileName);
                var s3FileNode = chefConfigContent.Add("s3_file");
                s3FileNode.Add("key", accessKeyString);
                s3FileNode.Add("secret", secretKeyString);

            }
            if (!chefConfig.Sources.ContainsKey("c:\\tools\\pstools\\"))
            {
                chefConfig.Sources.Add("c:\\tools\\pstools\\", "https://download.sysinternals.com/files/PSTools.zip");

            }
            return chefConfig;
        }

        public void AddChefExec(string s3bucketName, string cookbookFileName,string recipeList)
        {
            var chefConfig = this.GetChefConfig(s3bucketName, cookbookFileName);
            var chefCommandConfig = chefConfig.Commands.AddCommand<Command>(recipeList.Replace(':','-'));
            chefCommandConfig.Test = "IF EXIST \"C:\\Program Files\\Microsoft SQL Server\\MSSQL12.MSSQLSERVER\\MSSQL\\Binn\\sqlservr.exe\" EXIT 1";
            chefCommandConfig.Command.SetFnJoin($"C:\\opscode\\chefdk\\bin\\chef-client.bat --runlist 'recipe[{recipeList}]'");
        }

        public ConfigFileContent GetChefNodeJsonContent(string s3bucketName, string cookbookFileName)
        {
            var chefConfig = this.GetChefConfig(s3bucketName, cookbookFileName);
            var nodeJson = chefConfig.Files.GetFile("c:\\chef\\node.json");
            return nodeJson.Content;
        }
    }
}
