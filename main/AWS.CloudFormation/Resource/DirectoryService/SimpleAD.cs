using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Configuration.Packages;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Serialization;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.DirectoryService
{

    [JsonConverter(typeof(EnumConverter))]
    public enum DirectorySize
    {
        None,
        [EnumMember(Value = "Small")]
        Small,
        [EnumMember(Value = "Large")]
        Large
    }


    public class SimpleAd : ResourceBase
    {
        public const string DomainVersionParameterName = "DomainVersion";
        public const string DomainTopLevelNameParameterName = "DomainTopLevelName";
        public const string DomainAdminUsernameParameterName = "DomainAdminUsername";
        public const string DomainAdminPasswordParameterName = "DomainAdminPassword";
        public const string DomainNetBiosNameParameterName = "DomainNetBiosName";
        public const string DomainFqdnParameterName = "DomainFqdn";

        public SimpleAd(object name, object password, DirectorySize size, Vpc vpc, params Subnet[] subnets) : base(ResourceType.AwsDirectoryServiceMicrosoftAd)
        {
            Name = name;
            Password = password;
            if (size != DirectorySize.None)
            {
                Size = size;
            }
            VpcSettings = new VpcSettings(vpc, subnets);
        }
        public SimpleAd(string name, object password, Vpc vpc, params Subnet[] subnets) : this(name,password,DirectorySize.None,vpc,subnets)
        {
            this.LogicalId = $"SimpleAd{name}";
        }

        protected override bool SupportsTags => false;

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
        public DirectorySize Size
        {
            get
            {
                return this.Properties.GetValue<DirectorySize>();
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

        public static void AddInstanceToDomain(Config config)
        {

            const string CheckForDomainPsPath = "c:/cfn/scripts/check-for-domain.ps1";


            var checkForDomainPs = config.Files.GetFile(CheckForDomainPsPath);
            checkForDomainPs.Source = "https://s3.amazonaws.com/gtbb/check-for-domain.ps1";

            var joinCommand = config.Commands.AddCommand<Command>("JoinDomain");


            joinCommand.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None,
                "-Command \"",
                "Add-Computer -DomainName ",
                new FnJoin(FnJoinDelimiter.Period,
                            new ReferenceProperty(SimpleAd.DomainNetBiosNameParameterName),
                            new ReferenceProperty(SimpleAd.DomainTopLevelNameParameterName)),
                " -Credential (New-Object System.Management.Automation.PSCredential('",
                new ReferenceProperty(SimpleAd.DomainAdminUsernameParameterName),
                "@",
                new FnJoin(FnJoinDelimiter.Period,
                new ReferenceProperty(SimpleAd.DomainNetBiosNameParameterName),
                new ReferenceProperty(SimpleAd.DomainTopLevelNameParameterName)),
                "',(ConvertTo-SecureString \"",
                new ReferenceProperty(SimpleAd.DomainAdminPasswordParameterName),
                "\" -AsPlainText -Force))) ",
                "-Restart\"");
            joinCommand.WaitAfterCompletion = "forever";
            joinCommand.Test = $"powershell.exe -ExecutionPolicy RemoteSigned {CheckForDomainPsPath}";
        }

        public static string AddOu(Config config, string parentOu, string ouToAdd)
        {

            var command = config.Commands.AddCommand<Command>("InstallActiveDirectoryTools");
            command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None, "Add-WindowsFeature RSAT-AD-PowerShell,RSAT-AD-AdminCenter");
            command.WaitAfterCompletion = 0.ToString();

            var adminUserNameFqdn = new FnJoin(FnJoinDelimiter.None,
                new ReferenceProperty(SimpleAd.DomainAdminUsernameParameterName),
                "@",
                new ReferenceProperty(SimpleAd.DomainNetBiosNameParameterName),
                ".",
                new ReferenceProperty(SimpleAd.DomainTopLevelNameParameterName));

            

            command = config.Commands.AddCommand<Command>(ResourceBase.NormalizeLogicalId($"AddOu{ouToAdd}"));
            command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None,
                                                            "New-ADOrganizationalUnit -Name '",
                                                            ouToAdd,
                                                            "' -Path '",
                                                            parentOu,
                                                            "' -Credential (New-Object System.Management.Automation.PSCredential('",
                                                            adminUserNameFqdn,
                                                            "',(ConvertTo-SecureString '",
                                                            new ReferenceProperty(SimpleAd.DomainAdminPasswordParameterName),
                                                            "' -AsPlainText -Force)))");

            var finalOu = $"OU={ouToAdd},{parentOu}";

            command.Test = new FnJoinPowershellCommand(     FnJoinDelimiter.None, "if([ADSI]::Exists('LDAP://",
                                                            finalOu,
                                                            "')) { EXIT 1 }");
            command.WaitAfterCompletion = 0.ToString();

            return finalOu;
        }

        public static string AddUser(Config config, string ou, string user, string password)
        {
            ConfigCommand command = null;
            if (!config.Commands.ContainsKey("InstallActiveDirectoryTools"))
            {
                command = config.Commands.AddCommand<Command>("InstallActiveDirectoryTools");
                command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None, "Add-WindowsFeature RSAT-AD-PowerShell,RSAT-AD-AdminCenter");
                command.WaitAfterCompletion = 0.ToString();
            }

            var adminUserNameFqdn = new FnJoin(FnJoinDelimiter.None,
                new ReferenceProperty(SimpleAd.DomainAdminUsernameParameterName),
                "@",
                new ReferenceProperty(SimpleAd.DomainNetBiosNameParameterName),
                ".",
                new ReferenceProperty(SimpleAd.DomainTopLevelNameParameterName));



            command = config.Commands.AddCommand<Command>(ResourceBase.NormalizeLogicalId($"AddUser{user}"));
            command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None,
                                                            "New-ADUser -Name ",
                                                            user,
                                                            " -Path '",
                                                            ou,
                                                            "' -Credential (New-Object System.Management.Automation.PSCredential('",
                                                            adminUserNameFqdn,
                                                            "',(ConvertTo-SecureString '",
                                                            new ReferenceProperty(SimpleAd.DomainAdminPasswordParameterName),
                                                            "' -AsPlainText -Force)))",
                                                            " -SamAccountName ",
                                                            user,
                                                            " -AccountPassword (ConvertTo-SecureString -AsPlainText '",
                                                            GetPassword(),
                                                            "' -Force)");

            //var finalOu = $"OU={ouToAdd},{parentOu}";

            //command.Test = new FnJoinPowershellCommand(FnJoinDelimiter.None, "if([ADSI]::Exists('LDAP://",
            //                                                finalOu,
            //                                                "')) { EXIT 1 }");
            //command.WaitAfterCompletion = 0.ToString();

            //return finalOu;
            return null;
        }
        internal static string GetPassword()
        {
            var random = new Random(((int)DateTime.Now.Ticks % int.MaxValue));

            string password = string.Empty;

            for (int i = 0; i < 4; i++)
            {
                char charToAdd = ((char)random.Next((int)'A', (int)'Z'));
                password += charToAdd;
            }

            for (int i = 0; i < 4; i++)
            {
                char charToAdd = ((char)random.Next((int)'0', (int)'9'));
                password += charToAdd;
            }

            for (int i = 0; i < 4; i++)
            {
                char charToAdd = ((char)random.Next((int)'a', (int)'z'));
                password += charToAdd;
            }
            return password;
        }
    }

    public class VpcSettings
    {
        public VpcSettings(Vpc vpc, params Subnet[] subnets)
        {
            SubnetIds = new List<object>();

            VpcId = new ReferenceProperty(vpc);

            foreach (var subnet in subnets)
            {
                this.SubnetIds.Add( new ReferenceProperty(subnet));
            }
        }

        public object VpcId { get; }
        public List<object> SubnetIds { get; }
    }
}
