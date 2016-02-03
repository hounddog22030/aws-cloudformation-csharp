using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Configuration.Packages
{
    public class DomainControllerPackage : PackageBase<ConfigSet>
    {

        public DomainControllerPackage(DomainInfo domainInfo, Subnet subnet)
        {
            this.DomainInfo = domainInfo;
            Subnet = subnet;
        }

        private DomainInfo DomainInfo { get; }

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            var setup = this.Instance.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("setup");

            var setupFiles = setup.Files;

            ConfigFile file = setupFiles.GetFile("c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1");
            file.Source = "https://s3.amazonaws.com/quickstart-reference/microsoft/activedirectory/latest/scripts/ConvertTo-EnterpriseAdmin.ps1";


            var currentConfig = this.Instance.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("installADDS");
            var currentCommand = currentConfig.Commands.AddCommand<Command>("01-InstallPrequisites");

            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command = new PowershellFnJoin("-Command \"Install-WindowsFeature AD-Domain-Services, rsat-adds -IncludeAllSubFeature\"");
            

            currentCommand = currentConfig.Commands.AddCommand<Command>("02-InstallActiveDirectoryDomainServices");
            currentCommand.WaitAfterCompletion = new TimeSpan(0, 4, 0).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            currentCommand.Test = $"IF \"%USERDNSDOMAIN%\"==\"{this.DomainInfo.DomainDnsName.ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";


            currentCommand.Command = new PowershellFnJoin("-Command \"Install-ADDSForest -DomainName",
                this.DomainInfo.DomainDnsName,
                "-SafeModeAdministratorPassword (convertto-securestring jhkjhsdf338! -asplaintext -force) -DomainMode Win2012 -DomainNetbiosName",
                this.DomainInfo.DomainNetBiosName,
                "-ForestMode Win2012 -Confirm:$false -Force\"");

            currentCommand.Test = $"ECHO \"%USERDNSDOMAIN%\" IF \"%USERDNSDOMAIN%\"==\"{this.DomainInfo.DomainDnsName.ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";


            //currentCommand = currentConfig.Commands.AddCommand<Command>("3-restart-service");
            //currentCommand.WaitAfterCompletion = 0.ToString();
            //currentCommand.Command = new PowershellFnJoin("-Command \"Restart-Service NetLogon -EA 0\"");
            //currentCommand.Test = $"if \"%USERDNSDOMAIN%\"==\"{this.DomainInfo.DomainDnsName.ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";

            currentCommand = currentConfig.Commands.AddCommand<Command>("04-CreateAdminUser");
            currentCommand.WaitAfterCompletion = "0";
            currentCommand.Command = new PowershellFnJoin(FnJoinDelimiter.None, "\"New-ADUser -Name ",
                this.DomainInfo.AdminUserName,
                " -UserPrincipalName ",
                this.DomainInfo.AdminUserName,
                "@",
                this.DomainInfo.DomainDnsName,
                " -AccountPassword (ConvertTo-SecureString ",
                this.DomainInfo.AdminPassword,
                " -AsPlainText -Force) -Enabled $true -PasswordNeverExpires $true\"");

            currentCommand = currentConfig.Commands.AddCommand<Command>("05-UpdateAdminUser");
            currentCommand.WaitAfterCompletion = "0";
            currentCommand.Command = new PowershellFnJoin("-Command \"c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1 -Members",
                this.DomainInfo.AdminUserName,
                "\"");

            currentCommand = currentConfig.Commands.AddCommand<Command>("06-RenameDefaultSite");
            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command =
                new PowershellFnJoin(
                    "\"Get-ADObject -SearchBase (Get-ADRootDSE).ConfigurationNamingContext -filter {Name -eq 'Default-First-Site-Name'} | Rename-ADObject -NewName",
                    new ReferenceProperty(this.Subnet.LogicalId),
                    "\"");


            currentConfig.Commands.AddCommand<Command>(this.WaitCondition);

            this.CreateDomainControllerSecurityGroup();

        }

        public Subnet Subnet { get; }

        private SecurityGroup _domainMemberSecurityGroup;

        public SecurityGroup DomainMemberSecurityGroup {
            get
            {
                if (_domainMemberSecurityGroup == null)
                {
                    _domainMemberSecurityGroup = new SecurityGroup(this.Instance.Template, "DomainMemberSG", "For All Domain Members", this.Subnet.Vpc);
                    _domainMemberSecurityGroup.GroupDescription = "Domain Member Security Group";
                }
                return _domainMemberSecurityGroup;

            }
        }


        private void CreateDomainControllerSecurityGroup()
        {
            // ReSharper disable once InconsistentNaming
            SecurityGroup DomainControllerSG1 = new SecurityGroup(this.Instance.Template, "DomainControllerSG1", "Domain Controller", this.Subnet.Vpc);
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

            this.Instance.AddSecurityGroup(DomainControllerSG1);

        }

        public override void Participate(ResourceBase participant)
        {
            LaunchConfiguration participantLaunchConfiguration = participant as LaunchConfiguration;

            var joinCommandConfig = participant.Metadata.Init.ConfigSets.GetConfigSet($"JoinDomain{this.DomainInfo.DomainNetBiosName}").GetConfig("JoinDomain");
            const string checkForDomainPsPath = "c:/cfn/scripts/check-for-domain.ps1";
            var checkForDomainPs = joinCommandConfig.Files.GetFile(checkForDomainPsPath);
            checkForDomainPs.Source = "https://s3.amazonaws.com/gtbb/check-for-domain.ps1";

            var joinCommand = joinCommandConfig.Commands.AddCommand<Command>("JoinDomain");

            joinCommand.Command = new PowershellFnJoin(FnJoinDelimiter.None,
                "-Command \"",
                    "if ((gwmi win32_computersystem).partofdomain -eq $true)             {",
                        "write-host -fore green \"I am domain joined!\"",
                    "} else {",
                "Add-Computer -DomainName ",
                this.DomainInfo.DomainDnsName,
                " -Credential (New-Object System.Management.Automation.PSCredential('",
                this.DomainInfo.DomainNetBiosName,
                "\\",
                this.DomainInfo.AdminUserName,
                "',(ConvertTo-SecureString ",
                this.DomainInfo.AdminPassword,
                " -AsPlainText -Force))) ",
                "-Restart\"",
                " }");
            joinCommand.WaitAfterCompletion = "forever";

            joinCommand.Test = $"powershell.exe -ExecutionPolicy RemoteSigned {checkForDomainPsPath} {this.DomainInfo.DomainDnsName}";

            participant.AddDependsOn(this.WaitCondition);
            this.AddToDomainMemberSecurityGroup((Instance)participant);
            participantLaunchConfiguration.DomainNetBiosName = this.DomainInfo.DomainNetBiosName;
            participantLaunchConfiguration.DomainDnsName = this.DomainInfo.DomainDnsName;
            var nodeJson = participantLaunchConfiguration.GetChefNodeJsonContent();
            nodeJson.Add("domain", this.DomainInfo.DomainNetBiosName);

            Instance participantAsInstance = participant as Instance;
            if (participantAsInstance != null)
            {
                this.AddReplicationSite(participantAsInstance.Subnet);
            }



        }
        public void AddReplicationSite(Subnet subnet)
        {
            var currentConfig = this.Instance.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("configureSites");
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
    }
}
