using System;
using AWS.CloudFormation.Instance;
using AWS.CloudFormation.Instance.Metadata.Config;
using AWS.CloudFormation.Instance.Metadata.Config.Command;
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
            public DomainInfo(string domainDnsName, string domainNetBiosName, string adminUserName, string adminPassword)
            {
                DomainDnsName = domainDnsName;
                DomainNetBiosName = domainNetBiosName;
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
        


        public DomainController(Template template, string name, InstanceTypes instanceType, string imageId, Subnet subnet, string staticIpAddress, DomainInfo domainInfo)
            : base(template, name, instanceType, imageId, subnet, true)
        {
            this.DomainDnsName = new ParameterBase(DomainController.ParameterNameDomainDnsName, "String", domainInfo.DomainDnsName); ;
            Template.AddParameter(this.DomainDnsName);

            this.DomainAdminPassword = new ParameterBase(DomainController.ParameterNameDomainAdminPassword, "String", domainInfo.AdminPassword);
            Template.AddParameter(DomainAdminPassword);

            this.DomainNetBiosName = new ParameterBase(DomainController.ParameterNameDomainNetBiosName, "String", domainInfo.DomainNetBiosName);
            template.AddParameter(this.DomainNetBiosName);

            this.DomainAdminUser = new ParameterBase(DomainController.ParameterNameDomainAdminUser, "String", domainInfo.AdminUserName); 
            template.AddParameter(this.DomainAdminUser);

            DomainMemberSecurityGroup = this.CreateDomainMemberSecurityGroup();
            this.CreateDomainControllerSecurityGroup();
            if (!string.IsNullOrEmpty(staticIpAddress))
            {
                this.PrivateIpAddress = staticIpAddress;
            }
            this.MakeDomainController();

        }

        public ParameterBase DomainAdminUser { get; }

        public ParameterBase DomainAdminPassword { get; set; }


        private void MakeDomainController()
        {
            var setup = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("setup");

            var setupFiles = setup.Files;

            setupFiles.GetFile("c:\\cfn\\scripts\\Set-StaticIP.ps1")
                .Content.SetFnJoin(
                    "$netip = Get-NetIPConfiguration;",
                    "$ipconfig = Get-NetIPAddress | ?{$_.IpAddress -eq $netip.IPv4Address.IpAddress};",
                    "Get-NetAdapter | Set-NetIPInterface -DHCP Disabled;",
                    "Get-NetAdapter | New-NetIPAddress -AddressFamily IPv4 -IPAddress $netip.IPv4Address.IpAddress -PrefixLength $ipconfig.PrefixLength -DefaultGateway $netip.IPv4DefaultGateway.NextHop;",
                    "Get-NetAdapter | Set-DnsClientServerAddress -ServerAddresses $netip.DNSServer.ServerAddresses;",
                    "\n");

            ConfigFile file = setupFiles.GetFile("c:\\cfn\\scripts\\New-LabADUser.ps1");
            file.Source = "https://s3.amazonaws.com/CFN_WS2012_Scripts/AD/New-LabADUser.ps1";

            file = setupFiles.GetFile("c:\\cfn\\scripts\\users.csv");
            file.Source = "https://s3.amazonaws.com/CFN_WS2012_Scripts/AD/users.csv";

            file = setupFiles.GetFile("c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1");
            file.Source =
                "https://s3.amazonaws.com/quickstart-reference/microsoft/activedirectory/latest/scripts/ConvertTo-EnterpriseAdmin.ps1";

            //powershell - Command "Get-NetFirewallProfile | Set-NetFirewallProfile - Enabled False" > c:\cfn\log\a-disable-win-fw.log

            var disableFirewallCommand = setup.Commands.AddCommand<PowerShellCommand>("a-disable-win-fw");
            disableFirewallCommand.WaitAfterCompletion = 0.ToString();
            disableFirewallCommand.Command.AddCommandLine(new object[]
            {"-Command \"Get-NetFirewallProfile | Set-NetFirewallProfile -Enabled False\""});

            var setStaticIpCommand =
                this.Metadata.Init.ConfigSets.GetConfigSet("config")
                    .GetConfig("rename")
                    .Commands.AddCommand<PowerShellCommand>("a-set-static-ip");
            setStaticIpCommand.WaitAfterCompletion = 15.ToString();
            setStaticIpCommand.Command.AddCommandLine(
                "-ExecutionPolicy RemoteSigned -Command \"c:\\cfn\\scripts\\Set-StaticIP.ps1\"");

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


            currentConfig = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("configureSites");
            currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("a-rename-default-site");
            currentCommand.WaitAfterCompletion = 0.ToString();

            currentCommand.Command.AddCommandLine(" ",
                "\"",
                "Get-ADObject -SearchBase (Get-ADRootDSE).ConfigurationNamingContext -filter {Name -eq 'Default-First-Site-Name'} | Rename-ADObject -NewName DMZSubnet",
                "\"");

            currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("b-create-site-2");
            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command.AddCommandLine("\"",
                "New-ADReplicationSite DMZ2Subnet",
                "\"");


            //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("c-create-DMZSubnet-1");
            //currentCommand.WaitAfterCompletion = 0.ToString();
            //currentCommand.Command.AddCommandLine("-Command New-ADReplicationSubnet -Name ",
            //                                        DmzAz1Cidr,
            //                                        " -Site AZ1");

            //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("d-create-DMZSubnet-2");
            //currentCommand.WaitAfterCompletion = 0.ToString();
            //currentCommand.Command.AddCommandLine("-Command New-ADReplicationSubnet -Name ",
            //                                        DmzAz2Cidr,
            //                                        " -Site AZ2");

            //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("e-create-subnet-1");
            //currentCommand.WaitAfterCompletion = 0.ToString();
            //currentCommand.Command.AddCommandLine("-Command New-ADReplicationSubnet -Name ",
            //                                        Az1SubnetCidr,
            //                                        " -Site AZ1");

            //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("f-create-subnet-2");
            //currentCommand.WaitAfterCompletion = 0.ToString();
            //currentCommand.Command.AddCommandLine("-Command New-ADReplicationSubnet -Name ",
            //                                        Az2SubnetCidr,
            //                                        " -Site AZ2");

            currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("m-set-site-link");
            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command.AddCommandLine("-Command \"",
                "Get-ADReplicationSiteLink -Filter * | Set-ADReplicationSiteLink -SitesIncluded @{add='DMZ2Subnet'} -ReplicationFrequencyInMinutes 15\"");

            this.OnAddedToDomain(this.DomainNetBiosName.Default.ToString());
        }

        public void CreateAdReplicationSubnet(Subnet subnet)
        {
            var currentConfig = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("configureSites");
            var currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>($"create-subnet-{subnet.Name}");
            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command.AddCommandLine("-Command New-ADReplicationSubnet -Name ",
                subnet.CidrBlock,
                " -Site ",
                subnet.Name);
        }

        private void CreateDomainControllerSecurityGroup()
        {
            // ReSharper disable once InconsistentNaming
            SecurityGroup DomainControllerSG1 = Template.GetSecurityGroup("DomainControllerSG1", this.Vpc,
                "Domain Controller");
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(this.Vpc, Protocol.Tcp,
                Ports.WsManagementPowerShell);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(this.Vpc, Protocol.Tcp, Ports.Http);
            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Udp, Ports.Ntp);
            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.WinsManager);
            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.ActiveDirectoryManagement);
            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Udp, Ports.NetBios);

            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp | Protocol.Udp, Ports.Smb);
            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp | Protocol.Udp, Ports.ActiveDirectoryManagement2);
            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp | Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp | Protocol.Udp, Ports.Ldap);

            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.Ldaps);
            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.Ldap2Begin, Ports.Ldap2End);

            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp | Protocol.Udp, Ports.DnsQuery);

            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.ActiveDirectoryManagement);

            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp | Protocol.Udp, Ports.KerberosKeyDistribution);

            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Udp, Ports.DnsLlmnr);
            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Udp, Ports.NetBt);
            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.NetBiosNameServices);
            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.ActiveDirectoryFileReplication);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Udp,
                Ports.Ntp);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp,
                Ports.WinsManager);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp,
                Ports.ActiveDirectoryManagement);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Udp,
                Ports.NetBios);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.Smb);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.ActiveDirectoryManagement2);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.Ldap);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp,
                Ports.Ldaps);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp,
                Ports.Ldap2Begin, Ports.Ldap2End);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.DnsQuery);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.KerberosKeyDistribution);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup,
                Protocol.Tcp | Protocol.Udp, Ports.RemoteDesktopProtocol);

            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DMZSubnet, Protocol.Tcp | Protocol.Udp, Ports.Rdp);

            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DMZSubnet, Protocol.Icmp, Ports.All);
            //DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(dmzaz2Subnet, Protocol.Icmp, Ports.All);
            this.SecurityGroups.Add(DomainControllerSG1);

        }

        private SecurityGroup CreateDomainMemberSecurityGroup()
        {
            //ParameterBase domain = Template.Parameters["DomainDNSName"];
            //SecurityGroup returnValue = Template.AddSecurityGroup(domain.Default.Replace(".",string.Empty) + "SecurityGroup", this.Vpc,"Domain Member Security Group");
            //return returnValue;
            SecurityGroup domainMemberSg = Template.GetSecurityGroup("DomainMemberSG", this.Vpc,
                "For All Domain Members");
            domainMemberSg.GroupDescription = "Domain Member Security Group";
            ////az1Subnet
            //domainMemberSg.AddIngressEgress<SecurityGroupIngress>(domainMemberSg, Protocol.Tcp | Protocol.Udp, Ports.DnsQuery);
            //domainMemberSg.AddIngressEgress<SecurityGroupIngress>(domainMemberSg, Protocol.Tcp | Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);


            //SubnetRouteTableAssociation az1PrivateSubnetRouteTableAssociation = new SubnetRouteTableAssociation(
            //    this.Template,
            //    "AZ1PrivateSubnetRouteTableAssociation", 
            //    az1Subnet, 
            //    az1PrivateRouteTable);

            //this.Template.Resources.Add("AZ1PrivateSubnetRouteTableAssociation", az1PrivateSubnetRouteTableAssociation);

            return domainMemberSg;
        }

        public void AddToDomainMemberSecurityGroup(Instance domainMember)
        {
            //az1Subnet
            DomainMemberSecurityGroup.AddIngressEgress<SecurityGroupIngress>(domainMember.Subnet,
                Protocol.Tcp | Protocol.Udp, Ports.DnsQuery);
            DomainMemberSecurityGroup.AddIngressEgress<SecurityGroupIngress>(domainMember.Subnet,
                Protocol.Tcp | Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
            //DMZSubnet
            // this is questionable overkill
            DomainMemberSecurityGroup.AddIngressEgress<SecurityGroupIngress>(domainMember.Subnet, Protocol.Tcp,
                Ports.RemoteDesktopProtocol);
            DomainMemberSecurityGroup.AddIngressEgress<SecurityGroupIngress>(domainMember.Subnet, Protocol.Tcp,
                Ports.RemoteDesktopProtocol);

            domainMember.SecurityGroups.Add(DomainMemberSecurityGroup);
        }

        [JsonIgnore]
        public SecurityGroup DomainMemberSecurityGroup { get; }

        public void AddToDomain(WindowsInstance instanceToAddToDomain, TimeSpan timeToWait)
        {
            var joinCommandConfig =
                instanceToAddToDomain.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName)
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

            instanceToAddToDomain.AddDependsOn(this, timeToWait);
            this.SetDnsServers(instanceToAddToDomain);
            this.AddToDomainMemberSecurityGroup(instanceToAddToDomain);
            instanceToAddToDomain.DomainNetBiosName = this.DomainNetBiosName;
            instanceToAddToDomain.DomainDnsName = this.DomainDnsName;


            instanceToAddToDomain.OnAddedToDomain(this.DomainNetBiosName.Default.ToString());
        }

        private void SetDnsServers(WindowsInstance instanceToAddToDomain)
        {
            if (!string.IsNullOrEmpty(this.PrivateIpAddress))
            {
                var renameConfig = instanceToAddToDomain.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName).GetConfig(DefaultConfigSetJoinConfig);
                var renameCommandConfig = renameConfig.Commands.AddCommand<PowerShellCommand>(DefaultConfigSetRenameConfigSetDnsServers);
                //todo: the below is supposed to have two private ip addresses for the two different DCs
                renameCommandConfig.Command.AddCommandLine("-Command \"Get-NetAdapter | Set-DnsClientServerAddress -ServerAddresses ",
                                                            this.PrivateIpAddress,
                                                            "\"");
                renameCommandConfig.WaitAfterCompletion = 0.ToString();
            }
        }
    }
}
