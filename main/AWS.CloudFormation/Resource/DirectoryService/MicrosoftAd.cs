using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Amazon.DirectoryService;
using Amazon.DirectoryService.Model;
using Amazon.EC2;
using Amazon.Runtime;
using Amazon.Util;
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


    public class MicrosoftAd : ResourceBase
    {
        public const string DomainVersionParameterName = "DomainVersion";
        public const string DomainTopLevelNameParameterName = "DomainTopLevelName";
        public const string DomainAdminUsernameParameterName = "DomainAdminUsername";
        public const string DomainAdminPasswordParameterName = "DomainAdminPassword";
        public const string DomainNetBiosNameParameterName = "DomainNetBiosName";
        public const string DomainFqdnParameterName = "DomainFqdn";

        public MicrosoftAd(object name, object password, DirectorySize size, Vpc vpc, params Subnet[] subnets) : base(ResourceType.AwsDirectoryServiceMicrosoftAd)
        {
            Name = name;
            Password = password;
            if (size != DirectorySize.None)
            {
                Size = size;
            }
            VpcSettings = new VpcSettings(vpc, subnets);
        }
        public MicrosoftAd(string name, object password, Vpc vpc, params Subnet[] subnets) : this(name,password,DirectorySize.None,vpc,subnets)
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
                            new ReferenceProperty(MicrosoftAd.DomainNetBiosNameParameterName),
                            new ReferenceProperty(MicrosoftAd.DomainTopLevelNameParameterName)),
                " -Credential (New-Object System.Management.Automation.PSCredential('",
                new ReferenceProperty(MicrosoftAd.DomainAdminUsernameParameterName),
                "@",
                new FnJoin(FnJoinDelimiter.Period,
                new ReferenceProperty(MicrosoftAd.DomainNetBiosNameParameterName),
                new ReferenceProperty(MicrosoftAd.DomainTopLevelNameParameterName)),
                "',(ConvertTo-SecureString \"",
                new ReferenceProperty(MicrosoftAd.DomainAdminPasswordParameterName),
                "\" -AsPlainText -Force))) ",
                "-Restart\"");
            joinCommand.WaitAfterCompletion = "forever";
            joinCommand.Test = $"powershell.exe -ExecutionPolicy RemoteSigned {CheckForDomainPsPath}";
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
                new ReferenceProperty(MicrosoftAd.DomainAdminUsernameParameterName),
                "@",
                new ReferenceProperty(MicrosoftAd.DomainNetBiosNameParameterName),
                ".",
                new ReferenceProperty(MicrosoftAd.DomainTopLevelNameParameterName));

            

            command = createDevOuConfig.Commands.AddCommand<Command>(ResourceBase.NormalizeLogicalId($"AddOu{ouToAdd}"));
            command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None,
                                                            "New-ADOrganizationalUnit -Name '",
                                                            ouToAdd,
                                                            "' -Path '",
                                                            parentOu,
                                                            "' -Credential (New-Object System.Management.Automation.PSCredential('",
                                                            adminUserNameFqdn,
                                                            "',(ConvertTo-SecureString '",
                                                            new ReferenceProperty(MicrosoftAd.DomainAdminPasswordParameterName),
                                                            "' -AsPlainText -Force)))");

            var finalOu = $"OU={ouToAdd},{parentOu}";

            command.Test = new FnJoinPowershellCommand(     FnJoinDelimiter.None, "if([ADSI]::Exists('LDAP://",
                                                            finalOu,
                                                            "')) { EXIT 1 }");
            command.WaitAfterCompletion = 0.ToString();

            return finalOu;
        }

        public static string AddUser(LaunchConfiguration instance, object ou, object user, string password)
        {
            bool managed = false;
            var configSet = instance.Metadata.Init.ConfigSets.GetConfigSet(ActiveDirectoryConfigSet);
            var createDevOuConfig = configSet.GetConfig(ActiveDirectoryConfig);

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
                new ReferenceProperty(MicrosoftAd.DomainNetBiosNameParameterName),
                ".",
                new ReferenceProperty(MicrosoftAd.DomainTopLevelNameParameterName));


            var addUserCommand = "New-ADUser";
            if (managed)
            {
                addUserCommand = "New-ADServiceAccount";
            }

            command = createDevOuConfig.Commands.AddCommand<Command>(ResourceBase.NormalizeLogicalId($"AddUser{user}"));
            command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None,
                                                            addUserCommand,
                                                            " -Name ",
                                                            user,
                                                            " -Path '",
                                                            ou,
                                                            "' -Credential (New-Object System.Management.Automation.PSCredential('",
                                                            adminUserNameFqdn,
                                                            "',(ConvertTo-SecureString '",
                                                            new ReferenceProperty(MicrosoftAd.DomainAdminPasswordParameterName),
                                                            "' -AsPlainText -Force)))",
                                                            " -SamAccountName ",
                                                            user,
                                                            " -AccountPassword (ConvertTo-SecureString -AsPlainText '",
                                                            GetPassword(),
                                                            "' -Force)",
                                                            " -Enabled $true");
            command.Test = new FnJoinPowershellCommand(FnJoinDelimiter.Space,
                                                        "try {Get-ADUser -Identity",
                                                        user,
                                                        new ReferenceProperty(TeamFoundationServerBuildServerBase.TfsServiceAccountNameParameterName),
                                                        ";exit 1} catch {exit 0}");
            command.WaitAfterCompletion = 0.ToString();

            //var finalOu = $"OU={ouToAdd},{parentOu}";

            //command.Test = new FnJoinPowershellCommand(FnJoinDelimiter.None, "if([ADSI]::Exists('LDAP://",
            //                                                finalOu,
            //                                                "')) { EXIT 1 }");
            //command.WaitAfterCompletion = 0.ToString();

            //return finalOu;
            return null;

        }

        public static string AddUser(LaunchConfiguration instance, string ou, ReferenceProperty user, string password)
        {
            return AddUser(instance, (object)ou, (object)user, password);

        }
        public static string AddUser(LaunchConfiguration instance, string ou, string user, string password)
        {
            return AddUser(instance, (object)ou, (object)user, password);
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

        public static string GetSecurityGroupId()
        {
            AmazonEC2Client ec2Client = new AmazonEC2Client();

            var securityGroupsResponse = ec2Client.DescribeSecurityGroups();
            string groupIds = string.Empty;
            foreach (var securityGroup in securityGroupsResponse.SecurityGroups)
            {
                groupIds += securityGroup.GroupId;
            }

            AmazonDirectoryServiceClient c = new AmazonDirectoryServiceClient();
            DescribeDirectoriesRequest request = new DescribeDirectoriesRequest();
            var response =  c.DescribeDirectories(request);
            foreach (var dd in response.DirectoryDescriptions)
            {
                groupIds += dd.VpcSettings.SecurityGroupId;
            }
            return groupIds;
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
