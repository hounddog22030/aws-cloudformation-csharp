using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.DirectoryService
{
    public class SimpleActiveDirectory : ActiveDirectoryBase
    {
        public SimpleActiveDirectory(object name, object password, DirectorySize size, Vpc vpc, params Subnet[] subnets) : base(ResourceType.AwsDirectoryServiceSimpleAd, name, password, vpc, subnets)
        {
            Size = size;
        }

        [JsonIgnore]
        public DirectorySize Size
        {
            get
            {
                return this.Properties.GetValue<DirectorySize>();
            }
            private set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public override string AdministratorAccountName => "administrator";

        public override void AddUser(LaunchConfiguration instance, string ou, ReferenceProperty user, string password)
        {
            AddUser(instance, ou, (object)user, (object)password);
        }

        public override void AddUser(LaunchConfiguration instance, string ou, ReferenceProperty user, ReferenceProperty password)
        {
            AddUser(instance,ou,(object)user,(object)password);
        }

        private void AddUser(LaunchConfiguration instance, string ou, object user, object password)
        {
            var configSet = instance.Metadata.Init.ConfigSets.GetConfigSet(ActiveDirectoryConfigSet);

            var createDevOuConfig = configSet.GetConfig(ActiveDirectoryConfig);

            const string createUserPath = "c:/cfn/scripts/create-user.ps1";
            const string pstools = "c:/cfn/tools/pstools";

            if (!createDevOuConfig.Sources.ContainsKey(pstools))
            {
                createDevOuConfig.Sources.Add("c:/cfn/tools/pstools", "https://s3.amazonaws.com/gtbb/software/pstools.zip");
            }


            createDevOuConfig.Files.GetFile(createUserPath).Source = "https://s3.amazonaws.com/gtbb/create-user.ps1";

            ConfigCommand command = null;

            if (!createDevOuConfig.Commands.ContainsKey("InstallActiveDirectoryTools"))
            {
                command = createDevOuConfig.Commands.AddCommand<Command>("InstallActiveDirectoryTools");
                command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None, "Add-WindowsFeature RSAT-AD-PowerShell,RSAT-AD-AdminCenter");
                command.WaitAfterCompletion = 0.ToString();
            }

            var adminUserNameFqdn = new FnJoin(FnJoinDelimiter.None,
                new ReferenceProperty(MicrosoftAd.DomainAdminUsernameParameterName),
                "@",
                new ReferenceProperty(MicrosoftAd.DomainFqdnParameterName));

            command = createDevOuConfig.Commands.AddCommand<Command>(ResourceBase.NormalizeLogicalId($"AddUser{user}"));

            command.Command = new FnJoinPsExecPowershell(
                new FnJoin(FnJoinDelimiter.None, 
                    new ReferenceProperty(ActiveDirectoryBase.DomainNetBiosNameParameterName),
                    "\\", 
                    new ReferenceProperty(ActiveDirectoryBase.DomainAdminUsernameParameterName)),
                new ReferenceProperty(ActiveDirectoryBase.DomainAdminPasswordParameterName),
                createUserPath,
                " ",
                user,
                new FnJoin(FnJoinDelimiter.None,"\"",password,"\""));

            command.WaitAfterCompletion = 0.ToString();

        }
    }
}
