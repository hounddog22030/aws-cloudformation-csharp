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
        [EnumMember(Value = "Small")]
        Small,
        [EnumMember(Value = "Large")]
        Large
    }


    public class MicrosoftAd : ActiveDirectoryBase
    {
        public MicrosoftAd(object name, object password, Vpc vpc, params Subnet[] subnets) : base(ResourceType.AwsDirectoryServiceMicrosoftAd, name, password, vpc, subnets)
        {
        }

        public override string AdministratorAccountName => "admin";
        public override void AddUser(LaunchConfiguration instance, string ou, ReferenceProperty user, string password)
        {
            throw new NotImplementedException();
        }

        public override void AddUser(LaunchConfiguration instanceRdp, string ou, ReferenceProperty user, ReferenceProperty password)
        {
            throw new NotImplementedException();
        }

        //public static void AddInstanceToDomain(Config config)
        //{

        //    const string CheckForDomainPsPath = "c:/cfn/scripts/check-for-domain.ps1";


        //    var checkForDomainPs = config.Files.GetFile(CheckForDomainPsPath);
        //    checkForDomainPs.Source = "https://s3.amazonaws.com/gtbb/check-for-domain.ps1";

        //    var joinCommand = config.Commands.AddCommand<Command>("JoinDomain");


        //    joinCommand.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None,
        //        "-Command \"",
        //        "Add-Computer -DomainName ",
        //        new ReferenceProperty(ActiveDirectoryBase.DomainFqdnParameterName),
        //        " -Credential (New-Object System.Management.Automation.PSCredential('",
        //        new ReferenceProperty(ActiveDirectoryBase.DomainAdminUsernameParameterName),
        //        "@",
        //        new ReferenceProperty(ActiveDirectoryBase.DomainFqdnParameterName),
        //        "',(ConvertTo-SecureString \"",
        //        new ReferenceProperty(ActiveDirectoryBase.DomainAdminPasswordParameterName),
        //        "\" -AsPlainText -Force))) ",
        //        "-Restart\"");
        //    joinCommand.WaitAfterCompletion = "forever";
        //    joinCommand.Test = $"powershell.exe -ExecutionPolicy RemoteSigned {CheckForDomainPsPath}";
        //}

        //public static string AddOu(LaunchConfiguration instance, string parentOu, string ouToAdd)
        //{
        //    var configSet = instance.Metadata.Init.ConfigSets.GetConfigSet(ActiveDirectoryConfigSet);
        //    var createDevOuConfig = configSet.GetConfig(ActiveDirectoryConfig);

        //    var command = createDevOuConfig.Commands.AddCommand();
        //    command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None, "Add-WindowsFeature RSAT-AD-PowerShell,RSAT-AD-AdminCenter");
        //    command.WaitAfterCompletion = 0.ToString();

        //    var adminUserNameFqdn = new FnJoin(FnJoinDelimiter.None,
        //        new ReferenceProperty(ActiveDirectoryBase.DomainAdminUsernameParameterName),
        //        "@",
        //        new ReferenceProperty(ActiveDirectoryBase.DomainFqdnParameterName));



        //    command = createDevOuConfig.Commands.AddCommand<Command>(ResourceBase.NormalizeLogicalId($"AddOu{ouToAdd}"));
        //    command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None,
        //                                                    "New-ADOrganizationalUnit -Name '",
        //                                                    ouToAdd,
        //                                                    "' -Path '",
        //                                                    parentOu,
        //                                                    "' -Credential (New-Object System.Management.Automation.PSCredential('",
        //                                                    adminUserNameFqdn,
        //                                                    "',(ConvertTo-SecureString '",
        //                                                    new ReferenceProperty(ActiveDirectoryBase.DomainAdminPasswordParameterName),
        //                                                    "' -AsPlainText -Force)))");

        //    var finalOu = $"OU={ouToAdd},{parentOu}";

        //    command.Test = new FnJoinPowershellCommand(     FnJoinDelimiter.None, "if([ADSI]::Exists('LDAP://",
        //                                                    finalOu,
        //                                                    "')) { EXIT 1 }");
        //    command.WaitAfterCompletion = 0.ToString();

        //    return finalOu;
        //}

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
        //        new ReferenceProperty(ActiveDirectoryBase.DomainAdminUsernameParameterName),
        //        "@",
        //        new ReferenceProperty(ActiveDirectoryBase.DomainFqdnParameterName));


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
        //                                                    new ReferenceProperty(ActiveDirectoryBase.DomainAdminPasswordParameterName),
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


        //public static string GetSecurityGroupId()
        //{
        //    AmazonEC2Client ec2Client = new AmazonEC2Client();

        //    var securityGroupsResponse = ec2Client.DescribeSecurityGroups();
        //    string groupIds = string.Empty;
        //    foreach (var securityGroup in securityGroupsResponse.SecurityGroups)
        //    {
        //        groupIds += securityGroup.GroupId;
        //    }

        //    AmazonDirectoryServiceClient c = new AmazonDirectoryServiceClient();
        //    DescribeDirectoriesRequest request = new DescribeDirectoriesRequest();
        //    var response =  c.DescribeDirectories(request);
        //    foreach (var dd in response.DirectoryDescriptions)
        //    {
        //        groupIds += dd.VpcSettings.SecurityGroupId;
        //    }
        //    return groupIds;
        //}

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
