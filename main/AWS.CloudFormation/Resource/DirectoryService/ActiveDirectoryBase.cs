﻿using System;
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

        public abstract string AdministratorAccountName { get; }

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
                new ReferenceProperty(MicrosoftAd.DomainFqdnParameterName),
                " -Credential (New-Object System.Management.Automation.PSCredential('",
                new ReferenceProperty(MicrosoftAd.DomainAdminUsernameParameterName),
                "@",
                new ReferenceProperty(MicrosoftAd.DomainFqdnParameterName),
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
                new ReferenceProperty(MicrosoftAd.DomainFqdnParameterName));



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

            command.Test = new FnJoinPowershellCommand(FnJoinDelimiter.None, "if([ADSI]::Exists('LDAP://",
                                                            finalOu,
                                                            "')) { EXIT 1 }");
            command.WaitAfterCompletion = 0.ToString();

            return finalOu;
        }

        //public abstract void AddUser(LaunchConfiguration instance, object ou, object user, object password);
        public abstract void AddUser(LaunchConfiguration instance, string ou, ReferenceProperty user, string password);
        public abstract void AddUser(LaunchConfiguration instanceRdp, string ou, ReferenceProperty user, ReferenceProperty password);


        //public static string AddUser(LaunchConfiguration instance, object ou, object user, object password)
        //{
        //    var configSet = instance.Metadata.Init.ConfigSets.GetConfigSet(ActiveDirectoryConfigSet);
        //    var createDevOuConfig = configSet.GetConfig(ActiveDirectoryConfig);

        //    ConfigCommand command = null;
        //    if (!createDevOuConfig.Commands.ContainsKey("InstallActiveDirectoryTools"))
        //    {
        //        command = createDevOuConfig.Commands.AddCommand<Command>("InstallActiveDirectoryTools");
        //        command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None, "Add-WindowsFeature RSAT-AD-PowerShell,RSAT-AD-AdminCenter");
        //        command.WaitAfterCompletion = 0.ToString();
        //    }

        //    var adminUserNameFqdn = new FnJoin(FnJoinDelimiter.None,
        //        new ReferenceProperty(MicrosoftAd.DomainAdminUsernameParameterName),
        //        "@",
        //        new ReferenceProperty(MicrosoftAd.DomainFqdnParameterName));


        //    var addUserCommand = "New-ADUser";

        //    command = createDevOuConfig.Commands.AddCommand<Command>(ResourceBase.NormalizeLogicalId($"AddUser{user}"));
        //    command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None,
        //                                                    addUserCommand,
        //                                                    " -Name ",
        //                                                    user,
        //                                                    " -Path '",
        //                                                    ou,
        //                                                    "' -Credential (New-Object System.Management.Automation.PSCredential('",
        //                                                    adminUserNameFqdn,
        //                                                    "',(ConvertTo-SecureString '",
        //                                                    new ReferenceProperty(MicrosoftAd.DomainAdminPasswordParameterName),
        //                                                    "' -AsPlainText -Force)))",
        //                                                    " -SamAccountName ",
        //                                                    user,
        //                                                    " -AccountPassword (ConvertTo-SecureString -AsPlainText '",
        //                                                    password,
        //                                                    "' -Force)",
        //                                                    " -Enabled $true");
        //    command.Test = new FnJoinPowershellCommand(FnJoinDelimiter.Space,
        //                                                "try {Get-ADUser -Identity",
        //                                                user,
        //                                                ";exit 1} catch {exit 0}");
        //    command.WaitAfterCompletion = 0.ToString();

        //    //var finalOu = $"OU={ouToAdd},{parentOu}";

        //    //command.Test = new FnJoinPowershellCommand(FnJoinDelimiter.None, "if([ADSI]::Exists('LDAP://",
        //    //                                                finalOu,
        //    //                                                "')) { EXIT 1 }");
        //    //command.WaitAfterCompletion = 0.ToString();

        //    //return finalOu;
        //    return null;

        //}

        //public static string AddUser(LaunchConfiguration instance, string ou, ReferenceProperty user, string password)
        //{
        //    return AddUser(instance, (object)ou, (object)user, password);
        //}

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
            var response = c.DescribeDirectories(request);
            foreach (var dd in response.DirectoryDescriptions)
            {
                groupIds += dd.VpcSettings.SecurityGroupId;
            }
            return groupIds;
        }

        //public static string AddUser(Instance instanceRdp, string ou, ReferenceProperty user, ReferenceProperty password)
        //{
        //    return AddUser(instanceRdp, ou, (object)user, (object)password);
        //}

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