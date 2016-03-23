using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DirectoryService;
using Amazon.DirectoryService.Model;
using Amazon.EC2;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.DirectoryService
{
    public abstract class ActiveDirectoryBase : ResourceBase
    {
        public const string DomainVersionParameterName = "DomainVersion";
        public const string DomainAdminUsernameParameterName = "DomainAdminUsername";
        public const string DomainAdminPasswordParameterName = "DomainAdminPassword";
        public const string DomainNetBiosNameParameterName = "DomainNetBiosName";
        public const string DomainFqdnParameterName = "DomainFqdn";
        public const string DomainTopLevelParameterName = "DomainTopLevel";

        protected ActiveDirectoryBase(ResourceType type, object name, object password, Vpc vpc, params Subnet[] subnets) : base(type)
        {
            Name = name;
            Password = password;
            VpcSettings = new VpcSettings(vpc, subnets);
        }

        protected override bool SupportsTags => false;

        [JsonIgnore]
        public string AdministratorAccountName { get; protected set; }

        [JsonIgnore]
        public VpcSettings VpcSettings
        {
            get
            {
                return this.Properties.GetValue<VpcSettings>();
            }
            private set { this.Properties.SetValue(value); }
        }
        [JsonIgnore]
        public object Name
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            private set { this.Properties.SetValue(value); }
        }
        [JsonIgnore]
        public object Password
        {
            get
            {
                return this.Properties.GetValue<string>();
            }
            private set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public object ShortName
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            set { this.Properties.SetValue(value); }
        }

        public const string CidrPrimeDmz1SubnetParameterName = "CidrPrimeDmz1Subnet";

        public static void AddInstanceToDomain(Config config)
        {

            const string CheckForDomainPsPath = "c:/cfn/scripts/check-for-domain.ps1";


            var checkForDomainPs = config.Files.GetFile(CheckForDomainPsPath);
            checkForDomainPs.Source = "https://s3.amazonaws.com/gtbb/check-for-domain.ps1";

            var joinCommand = config.Commands.AddCommand<Command>("JoinDomain");


            joinCommand.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None,
                "-Command \"",
                "Add-Computer -DomainName ",
                new ReferenceProperty(ActiveDirectoryBase.DomainFqdnParameterName),
                " -Credential (New-Object System.Management.Automation.PSCredential('",
                new ReferenceProperty(ActiveDirectoryBase.DomainAdminUsernameParameterName),
                "@",
                new ReferenceProperty(ActiveDirectoryBase.DomainFqdnParameterName),
                "',(ConvertTo-SecureString \"",
                new ReferenceProperty(ActiveDirectoryBase.DomainAdminPasswordParameterName),
                "\" -AsPlainText -Force))) ",
                "-Restart\"");
            joinCommand.WaitAfterCompletion = "forever";
            joinCommand.Test = $"powershell.exe -ExecutionPolicy RemoteSigned {CheckForDomainPsPath}";

            var register = config.Commands.AddCommand<Command>("RegisterDns");
            register.Command = new FnJoinPowershellCommand("Register-DnsClient");
            register.WaitAfterCompletion = 0.ToString();

        }

        public const string ActiveDirectoryConfigSet = "ActiveDirectoryConfigSet";
        public const string ActiveDirectoryConfig = "ActiveDirectoryConfig";

        public static string AddOu(LaunchConfiguration instance, string parentOu, string ouToAdd)
        {
            var configSet = instance.Metadata.Init.ConfigSets.GetConfigSet(ActiveDirectoryConfigSet);
            var createDevOuConfig = configSet.GetConfig(ActiveDirectoryConfig);

            var command = createDevOuConfig.Commands.AddCommand();
            command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None, "Add-WindowsFeature RSAT-AD-PowerShell,RSAT-AD-AdminCenter");
            command.WaitAfterCompletion = 0.ToString();

            var adminUserNameFqdn = new FnJoin(FnJoinDelimiter.None,
                new ReferenceProperty(ActiveDirectoryBase.DomainAdminUsernameParameterName),
                "@",
                new ReferenceProperty(ActiveDirectoryBase.DomainFqdnParameterName));



            command = createDevOuConfig.Commands.AddCommand<Command>(ResourceBase.NormalizeLogicalId($"AddOu{ouToAdd}"));
            command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None,
                                                            "New-ADOrganizationalUnit -Name '",
                                                            ouToAdd,
                                                            "' -Path '",
                                                            parentOu,
                                                            "' -Credential (New-Object System.Management.Automation.PSCredential('",
                                                            adminUserNameFqdn,
                                                            "',(ConvertTo-SecureString '",
                                                            new ReferenceProperty(ActiveDirectoryBase.DomainAdminPasswordParameterName),
                                                            "' -AsPlainText -Force)))");

            var finalOu = $"OU={ouToAdd},{parentOu}";

            command.Test = new FnJoinPowershellCommand(FnJoinDelimiter.None, "if([ADSI]::Exists('LDAP://",
                                                            finalOu,
                                                            "')) { EXIT 1 }");
            command.WaitAfterCompletion = 0.ToString();

            return finalOu;
        }

        public abstract void AddUser(LaunchConfiguration instance, string ou, ReferenceProperty user, string password);
        public abstract void AddUser(LaunchConfiguration instanceRdp, string ou, ReferenceProperty user, ReferenceProperty password);

        public override string LogicalId {
            get
            {
                if (base.LogicalId == null)
                {
                    this.LogicalId = $"ActiveDirectory{this.Name}";
                }
                return base.LogicalId;
            }
            internal set { base.LogicalId = value; }
        }
    }
}
