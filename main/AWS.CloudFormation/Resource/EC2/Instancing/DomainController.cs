using System;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Resource.Wait;
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
            //this.MakeDomainController();
            ConfigureDefaultSite(subnet);
        }

        [JsonIgnore]
        public ParameterBase DomainAdminUser { get; }

        [JsonIgnore]
        public ParameterBase DomainAdminPassword { get; set; }

        private WaitCondition _domainAvailable = null;

        private WaitCondition DomainAvailable
        {
            get
            {
                if (_domainAvailable == null)
                {
                    _domainAvailable = new WaitCondition(this.Template, $"{this.LogicalId}DomainAvailableWaitCondition", new TimeSpan(4,0,0));
                }
                return _domainAvailable;
            }
        }

        private void MakeDomainController()
        {
            var setup = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("setup");

            var setupFiles = setup.Files;

            ConfigFile file = setupFiles.GetFile("c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1");

            file.Source =
                "https://s3.amazonaws.com/quickstart-reference/microsoft/activedirectory/latest/scripts/ConvertTo-EnterpriseAdmin.ps1";

            var currentConfig = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("installADDS");
            var currentCommand = currentConfig.Commands.AddCommand<Command>("1-install-prereqsz");

            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command = new PowershellFnJoin("-Command \"Install-WindowsFeature AD-Domain-Services, rsat-adds -IncludeAllSubFeature\"");

            currentCommand = currentConfig.Commands.AddCommand<Command>("2-install-adds");
            currentCommand.WaitAfterCompletion = "forever";
            currentCommand.Test = $"if \"%USERDNSDOMAIN%\"==\"{this.DomainDnsName.Default.ToString().ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";
            currentCommand.Command = new PowershellFnJoin("-Command \"Install-ADDSForest -DomainName",
                new ReferenceProperty(this.DomainDnsName),
                "-SafeModeAdministratorPassword (convertto-securestring jhkjhsdf338! -asplaintext -force) -DomainMode Win2012 -DomainNetbiosName",
                new ReferenceProperty(this.DomainNetBiosName),
                "-ForestMode Win2012 -Confirm:$false -Force\"");

            currentCommand.Test =
                $"if \"%USERDNSDOMAIN%\"==\"{this.DomainDnsName.Default.ToString().ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";


            currentCommand = currentConfig.Commands.AddCommand<Command>("3-restart-service");
            currentCommand.WaitAfterCompletion = 20.ToString();
            currentCommand.Command = new PowershellFnJoin("-Command \"Restart-Service NetLogon -EA 0\"");
            currentCommand.Test = $"if \"%USERDNSDOMAIN%\"==\"{this.DomainDnsName.Default.ToString().ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";

            currentCommand = currentConfig.Commands.AddCommand<Command>("4 - create - adminuser");
            currentCommand.WaitAfterCompletion = "0";
            currentCommand.Command = new PowershellFnJoin("\"New-ADUser -Name ",
                new ReferenceProperty(this.DomainAdminUser),
                "-UserPrincipalName",
                new ReferenceProperty(this.DomainAdminUser),
                "@",
                new ReferenceProperty(this.DomainDnsName),
                "-AccountPassword (ConvertTo-SecureString ",
                new ReferenceProperty(this.DomainAdminPassword),
                "-AsPlainText -Force) -Enabled $true -PasswordNeverExpires $true\"");
            currentCommand.Test =
                $"if \"%USERDNSDOMAIN%\"==\"{this.DomainDnsName.Default.ToString().ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";

            currentCommand = currentConfig.Commands.AddCommand<Command>("5 - update - adminuser");
            currentCommand.WaitAfterCompletion = "0";
            currentCommand.Command = new PowershellFnJoin("-Command \"c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1 -Members",
                new ReferenceProperty(this.DomainAdminUser),
                "\"");
            currentCommand.Test = $"if \"%USERDNSDOMAIN%\"==\"{this.DomainDnsName.Default.ToString().ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";

            currentConfig.Commands.AddCommand<Command>(this.DomainAvailable);

            this.OnAddedToDomain(this.DomainNetBiosName.Default.ToString());
        }

        public void AddReplicationSite(Subnet subnet)
        {
            var currentConfig = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("configureSites");
            string commandName = $"create-site-{subnet.LogicalId}";
            if (!currentConfig.Commands.ContainsKey(commandName))
            {
                ConfigCommand currentCommand = currentConfig.Commands.AddCommand<Command>(commandName);
                currentCommand.WaitAfterCompletion = 0.ToString();
                currentCommand.Command = new PowershellFnJoin("-Command \"New-ADReplicationSite",
                    new ReferenceProperty(subnet),
                    "\"");

                currentCommand = currentConfig.Commands.AddCommand<Command>($"create-subnet-{subnet.LogicalId}");
                currentCommand.WaitAfterCompletion = 0.ToString();
                currentCommand.Command = new PowershellFnJoin($"-Command New-ADReplicationSubnet -Name {subnet.CidrBlock} -Site ",
                    new ReferenceProperty(subnet));
            }
        }

        private Config ConfigureDefaultSite(Subnet defaultSubnet)
        {
            var currentConfig = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("configureSites");
            ConfigCommand currentCommand = currentConfig.Commands.AddCommand<Command>("a-rename-default-site");
            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command =
                new PowershellFnJoin(
                    "\"Get-ADObject -SearchBase (Get-ADRootDSE).ConfigurationNamingContext -filter {{Name -eq 'Default-First-Site-Name'}} | Rename-ADObject -NewName",
                    new ReferenceProperty(defaultSubnet.LogicalId),
                    "\"");

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
                joinCommandConfig.Commands.AddCommand<Command>(DefaultConfigSetRenameConfigJoinDomain);
            joinCommand.Command = new FnJoin(FnJoinDelimiter.None,
                "-Command \"",
                    "if ((gwmi win32_computersystem).partofdomain -eq $true)             {",
                        "write-host -fore green \"I am domain joined!\"",
                    "} else {",
                "Add-Computer -DomainName ",
                new ReferenceProperty(this.DomainDnsName),
                " -Credential (New-Object System.Management.Automation.PSCredential('",
                new ReferenceProperty(this.DomainNetBiosName),
                "\\",
                new ReferenceProperty(this.DomainAdminUser),
                "',(ConvertTo-SecureString ",
                new ReferenceProperty(this.DomainAdminPassword),
                " -AsPlainText -Force))) ",
                "-Restart\"",
                " }");
            joinCommand.WaitAfterCompletion = "90";
            joinCommand.Test = $"if \"%USERDNSDOMAIN%\"==\"{this.DomainDnsName.Default.ToString().ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";

            instance.AddDependsOn(this.DomainAvailable);
            this.AddToDomainMemberSecurityGroup(instance);
            instance.DomainNetBiosName = this.DomainNetBiosName;
            instance.DomainDnsName = this.DomainDnsName;
            this.AddReplicationSite(instance.Subnet);
            instance.OnAddedToDomain(this.DomainNetBiosName.Default.ToString());
        }

    }
}
