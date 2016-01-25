using System;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Instancing
{
    public class DomainController : WindowsInstance
    {

        public const string DefaultConfigSetRenameConfigSetDnsServers = "a-set-dns-servers";

        public class DomainInfo
        {
            public DomainInfo(string domainDnsName, string adminUserName, string adminPassword)
            {
                DomainDnsName = domainDnsName;
                DomainNetBiosName = domainDnsName.Split('.')[0];
                AdminUserName = adminUserName;
                AdminPassword = adminPassword;

            }

            public string DomainDnsName { get; }
            public string DomainNetBiosName { get; }
            public string AdminUserName { get; }
            public string AdminPassword { get; }
        }

        public const string ParameterNameDomainAdminPassword = "DomainAdminPassword";
        public const string ParameterNameDomainDnsName = "DomainDNSName";
        public const string ParameterNameDomainNetBiosName = "DomainNetBIOSName";
        public const string ParameterNameDomainAdminUser = "DomainAdminUser";



        public DomainController(Template template, string name, InstanceTypes instanceType, string imageId,
            Subnet subnet, DomainInfo domainInfo)
            : base(template, name, instanceType, imageId, subnet, true)
        {
            this.DomainDnsName = new ParameterBase(DomainController.ParameterNameDomainDnsName, "String",
                domainInfo.DomainDnsName);
            ;
            Template.AddParameter(this.DomainDnsName);

            this.DomainAdminPassword = new ParameterBase(DomainController.ParameterNameDomainAdminPassword, "String",
                domainInfo.AdminPassword);
            Template.AddParameter(DomainAdminPassword);

            this.DomainNetBiosName = new ParameterBase(DomainController.ParameterNameDomainNetBiosName, "String",
                domainInfo.DomainNetBiosName);
            template.AddParameter(this.DomainNetBiosName);

            this.DomainAdminUser = new ParameterBase(DomainController.ParameterNameDomainAdminUser, "String",
                domainInfo.AdminUserName);
            template.AddParameter(this.DomainAdminUser);

            DomainMemberSecurityGroup = this.CreateDomainMemberSecurityGroup();
            this.CreateDomainControllerSecurityGroup();
            this.MakeDomainController();
            ConfigureDefaultSite(subnet);
        }

        [JsonIgnore]
        public ParameterBase DomainAdminUser { get; }

        [JsonIgnore]
        public ParameterBase DomainAdminPassword { get; set; }

        private void MakeDomainController()
        {
            var setup = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("setup");

            var setupFiles = setup.Files;

            ConfigFile file = setupFiles.GetFile("c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1");

            file.Source = "https://s3.amazonaws.com/quickstart-reference/microsoft/activedirectory/latest/scripts/ConvertTo-EnterpriseAdmin.ps1";

            var currentConfig = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("installADDS");
            var currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("1-install-prereqsz");

            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command.AddCommandLine(
                "-Command \"Install-WindowsFeature AD-Domain-Services, rsat-adds -IncludeAllSubFeature\"");

            currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("2-install-adds");
            currentCommand.WaitAfterCompletion = "forever";

            currentCommand.Command.AddCommandLine(
                "-Command \"Install-ADDSForest -DomainName ",
                this.DomainDnsName,
                " -SafeModeAdministratorPassword (convertto-securestring jhkjhsdf338! -asplaintext -force) -DomainMode Win2012 -DomainNetbiosName ",
                this.DomainNetBiosName,
                " -ForestMode Win2012 -Confirm:$false -Force\"");


            currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("3-restart-service");
            currentCommand.WaitAfterCompletion = 20.ToString();
            currentCommand.Command.AddCommandLine(
                new object[]
                {
                    "-Command \"Restart-Service NetLogon -EA 0\""
                }
                );

            currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("4 - create - adminuser");
            currentCommand.WaitAfterCompletion = "0";
            currentCommand.Command.AddCommandLine(
                new object[]
                {
                    "-Command \"",
                    "New-ADUser ",
                    "-Name ",
                    this.DomainAdminUser,
                    " -UserPrincipalName ",
                    this.DomainAdminUser,
                    "@",
                    this.DomainDnsName,
                    " ",
                    "-AccountPassword (ConvertTo-SecureString ",
                    this.DomainAdminPassword,
                    " -AsPlainText -Force) ",
                    "-Enabled $true ",
                    "-PasswordNeverExpires $true\""
                });

            currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("5 - update - adminuser");
            currentCommand.WaitAfterCompletion = "0";
            currentCommand.Command.AddCommandLine(
                new object[]
                {
                    "-ExecutionPolicy RemoteSigned -Command \"c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1 -Members ",
                    this.DomainAdminUser,
                    "\""
                });

            this.OnAddedToDomain(this.DomainNetBiosName.Default.ToString());
        }

        public void AddReplicationSite(Subnet subnet)
        {
            var currentConfig = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("configureSites");
            string commandName = $"create-site-{subnet.LogicalId}";
            if (!currentConfig.Commands.ContainsKey(commandName))
            {
                ConfigCommand currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>(commandName);
                currentCommand.WaitAfterCompletion = 0.ToString();
                currentCommand.Command.AddCommandLine("-Command \"", $"New-ADReplicationSite  {subnet.LogicalId}\"");

                currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>($"create-subnet-{subnet.LogicalId}");
                currentCommand.WaitAfterCompletion = 0.ToString();
                currentCommand.Command.AddCommandLine("-Command New-ADReplicationSubnet -Name ", subnet.CidrBlock, $" -Site {subnet.LogicalId}");
            }
        }

        private Config ConfigureDefaultSite(Subnet defaultSubnet)
        {
            var currentConfig = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("configureSites");
            ConfigCommand currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("a-rename-default-site");
            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command.AddCommandLine(" ", "\"", $"Get-ADObject -SearchBase (Get-ADRootDSE).ConfigurationNamingContext -filter {{Name -eq 'Default-First-Site-Name'}} | Rename-ADObject -NewName {defaultSubnet.LogicalId}", "\"");

            return currentConfig;
        }

        private void CreateDomainControllerSecurityGroup()
        {
            // ReSharper disable once InconsistentNaming
            SecurityGroup DomainControllerSG1 = new SecurityGroup(Template, "DomainControllerSG1", "Domain Controller", this.Subnet.Vpc);
            DomainControllerSG1.AddIngress(this.Subnet.Vpc as ICidrBlock, Protocol.Tcp,
                Ports.WsManagementPowerShell);
            DomainControllerSG1.AddIngress(this.Subnet.Vpc as ICidrBlock, Protocol.Tcp, Ports.Http);

            DomainControllerSG1.AddIngress(DomainMemberSecurityGroup, Protocol.Udp,
                Ports.Ntp);
            DomainControllerSG1.AddIngress(DomainMemberSecurityGroup, Protocol.Tcp,
                Ports.WinsManager);
            DomainControllerSG1.AddIngress(DomainMemberSecurityGroup, Protocol.Tcp,
                Ports.ActiveDirectoryManagement);
            DomainControllerSG1.AddIngress(DomainMemberSecurityGroup, Protocol.Udp,
                Ports.NetBios);
            DomainControllerSG1.AddIngress(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.Smb);
            DomainControllerSG1.AddIngress(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.ActiveDirectoryManagement2);
            DomainControllerSG1.AddIngress(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
            DomainControllerSG1.AddIngress(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.Ldap);
            DomainControllerSG1.AddIngress(DomainMemberSecurityGroup, Protocol.Tcp,
                Ports.Ldaps);
            DomainControllerSG1.AddIngress(DomainMemberSecurityGroup, Protocol.Tcp,
                Ports.Ldap2Begin, Ports.Ldap2End);
            DomainControllerSG1.AddIngress(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.DnsQuery);
            DomainControllerSG1.AddIngress(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.KerberosKeyDistribution);
            DomainControllerSG1.AddIngress(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.RemoteDesktopProtocol);

            this.AddSecurityGroup(DomainControllerSG1);

        }

        private SecurityGroup CreateDomainMemberSecurityGroup()
        {
            SecurityGroup domainMemberSg = new SecurityGroup(this.Template, "DomainMemberSG", "For All Domain Members", this.Subnet.Vpc);
            domainMemberSg.GroupDescription = "Domain Member Security Group";
            return domainMemberSg;
        }

        public void AddToDomainMemberSecurityGroup(Instance domainMember)
        {
            //az1Subnet
            DomainMemberSecurityGroup.AddIngress(domainMember.Subnet as ICidrBlock,
                Protocol.Tcp | Protocol.Udp, Ports.DnsQuery);
            DomainMemberSecurityGroup.AddIngress(domainMember.Subnet,
                Protocol.Tcp | Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
            //DMZSubnet
            // this is questionable overkill
            DomainMemberSecurityGroup.AddIngress(domainMember.Subnet as ICidrBlock, Protocol.Tcp,
                Ports.RemoteDesktopProtocol);
            DomainMemberSecurityGroup.AddIngress(domainMember.Subnet as ICidrBlock, Protocol.Tcp,
                Ports.RemoteDesktopProtocol);

            domainMember.AddSecurityGroup(DomainMemberSecurityGroup);
        }

        [JsonIgnore]
        public SecurityGroup DomainMemberSecurityGroup { get; }

        public void AddToDomain(WindowsInstance instance, TimeSpan timeToWait)
        {
            
            var joinCommandConfig =
                instance.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName)
                    .GetConfig(DefaultConfigSetJoinConfig);
            var joinCommand =
                joinCommandConfig.Commands.AddCommand<PowerShellCommand>(DefaultConfigSetRenameConfigJoinDomain);
            joinCommand.Command.AddCommandLine("-Command \"",
                " Add-Computer -DomainName ",
                this.DomainDnsName,
                " -Credential ",
                "(New-Object System.Management.Automation.PSCredential('",
                this.DomainNetBiosName,
                "\\",
                this.DomainAdminUser,
                "',",
                "(ConvertTo-SecureString ",
                this.DomainAdminPassword,
                " -AsPlainText -Force))) ",
                "-Restart\"");
            joinCommand.WaitAfterCompletion = "forever";

            instance.AddDependsOn(this, timeToWait);
            this.SetDnsServers(instance);
            this.AddToDomainMemberSecurityGroup(instance);
            instance.DomainNetBiosName = this.DomainNetBiosName;
            instance.DomainDnsName = this.DomainDnsName;
            this.AddReplicationSite(instance.Subnet);
            instance.OnAddedToDomain(this.DomainNetBiosName.Default.ToString());
        }

        private void SetDnsServers(WindowsInstance instanceToAddToDomain)
        {
            var renameConfig =
                instanceToAddToDomain.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName)
                    .GetConfig(DefaultConfigSetJoinConfig);
            var renameCommandConfig =
                renameConfig.Commands.AddCommand<PowerShellCommand>(DefaultConfigSetRenameConfigSetDnsServers);
            //todo: the below is supposed to have two private ip addresses for the two different DCs

            string[] ipAddress = new[] {this.LogicalId, "PrivateIp"};

            var ipAddressDictionary = new CloudFormationDictionary(this);

            ipAddressDictionary.Add("Fn::GetAtt", ipAddress);

            renameCommandConfig.Command.AddCommandLine(
                "-Command \"Get-NetAdapter | Set-DnsClientServerAddress -ServerAddresses ",
                ipAddressDictionary,
                "\"");

            renameCommandConfig.WaitAfterCompletion = 0.ToString();
        }

        public class FnGetAtt
        {
            //{ "Fn::GetAtt": [ "dc1", "PrivateIp" ] }

        }
    }
}
