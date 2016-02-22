using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        public const string DomainAdminUsernameParameterName = "DomainAdminUsername";
        public const string DomainTopLevelNameParameterName = "DomainTopLevelName";
        public const string DomainAppNameParameterName = "DomainAppName";
        public const string DomainVersionParameterName = "DomainVersion";
        public const string DomainNetBiosNameParameterName = "DomainNetBiosName";
        public const string DomainAdminPasswordParameterName = "DomainAdminPassword";
        public const string DomainFqdn = "DomainFqdn";

        const string CheckForDomainPsPath = "c:/cfn/scripts/check-for-domain.ps1";

        public DomainControllerPackage(Subnet subnet)
        {
            Subnet = subnet;
        }

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);

            var setupFiles = this.Config.Files;
            var checkForDomainPs = this.Config.Files.GetFile(CheckForDomainPsPath);
            checkForDomainPs.Source = "https://s3.amazonaws.com/gtbb/check-for-domain.ps1";

            ConfigFile file = setupFiles.GetFile("c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1");
            file.Source = "https://s3.amazonaws.com/quickstart-reference/microsoft/activedirectory/latest/scripts/ConvertTo-EnterpriseAdmin.ps1";
            const string checkIfUserExists = "c:/cfn/scripts/check-for-user-exists.ps1";
            file = setupFiles.GetFile(checkIfUserExists);
            file.Source = "https://s3.amazonaws.com/gtbb/check-for-user-exists.ps1";


            InstallDomainControllerCommon(this.Instance);

            var currentCommand = this.Config.Commands.AddCommand<Command>("InstallActiveDirectoryDomainServices");
            currentCommand.Test = $"powershell.exe -ExecutionPolicy RemoteSigned {CheckForDomainPsPath}";
            currentCommand.WaitAfterCompletion = TimeoutMax.TotalSeconds.ToString(CultureInfo.InvariantCulture);


            currentCommand.Command = new PowershellFnJoin("-Command \"Install-ADDSForest -DomainName",
                new FnJoin( FnJoinDelimiter.Period,
                            new ReferenceProperty(DomainControllerPackage.DomainVersionParameterName),
                            new ReferenceProperty(DomainControllerPackage.DomainAppNameParameterName),
                            new ReferenceProperty(DomainControllerPackage.DomainTopLevelNameParameterName)),
                "-SafeModeAdministratorPassword (convertto-securestring \"jhkjhsdf338!\" -asplaintext -force) -DomainMode Win2012 -DomainNetbiosName",
                new ReferenceProperty(DomainNetBiosNameParameterName),
                "-ForestMode Win2012 -Confirm:$false -Force\"");

            currentCommand.Test = $"powershell.exe -ExecutionPolicy RemoteSigned {CheckForDomainPsPath}";

            currentCommand = this.Config.Commands.AddCommand<Command>("RestartNetLogon");
            currentCommand.Command = new PowershellFnJoin("-Command Restart-Service NetLogon -EA 0");
            currentCommand.WaitAfterCompletion = 30.ToString();

            currentCommand = this.Config.Commands.AddCommand<Command>("CreateAdminUser");
            currentCommand.WaitAfterCompletion = "0";
            currentCommand.Command = new PowershellFnJoin(FnJoinDelimiter.None, "\"New-ADUser -Name ",
                                new ReferenceProperty(DomainAdminUsernameParameterName),
                                " -UserPrincipalName ",
                                new ReferenceProperty(DomainAdminUsernameParameterName),
                                "@",
                                new FnJoin(FnJoinDelimiter.Period,
                                            new ReferenceProperty(DomainControllerPackage.DomainVersionParameterName),
                                            new ReferenceProperty(DomainControllerPackage.DomainAppNameParameterName),
                                            new ReferenceProperty(DomainControllerPackage.DomainTopLevelNameParameterName)),
                                " -AccountPassword (ConvertTo-SecureString \"",
                                new ReferenceProperty((ILogicalId)this.Instance.Template.Parameters[Template.ParameterDomainAdminPassword]),
                                "\" -AsPlainText -Force) -Enabled $true -PasswordNeverExpires $true\"");
            currentCommand.Test = new PowershellFnJoin(checkIfUserExists, new ReferenceProperty(DomainAdminUsernameParameterName));


            currentCommand = this.Config.Commands.AddCommand<Command>("CreateTfsUser");
            currentCommand.WaitAfterCompletion = "0";
            currentCommand.Command = new PowershellFnJoin(FnJoinDelimiter.None, "\"New-ADUser -Name ",
                new ReferenceProperty(TeamFoundationServerBuildServerBase.TfsServiceAccountNameParameterName),
                " -UserPrincipalName ",
                new ReferenceProperty(TeamFoundationServerBuildServerBase.TfsServiceAccountNameParameterName),
                "@",
                new FnJoin(FnJoinDelimiter.Period,
                            new ReferenceProperty(DomainControllerPackage.DomainVersionParameterName),
                            new ReferenceProperty(DomainControllerPackage.DomainAppNameParameterName),
                            new ReferenceProperty(DomainControllerPackage.DomainTopLevelNameParameterName)),
                " -AccountPassword (ConvertTo-SecureString \"",
                new ReferenceProperty(TeamFoundationServerBuildServerBase.TfsServicePasswordParameterName),
                "\" -AsPlainText -Force) -Enabled $true -PasswordNeverExpires $true\"");

            currentCommand.Test = $"powershell.exe -ExecutionPolicy RemoteSigned {checkIfUserExists} tfsservice";

            currentCommand = this.Config.Commands.AddCommand<Command>("UpdateAdminUser");
            currentCommand.WaitAfterCompletion = "0";
            currentCommand.Command = new PowershellFnJoin("-Command \"c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1 -Members",
                new ReferenceProperty(DomainAdminUsernameParameterName),
                "\"");

            var x = this.WaitCondition;

            currentCommand = this.Config.Commands.AddCommand<Command>("RenameDefaultSite");
            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command =
                new PowershellFnJoin(
                    "\"Get-ADObject -SearchBase (Get-ADRootDSE).ConfigurationNamingContext -filter {Name -eq 'Default-First-Site-Name'} | Rename-ADObject -NewName",
                    new ReferenceProperty(this.Subnet.LogicalId),
                    "\"");

            this.Instance.AddSecurityGroup(this.DomainControllerSecurityGroup);

            const string InstallWindowsServerBackup = "c:/cfn/scripts/InstallBackup.ps1";
            var installWindowsServerBackupPath = this.Config.Files.GetFile(InstallWindowsServerBackup);
            installWindowsServerBackupPath.Source = "https://s3.amazonaws.com/gtbb/InstallBackup.ps1";

            currentCommand  = this.Config.Commands.AddCommand<Command>("InstallServerBackup");
            currentCommand.Command = new PowershellFnJoin(" -Command ",InstallWindowsServerBackup);
            currentCommand.WaitAfterCompletion = 0.ToString();

            //"Get-ADReplicationSiteLink -Filter * | Set-ADReplicationSiteLink -SitesIncluded @{add='AZ2'} -ReplicationFrequencyInMinutes 15\""


        }

        private void InstallDomainControllerCommon(LaunchConfiguration instance)
        {
            var config = instance.Metadata.Init.ConfigSets.GetConfigSet(this.ConfigSetName).GetConfig(this.ConfigName);
            var currentCommand = config.Commands.AddCommand<Command>("InstallPrequisites");
            var addActiveDirectoryPowershell = config.Commands.AddCommand<Command>("AddRSATADPowerShell");
            addActiveDirectoryPowershell.Command = new PowershellFnJoin(FnJoinDelimiter.None,
                "-Command \"Add-WindowsFeature RSAT-AD-PowerShell,RSAT-AD-AdminCenter\"");
            addActiveDirectoryPowershell.WaitAfterCompletion = 0.ToString();


            currentCommand.Command =
                new PowershellFnJoin("-Command \"Install-WindowsFeature AD-Domain-Services, rsat-adds -IncludeAllSubFeature\"");
            currentCommand.Test = $"powershell.exe -ExecutionPolicy RemoteSigned {CheckForDomainPsPath}";
            currentCommand.WaitAfterCompletion = 0.ToString();
        }

        public void MakeSecondaryDomainController(LaunchConfiguration secondary)
        {
            
            secondary.AddSecurityGroup(this.DomainControllerSecurityGroup);

            InstallDomainControllerCommon(secondary);
            var config = secondary.Metadata.Init.ConfigSets.GetConfigSet(this.ConfigSetName).GetConfig(this.ConfigName);

            //Install-WindowsFeature AD-Domain-Services, rsat-adds -IncludeAllSubFeature

            var currentCommand = config.Commands.AddCommand<Command>("InstallWindowsFeatureADDomainServices");
            currentCommand.Command = new PowershellFnJoin("-Command \"Install-WindowsFeature AD-Domain-Services, rsat-adds -IncludeAllSubFeature\"");

            currentCommand = config.Commands.AddCommand<Command>("InstallADDSDomainControllerr");
            currentCommand.Command = new PowershellFnJoin(FnJoinDelimiter.None, "-Command \"Install-ADDSDomainController -InstallDns -DomainName ",
                new FnJoin(FnJoinDelimiter.Period,
                            new ReferenceProperty(DomainControllerPackage.DomainVersionParameterName),
                            new ReferenceProperty(DomainControllerPackage.DomainAppNameParameterName),
                            new ReferenceProperty(DomainControllerPackage.DomainTopLevelNameParameterName)),
                " -Credential ",
                "(New-Object System.Management.Automation.PSCredential('",
                new ReferenceProperty(DomainControllerPackage.DomainNetBiosNameParameterName),
                "\\",
                new ReferenceProperty(DomainControllerPackage.DomainAdminUsernameParameterName),
                "',",
                "(ConvertTo-SecureString ",
                new ReferenceProperty(DomainControllerPackage.DomainAdminPasswordParameterName),
                " -AsPlainText -Force))) ",
                " -SafeModeAdministratorPassword (convertto-securestring \"jhkjhsdf338!\" -asplaintext -force) ",
                " -Confirm:$false -Force\"");
        }

        public Subnet Subnet { get; }

        private SecurityGroup _domainMemberSecurityGroup;

        public SecurityGroup DomainMemberSecurityGroup {
            get
            {
                if (_domainMemberSecurityGroup == null)
                {
                    _domainMemberSecurityGroup = new SecurityGroup("For All Domain Members", this.Subnet.Vpc);
                    this.Instance.Template.Resources.Add("SecurityGroup4DomainMember", _domainMemberSecurityGroup);
                    _domainMemberSecurityGroup.GroupDescription = "Domain Member Security Group";
                }
                return _domainMemberSecurityGroup;

            }
        }

        private SecurityGroup _domainControllerSecurityGroup = null;

        private SecurityGroup DomainControllerSecurityGroup
        {
            get
            {
                if (_domainControllerSecurityGroup == null)
                {
                    ICidrBlock vpcCidrBlock = this.Subnet.Vpc;
                    // ReSharper disable once InconsistentNaming
                    _domainControllerSecurityGroup = new SecurityGroup("Domain Controller", this.Subnet.Vpc);
                    this.Instance.Template.Resources.Add("SecurityGroup4DomainController", _domainControllerSecurityGroup);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Tcp, Ports.WsManagementPowerShell);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Tcp, Ports.Http);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Udp, Ports.Ntp);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Tcp, Ports.WinsManager);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Tcp, Ports.ActiveDirectoryManagement);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Udp, Ports.NetBios);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.Smb);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.ActiveDirectoryManagement2);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.Ldap);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Tcp, Ports.Ldaps);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Tcp, Ports.Ldap2Begin, Ports.Ldap2End);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.DnsQuery);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.KerberosKeyDistribution);
                    _domainControllerSecurityGroup.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.RemoteDesktopProtocol);
                }
                return _domainControllerSecurityGroup;
            }
        }


        public override void Participate(ResourceBase participant)
        {
            LaunchConfiguration participantLaunchConfiguration = participant as LaunchConfiguration;

            var joinCommandConfig = participant.Metadata.Init.ConfigSets.GetConfigSet($"JoinDomain").GetConfig("JoinDomain");

            var checkForDomainPs = joinCommandConfig.Files.GetFile(CheckForDomainPsPath);
            checkForDomainPs.Source = "https://s3.amazonaws.com/gtbb/check-for-domain.ps1";


            //var addActiveDirectoryPowershell = joinCommandConfig.Commands.AddCommand<Command>("AddRSATADPowerShell");
            //addActiveDirectoryPowershell.Command = new PowershellFnJoin(FnJoinDelimiter.None, "Add-WindowsFeature RSAT-AD-PowerShell,RSAT-AD-AdminCenter");
            //addActiveDirectoryPowershell.WaitAfterCompletion = 0.ToString();

            //start /high powershell.exe -Command "Add-Computer -DomainName nu.dev.yadayadasoftware.com -Credential (New-Object System.Management.Automation.PSCredential('Nudev\admin',(ConvertTo-SecureString "MSKB4417hfcm" -AsPlainText -Force))) -Restart"

            var joinCommand = joinCommandConfig.Commands.AddCommand<Command>("JoinDomain");
            joinCommand.Command = new PowershellFnJoin(FnJoinDelimiter.None,
                "-Command \"",
                "Add-Computer -DomainName ",
                new FnJoin(FnJoinDelimiter.Period,
                            new ReferenceProperty(DomainControllerPackage.DomainVersionParameterName),
                            new ReferenceProperty(DomainControllerPackage.DomainAppNameParameterName),
                            new ReferenceProperty(DomainControllerPackage.DomainTopLevelNameParameterName)),
                " -Credential (New-Object System.Management.Automation.PSCredential('",
                new ReferenceProperty(DomainAdminUsernameParameterName),
                "@",
                new FnJoin(FnJoinDelimiter.Period,
                new ReferenceProperty(DomainControllerPackage.DomainVersionParameterName),
                new ReferenceProperty(DomainControllerPackage.DomainAppNameParameterName),
                new ReferenceProperty(DomainControllerPackage.DomainTopLevelNameParameterName)),
                "',(ConvertTo-SecureString \"",
                new ReferenceProperty(DomainAdminPasswordParameterName),
                "\" -AsPlainText -Force))) ",
                "-Restart\"");
            joinCommand.WaitAfterCompletion = "forever";
            joinCommand.Test = $"powershell.exe -ExecutionPolicy RemoteSigned {CheckForDomainPsPath}";
            var xFile = joinCommandConfig.Files.GetFile("c:/cfn/scripts/joindomain.txt");
            xFile.Content.Add("x", joinCommand.Command);

            //participant.AddDependsOn(this.WaitCondition);
            //this.AddToDomainMemberSecurityGroup(participantLaunchConfiguration);
            var nodeJson = participantLaunchConfiguration.GetChefNodeJsonContent();
            nodeJson.Add("domain", new ReferenceProperty(DomainNetBiosNameParameterName));

            //Instance participantAsInstance = participant as Instance;
            //if (participantAsInstance != null)
            //{
            //    this.AddReplicationSite(participantAsInstance.Subnet);
            //}



        }
        public void AddReplicationSite(Subnet subnet, bool isDomainController)
        {
            var currentConfig = this.Config;
            string commandName = $"CreateSite4{subnet.LogicalId}";

            if (!currentConfig.Commands.ContainsKey(commandName))
            {
                const string checkAdReplicationSite = "c:/cfn/scripts/check-ADReplicationSite-exists.ps1";
                const string checkAdReplicationSubnet = "c:/cfn/scripts/check-ADReplicationSubnet-exists.ps1";

                var fileCheckAdReplicationSite = currentConfig.Files.GetFile(checkAdReplicationSite);
                fileCheckAdReplicationSite.Source = "https://s3.amazonaws.com/gtbb/check-ADReplicationSite-exists.ps1";
                fileCheckAdReplicationSite = currentConfig.Files.GetFile(checkAdReplicationSubnet);
                fileCheckAdReplicationSite.Source = "https://s3.amazonaws.com/gtbb/check-ADReplicationSubnet-exists.ps1";


                ConfigCommand currentCommand = currentConfig.Commands.AddCommand<Command>(commandName);
                currentCommand.WaitAfterCompletion = 0.ToString();
                currentCommand.Command = new PowershellFnJoin("-Command  \"New-ADReplicationSite",
                    new ReferenceProperty(subnet),
                    "\"");
                currentCommand.Test = new PowershellFnJoin(
                        checkAdReplicationSite,
                        new ReferenceProperty(subnet));

                currentCommand = currentConfig.Commands.AddCommand<Command>($"CreateSubnet{subnet.LogicalId}");
                currentCommand.WaitAfterCompletion = 0.ToString();
                currentCommand.Command = new PowershellFnJoin($"-Command New-ADReplicationSubnet -Name {subnet.CidrBlock} -Site ",
                    new ReferenceProperty(subnet));
                currentCommand.Test = new PowershellFnJoin(
                        checkAdReplicationSubnet,
                        subnet.CidrBlock);

                if (isDomainController)
                {
                    currentCommand = currentConfig.Commands.AddCommand<Command>($"SetADReplicationSiteLink{subnet.LogicalId}");
                    currentCommand.Command = new PowershellFnJoin($"Get-ADReplicationSiteLink -Filter * | Set-ADReplicationSiteLink -SitesIncluded @{{add = '{subnet.LogicalId}'}} -ReplicationFrequencyInMinutes 15\"");
                    currentCommand.WaitAfterCompletion = 0.ToString();
                }
            }
        }
    }
}
