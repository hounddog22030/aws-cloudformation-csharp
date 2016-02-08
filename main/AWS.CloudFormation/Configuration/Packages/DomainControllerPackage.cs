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


            var currentCommand = this.Config.Commands.AddCommand<Command>("InstallPrequisites");

            var addActiveDirectoryPowershell = this.Config.Commands.AddCommand<Command>("AddRSATADPowerShell");
            addActiveDirectoryPowershell.Command = new PowershellFnJoin(FnJoinDelimiter.None, "-Command \"Add-WindowsFeature RSAT-AD-PowerShell,RSAT-AD-AdminCenter\"");
            addActiveDirectoryPowershell.WaitAfterCompletion = 0.ToString();


            currentCommand.Command = new PowershellFnJoin("-Command \"Install-WindowsFeature AD-Domain-Services, rsat-adds -IncludeAllSubFeature\"");
            currentCommand.Test = $"powershell.exe -ExecutionPolicy RemoteSigned {CheckForDomainPsPath}";
            currentCommand.WaitAfterCompletion = 0.ToString();

            currentCommand = this.Config.Commands.AddCommand<Command>("InstallActiveDirectoryDomainServices");
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

            this.CreateDomainControllerSecurityGroup();

            configuration.AddDisk(Ebs.VolumeTypes.Magnetic, 40);

            const string InstallWindowsServerBackup = "c:/cfn/scripts/InstallBackup.ps1";
            var installWindowsServerBackupPath = this.Config.Files.GetFile(InstallWindowsServerBackup);
            installWindowsServerBackupPath.Source = "https://s3.amazonaws.com/gtbb/InstallBackup.ps1";
            this.Config.Commands.AddCommand<Command>("InstallServerBackup", TimeoutMax, null,
                new PowershellFnJoin(FnJoinDelimiter.None, InstallWindowsServerBackup));

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



        private void CreateDomainControllerSecurityGroup()
        {
            ICidrBlock vpcCidrBlock = this.Subnet.Vpc;
            // ReSharper disable once InconsistentNaming
            SecurityGroup SecurityGroup4DomainController = new SecurityGroup("Domain Controller", this.Subnet.Vpc);
            this.Instance.Template.Resources.Add("SecurityGroup4DomainController", SecurityGroup4DomainController);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Tcp, Ports.WsManagementPowerShell);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Tcp, Ports.Http);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Udp, Ports.Ntp);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Tcp, Ports.WinsManager);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Tcp, Ports.ActiveDirectoryManagement);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Udp, Ports.NetBios);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.Smb);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.ActiveDirectoryManagement2);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.Ldap);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Tcp, Ports.Ldaps);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Tcp, Ports.Ldap2Begin, Ports.Ldap2End);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.DnsQuery);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.KerberosKeyDistribution);
            SecurityGroup4DomainController.AddIngress(vpcCidrBlock, Protocol.Tcp | Protocol.Udp, Ports.RemoteDesktopProtocol);
            this.Instance.AddSecurityGroup(SecurityGroup4DomainController);

        }

        public override void Participate(ResourceBase participant)
        {
            LaunchConfiguration participantLaunchConfiguration = participant as LaunchConfiguration;

            var joinCommandConfig = participant.Metadata.Init.ConfigSets.GetConfigSet($"JoinDomain").GetConfig("JoinDomain");

            var checkForDomainPs = joinCommandConfig.Files.GetFile(CheckForDomainPsPath);
            checkForDomainPs.Source = "https://s3.amazonaws.com/gtbb/check-for-domain.ps1";


            var addActiveDirectoryPowershell = joinCommandConfig.Commands.AddCommand<Command>("AddRSATADPowerShell");
            addActiveDirectoryPowershell.Command = new PowershellFnJoin(FnJoinDelimiter.None, "Add-WindowsFeature RSAT-AD-PowerShell,RSAT-AD-AdminCenter");
            addActiveDirectoryPowershell.WaitAfterCompletion = 0.ToString();

            var joinCommand = joinCommandConfig.Commands.AddCommand<Command>("JoinDomain");
            joinCommand.Command = new PowershellFnJoin(FnJoinDelimiter.None,
                "-Command \"",
                    "if ((gwmi win32_computersystem).partofdomain -eq $true)             {",
                        "write-host -fore green \"I am domain joined!\"",
                    "} else {",
                "Add-Computer -DomainName ",
                new FnJoin(FnJoinDelimiter.Period,
                            new ReferenceProperty(DomainControllerPackage.DomainVersionParameterName),
                            new ReferenceProperty(DomainControllerPackage.DomainAppNameParameterName),
                            new ReferenceProperty(DomainControllerPackage.DomainTopLevelNameParameterName)),
                " -Credential (New-Object System.Management.Automation.PSCredential('",
                new ReferenceProperty(DomainNetBiosNameParameterName),
                "\\",
                new ReferenceProperty(DomainAdminUsernameParameterName),
                "',(ConvertTo-SecureString \"",
                new ReferenceProperty(DomainAdminPasswordParameterName),
                "\" -AsPlainText -Force))) ",
                "-Restart\"",
                " }");
            joinCommand.WaitAfterCompletion = "forever";
            joinCommand.Test = $"powershell.exe -ExecutionPolicy RemoteSigned {CheckForDomainPsPath}";

            participant.AddDependsOn(this.WaitCondition);
            //this.AddToDomainMemberSecurityGroup(participantLaunchConfiguration);
            var nodeJson = participantLaunchConfiguration.GetChefNodeJsonContent();
            nodeJson.Add("domain", new ReferenceProperty(DomainNetBiosNameParameterName));

            //Instance participantAsInstance = participant as Instance;
            //if (participantAsInstance != null)
            //{
            //    this.AddReplicationSite(participantAsInstance.Subnet);
            //}



        }
        public void AddReplicationSite(Subnet subnet)
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
            }
        }
    }
}
