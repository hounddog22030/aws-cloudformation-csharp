using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Instance.MetaData.Config;
using AWS.CloudFormation.Instance.MetaData.Config.Command;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Instance
{
    public class DomainController : WindowsInstance
    {
        public const string DomainAdminPasswordParameterName = "DomainAdminPassword";
        public const string DomainDnsNameParameterName = "DomainDNSName";


        public DomainController(Template template, string name, InstanceTypes instanceType, string imageId, string keyName, Subnet subnet, string domainController1PrivateIpAddress, ParameterBase domainDnsName) 
                : base(template, name, instanceType, imageId, keyName, subnet, null)
        {
            DomainMemberSecurityGroup = this.CreateDomainMemberSecurityGroup();
            this.CreateDomainControllerSecurityGroup();
            this.PrivateIpAddress = domainController1PrivateIpAddress;
            this.MakeDomainController(domainDnsName);
        }

        private void MakeDomainController(ParameterBase domainDnsName)
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
            file.Source = "https://s3.amazonaws.com/quickstart-reference/microsoft/activedirectory/latest/scripts/ConvertTo-EnterpriseAdmin.ps1";

            //powershell - Command "Get-NetFirewallProfile | Set-NetFirewallProfile - Enabled False" > c:\cfn\log\a-disable-win-fw.log

            var disableFirewallCommand = setup.Commands.AddCommand<PowerShellCommand>("a-disable-win-fw");
            disableFirewallCommand.WaitAfterCompletion = 0.ToString();
            disableFirewallCommand.Command.AddCommandLine(new object[] { "-Command \"Get-NetFirewallProfile | Set-NetFirewallProfile -Enabled False\"" });

            var setStaticIpCommand = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("rename").Commands.AddCommand<PowerShellCommand>("a-set-static-ip");
            setStaticIpCommand.WaitAfterCompletion = 15.ToString();
            setStaticIpCommand.Command.AddCommandLine("-ExecutionPolicy RemoteSigned -Command \"c:\\cfn\\scripts\\Set-StaticIP.ps1\"");

            var currentConfig = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("installADDS");
            var currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("1-install-prereqsz");

            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command.AddCommandLine("-Command \"Install-WindowsFeature AD-Domain-Services, rsat-adds -IncludeAllSubFeature\"");

            currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("2-install-adds");
            currentCommand.WaitAfterCompletion = "forever";
            
            currentCommand.Command.AddCommandLine(
                "-Command \"Install-ADDSForest -DomainName ",
                domainDnsName,
                " -SafeModeAdministratorPassword (convertto-securestring jhkjhsdf338! -asplaintext -force) -DomainMode Win2012 -DomainNetbiosName corp -ForestMode Win2012 -Confirm:$false -Force\"");


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
                "-Name johnny",
                //{
                //    "Ref" : "DomainAdminUser"
                //},
                " -UserPrincipalName ",
                " johnny",
                //{
                //    "Ref" : "DomainAdminUser"
                //},
                "@corp.getthebuybox.com",
                //{
                //    "Ref" : "DomainDNSName"
                //},
                " ",
                "-AccountPassword (ConvertTo-SecureString kasdfiajs!!9",
                //{
                //    "Ref" : "DomainAdminPassword"
                //},
                " -AsPlainText -Force) ",
                "-Enabled $true ",
                "-PasswordNeverExpires $true\""});

            currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("5 - update - adminuser");
            currentCommand.WaitAfterCompletion = "0";
            currentCommand.Command.AddCommandLine(
                new object[]
                {
                    "-ExecutionPolicy RemoteSigned -Command \"c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1 -Members johnny\""
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
            SecurityGroup DomainControllerSG1 = Template.GetSecurityGroup("DomainControllerSG1", this.Vpc, "Domain Controller");
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(this.Vpc, Protocol.Tcp, Ports.WsManagementPowerShell);
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
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Udp, Ports.Ntp);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp, Ports.WinsManager);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp, Ports.ActiveDirectoryManagement);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Udp, Ports.NetBios);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp | Protocol.Udp, Ports.Smb);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp | Protocol.Udp, Ports.ActiveDirectoryManagement2);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp | Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp | Protocol.Udp, Ports.Ldap);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp, Ports.Ldaps);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp, Ports.Ldap2Begin, Ports.Ldap2End);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp | Protocol.Udp, Ports.DnsQuery);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp | Protocol.Udp, Ports.KerberosKeyDistribution);
            DomainControllerSG1.AddIngressEgress<SecurityGroupIngress>(DomainMemberSecurityGroup, Protocol.Tcp | Protocol.Udp, Ports.Rdp);

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
            SecurityGroup domainMemberSg = Template.GetSecurityGroup("DomainMemberSG", this.Vpc, "For All Domain Members");
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
            DomainMemberSecurityGroup.AddIngressEgress<SecurityGroupIngress>(domainMember.Subnet, Protocol.Tcp | Protocol.Udp, Ports.DnsQuery);
            DomainMemberSecurityGroup.AddIngressEgress<SecurityGroupIngress>(domainMember.Subnet, Protocol.Tcp | Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
            //DMZSubnet
            // this is questionable overkill
            DomainMemberSecurityGroup.AddIngressEgress<SecurityGroupIngress>(domainMember.Subnet, Protocol.Tcp, Ports.Rdp);
            DomainMemberSecurityGroup.AddIngressEgress<SecurityGroupIngress>(domainMember.Subnet, Protocol.Tcp, Ports.Rdp);

            domainMember.SecurityGroups.Add(DomainMemberSecurityGroup);
        }

        [JsonIgnore]
        public SecurityGroup DomainMemberSecurityGroup { get; }

    }
}
