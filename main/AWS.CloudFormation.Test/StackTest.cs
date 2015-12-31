using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Instance;
using AWS.CloudFormation.Instance.Metadata.Config;
using AWS.CloudFormation.Instance.Metadata.Config.Command;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperatingSystem = AWS.CloudFormation.Instance.OperatingSystem;

// created Stackfe0c756c-8972-4c36-930f-262a8002b2bb

namespace AWS.CloudFormation.Test
{
    [TestClass]
    public class StackTest
    {
        // ReSharper disable once InconsistentNaming
        const string DMZ1CIDR = "10.0.0.0/24";
        const string Az1SubnetCidr = "10.0.4.0/24";
        const string Az2SubnetCidr = "10.0.12.0/24";
        const string SqlServer1SubnetCidr = "10.0.5.0/24";
        const string TfsServer1SubnetCidr = "10.0.6.0/24";
        const string BuildServer1SubnetCidr = "10.0.3.0/24";
        const string WorkstationSubnetCidr = "10.0.1.0/24";
        const string DmzAz2Cidr = "10.0.8.0/24";
        const string DefaultEncryptionKeyName = "corp.getthebuybox.com";
        const string DomainController1PrivateIpAddress = "10.0.4.20";
        const string DomainController2PrivateIpAddress = "10.0.8.20";
        const string VpcCidrBlock = "10.0.0.0/16";
        const string DomainDNSName = "prime.getthebuybox.com";
        const string DomainAdminUser = "johnny";
        const string DomainAdminPassword = "kasdfiajs!!9";
        const string DomainNetBIOSName = "prime";

        private static Template GetTemplate()
        {

            var template = new Template();

            // parameters
            // ReSharper disable once InconsistentNaming
            var ParameterNameDefaultEncryptionKey = new ParameterBase(Instance.Instance.ParameterNameDefaultKeyPairKeyName, "AWS::EC2::KeyPair::KeyName", DefaultEncryptionKeyName);
            template.AddParameter(ParameterNameDefaultEncryptionKey);

            var vpc = new Vpc(template, "VPC", VpcCidrBlock);
            template.AddVpc(template, "VPC", VpcCidrBlock);

            var DMZSubnet = template.AddSubnet("DMZSubnet", vpc, DMZ1CIDR, Template.AvailabilityZone.UsEast1A);
            var DMZ2Subnet = template.AddSubnet("DMZ2Subnet", vpc, DmzAz2Cidr, Template.AvailabilityZone.UsEast1A);
            var az1Subnet = template.AddSubnet("az1Subnet", vpc, Az1SubnetCidr, Template.AvailabilityZone.UsEast1A);
            var az2Subnet = template.AddSubnet("az2Subnet", vpc, Az2SubnetCidr, Template.AvailabilityZone.UsEast1A);
            var sqlServer1Subnet = template.AddSubnet("sqlServer1Subnet", vpc, SqlServer1SubnetCidr, Template.AvailabilityZone.UsEast1A);
            var tfsServer1Subnet = template.AddSubnet("tfsServer1Subnet", vpc, TfsServer1SubnetCidr, Template.AvailabilityZone.UsEast1A);
            var buildServer1Subnet = template.AddSubnet("buildServer1Subnet", vpc, BuildServer1SubnetCidr, Template.AvailabilityZone.UsEast1A);
            var workstationSubnet = template.AddSubnet("workstationSubnet", vpc, WorkstationSubnetCidr, Template.AvailabilityZone.UsEast1A);

            InternetGateway gateway = template.AddInternetGateway("VpcInternetGateway", vpc);
            AddRouteTables(template, vpc, gateway, DMZSubnet);

            RouteTable az1PrivateRouteTable = template.AddRouteTable("az1PrivateRouteTable", vpc);
            Route nat1PrivateRoute = template.AddRoute("NAT1PrivateRoute", Template.CIDR_IP_THE_WORLD, az1PrivateRouteTable);


            var nat1 = AddNat1(template, vpc, az1Subnet, az2Subnet, sqlServer1Subnet, tfsServer1Subnet, buildServer1Subnet, workstationSubnet, DefaultEncryptionKeyName, DMZSubnet);
            nat1PrivateRoute.Instance = nat1;

            SubnetRouteTableAssociation az1PrivateSubnetRouteTableAssociation = new SubnetRouteTableAssociation(template,"AZ1PrivateSubnetRouteTableAssociation", az1Subnet, az1PrivateRouteTable);
            template.Resources.Add("AZ1PrivateSubnetRouteTableAssociation", az1PrivateSubnetRouteTableAssociation);

            // ReSharper disable once InconsistentNaming
            var DC1 = new Instance.DomainController(template,
                "DC1",
                InstanceTypes.T2Micro,
                "ami-e4034a8e",
                az1Subnet,
                DomainController1PrivateIpAddress,
                new DomainController.DomainInfo(StackTest.DomainDNSName, StackTest.DomainNetBIOSName,
                    StackTest.DomainAdminUser, StackTest.DomainAdminPassword));


            //domainController1.SecurityGroups.Add(domainControllerSg1);
            DC1.CreateAdReplicationSubnet(DMZSubnet);
            DC1.CreateAdReplicationSubnet(DMZ2Subnet);
            //DC1.CreateAdReplicationSubnet(az1Subnet);
            //DC1.CreateAdReplicationSubnet(az2Subnet);
            template.AddInstance(DC1);

            // ReSharper disable once InconsistentNaming
            var RDGateway = new RemoteDesktopGateway(template, "RDGateway", InstanceTypes.T2Micro, "ami-e4034a8e", DMZSubnet);
            RDGateway.AddFinalizer(new TimeSpan(0,120,0));
            template.AddInstance(RDGateway);
            DC1.AddToDomain(RDGateway);

            //SecurityGroup domainMemberSg = domainController1.DomainMemberSecurityGroup;
            //instanceRdgw1.SecurityGroups.Add(domainMemberSg);
            //domainController1.AddToDomainMemberSecurityGroup(instanceRdgw1);


            //var rdgq1Config = instanceRdgw1.Metadata.Init.ConfigSets.GetConfigSet("config");
            //var rdgq1ConfigJoin = rdgq1Config.GetConfig("join");
            //var setDnsServers = rdgq1ConfigJoin.Commands.AddCommand<PowerShellCommand>("a-set-dns-servers");
            //setDnsServers.Command.AddCommandLine("-Command \"Get-NetAdapter | Set-DnsClientServerAddress -ServerAddresses ",
            //    DomainController1PrivateIpAddress,
            //    ",",
            //    DomainController2PrivateIpAddress,
            //    "\"");
            //setDnsServers.WaitAfterCompletion = 30.ToString();

            //var joinDomain = rdgq1ConfigJoin.Commands.AddCommand<PowerShellCommand>("b-join-domain");
            //joinDomain.Command.AddCommandLine("-Command \"",
            //                        "Add-Computer -DomainName ",
            //                        DomainDNSName,
            //                        " -Credential ",
            //                        "(New-Object System.Management.Automation.PSCredential('",
            //                        DomainNetBIOSName,
            //                        "\\",
            //                        DomainAdminUser,
            //                        "',",
            //                        "(ConvertTo-SecureString ",
            //                        DomainAdminPassword,
            //                        " -AsPlainText -Force))) ",
            //                        "-Restart\"");
            //joinDomain.WaitAfterCompletion = "forever";

            //joinDomain = rdgq1ConfigJoin.Commands.AddCommand<PowerShellCommand>("b-join-domainX");
            //joinDomain.Command.AddCommandLine("dir");
            //joinDomain.WaitAfterCompletion = 0.ToString();


            // the below is a remote desktop gateway server that can
            // be uncommented to debug domain setup problems
            //var RDGateway2 = new RemoteDesktopGateway(template, "RDGateway2", InstanceTypes.T2Micro, "ami-e4034a8e", EncryptionKeyName, DMZSubnet);
            //DC1.AddToDomainMemberSecurityGroup(RDGateway2);
            //template.AddInstance(RDGateway2);

            return template;
        }

        private static void AddRouteTables(Template template, Vpc vpc, InternetGateway gateway, Subnet subnet)
        {
            RouteTable dmzRouteTable = template.AddRouteTable("DMZRouteTable", vpc);
            template.AddRoute("DMZRoute", gateway, "0.0.0.0/0", dmzRouteTable);
            SubnetRouteTableAssociation DMZSubnetRouteTableAssociation = new SubnetRouteTableAssociation(template,
                "DMZSubnetRouteTableAssociation", subnet, dmzRouteTable);
            template.AddResource(DMZSubnetRouteTableAssociation);
        }

        private static Instance.Instance AddNat1(Template template, Vpc vpc, Subnet az1Subnet, Subnet az2Subnet, Subnet sqlServer1Subnet,
            Subnet tfsServer1Subnet, Subnet buildServer1Subnet, Subnet workstationSubnet, string encryptionKeyName,
            Subnet DMZSubnet)
        {
            SecurityGroup natSecurityGroup = template.GetSecurityGroup("natSecurityGroup", vpc,
                "Enables Ssh access to NAT1 in AZ1 via port 22 and outbound internet access via private subnets");
            AddNatSecurityGroupIngressRules(natSecurityGroup, az1Subnet, az2Subnet, sqlServer1Subnet, tfsServer1Subnet,
                buildServer1Subnet, workstationSubnet);

            var nat1 = new Instance.Instance(template,
                "NAT1",
                InstanceTypes.T2Micro,
                "ami-4c9e4b24",
                OperatingSystem.Linux, 
                false)
            {
                SourceDestCheck = false
            };

            var natNetworkInterface = new NetworkInterface(DMZSubnet)
            {
                AssociatePublicIpAddress = true,
                DeviceIndex = 0,
                DeleteOnTermination = true
            };
            natNetworkInterface.GroupSet.Add(natSecurityGroup);
            nat1.NetworkInterfaces.Add(natNetworkInterface);
            template.AddInstance(nat1);
            return nat1;
        }

        //private static void AddSecurityGroups(Template template, Vpc vpc, Subnet az1Subnet, Subnet DMZSubnet,
        //    Subnet dmzaz2Subnet, Subnet az2Subnet, RouteTable az1PrivateRouteTable)
        //{
        //    //SecurityGroup domainMemberSg = template.AddSecurityGroup("DomainMemberSG", vpc, "For All Domain Members");
        //    //domainMemberSg.GroupDescription = "Domain Member Security Group";
        //    ////az1Subnet
        //    //domainMemberSg.AddIngressEgress<SecurityGroupIngress>(az1Subnet, Protocol.Tcp | Protocol.Udp, Ports.DnsQuery);
        //    //domainMemberSg.AddIngressEgress<SecurityGroupIngress>(az1Subnet, Protocol.Tcp | Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
        //    ////DMZSubnet
        //    //domainMemberSg.AddIngressEgress<SecurityGroupIngress>(DMZSubnet, Protocol.Tcp, Ports.Rdp);
        //    //domainMemberSg.AddIngressEgress<SecurityGroupIngress>(dmzaz2Subnet, Protocol.Tcp, Ports.Rdp);


        //    SubnetRouteTableAssociation az1PrivateSubnetRouteTableAssociation = new SubnetRouteTableAssociation(template,
        //        "AZ1PrivateSubnetRouteTableAssociation", az1Subnet, az1PrivateRouteTable);
        //    template.Resources.Add("AZ1PrivateSubnetRouteTableAssociation", az1PrivateSubnetRouteTableAssociation);

        //    //return domainMemberSg;
        //}

        //private static SecurityGroup AddDomainControllerSecurityGroup(Template template, Vpc vpc, Subnet DMZSubnet,
        //    Subnet dmzaz2Subnet, Subnet az2Subnet, SecurityGroup domainMemberSg)
        //{
        //    SecurityGroup domainControllerSg1 = template.AddSecurityGroup("domainControllerSg1", vpc,
        //        "Domain Controller Security Group");
        //    SetupDomainController1SecurityGround(domainControllerSg1, vpc, az2Subnet, domainMemberSg, DMZSubnet, dmzaz2Subnet);
        //    return domainControllerSg1;
        //}


        private static void AddDomainControllerInitAndFinalize(DomainController domainController1)
        {
            //var setup = domainController1.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("setup");

            //var setupFiles = setup.Files;

            //setupFiles.GetFile("c:\\cfn\\scripts\\Set-StaticIP.ps1")
            //    .Content.SetFnJoin(
            //        "$netip = Get-NetIPConfiguration;",
            //        "$ipconfig = Get-NetIPAddress | ?{$_.IpAddress -eq $netip.IPv4Address.IpAddress};",
            //        "Get-NetAdapter | Set-NetIPInterface -DHCP Disabled;",
            //        "Get-NetAdapter | New-NetIPAddress -AddressFamily IPv4 -IPAddress $netip.IPv4Address.IpAddress -PrefixLength $ipconfig.PrefixLength -DefaultGateway $netip.IPv4DefaultGateway.NextHop;",
            //        "Get-NetAdapter | Set-DnsClientServerAddress -ServerAddresses $netip.DNSServer.ServerAddresses;",
            //        "\n");

            //ConfigFile file = setupFiles.GetFile("c:\\cfn\\scripts\\New-LabADUser.ps1");
            //file.Source = "https://s3.amazonaws.com/CFN_WS2012_Scripts/AD/New-LabADUser.ps1";

            //file = setupFiles.GetFile("c:\\cfn\\scripts\\users.csv");
            //file.Source = "https://s3.amazonaws.com/CFN_WS2012_Scripts/AD/users.csv";

            //file = setupFiles.GetFile("c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1");
            //file.Source = "https://s3.amazonaws.com/quickstart-reference/microsoft/activedirectory/latest/scripts/ConvertTo-EnterpriseAdmin.ps1";

            ////powershell - Command "Get-NetFirewallProfile | Set-NetFirewallProfile - Enabled False" > c:\cfn\log\a-disable-win-fw.log

            //var disableFirewallCommand = setup.Commands.AddCommand<PowerShellCommand>("a-disable-win-fw");
            //disableFirewallCommand.WaitAfterCompletion = 0.ToString();
            //disableFirewallCommand.Command.AddCommandLine( new object[] { "-Command \"Get-NetFirewallProfile | Set-NetFirewallProfile -Enabled False\"" });

            //var setStaticIpCommand = domainController1.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("rename").Commands.AddCommand<PowerShellCommand>("a-set-static-ip");
            //setStaticIpCommand.WaitAfterCompletion = 45.ToString();
            //setStaticIpCommand.Command.AddCommandLine( "-ExecutionPolicy RemoteSigned -Command \"c:\\cfn\\scripts\\Set-StaticIP.ps1\"");

            var currentConfig = domainController1.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("installADDS");
            //var currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("1-install-prereqsz");

            //currentCommand.WaitAfterCompletion = 0.ToString();
            //currentCommand.Command.AddCommandLine( "-Command \"Install-WindowsFeature AD-Domain-Services, rsat-adds -IncludeAllSubFeature\"");

            //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("2-install-adds");
            //currentCommand.WaitAfterCompletion = "forever";
            //currentCommand.Command.AddCommandLine(
            //    "-Command \"Install-ADDSForest -DomainName XXXXXXXXXXXXXXXXXXXXXXX -SafeModeAdministratorPassword (convertto-securestring jhkjhsdf338! -asplaintext -force) -DomainMode Win2012 -DomainNetbiosName XXXXXXXXXXXXXXXXXXXXXX -ForestMode Win2012 -Confirm:$false -Force\"");


            //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("3-restart-service");
            //currentCommand.WaitAfterCompletion = 20.ToString();
            //currentCommand.Command.AddCommandLine(
            //    new object[]
            //    {
            //        "-Command \"Restart-Service NetLogon -EA 0\""
            //    }
            //    );

            //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("4 - create - adminuser");
            //currentCommand.WaitAfterCompletion = "0";
            //currentCommand.Command.AddCommandLine(
            //    new object[]
            //    {
            //    "-Command \"",
            //    "New-ADUser ",
            //    "-Name domainadminXXXXXXXXXXXXXXXX",
            //    //{
            //    //    "Ref" : "DomainAdminUser"
            //    //},
            //    " -UserPrincipalName ",
            //    " domainadminXXXXXXXXXXXXXXXX",
            //    //{
            //    //    "Ref" : "DomainAdminUser"
            //    //},
            //    "@XXXX.XXXXX.com",
            //    //{
            //    //    "Ref" : "DomainDNSName"
            //    //},
            //    " ",
            //    "-AccountPassword (ConvertTo-SecureString oldpassword123",
            //    //{
            //    //    "Ref" : "DomainAdminPassword"
            //    //},
            //    " -AsPlainText -Force) ",
            //    "-Enabled $true ",
            //    "-PasswordNeverExpires $true\""});

            //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("5 - update - adminuser");
            //currentCommand.WaitAfterCompletion = "0";
            //currentCommand.Command.AddCommandLine(
            //    new object[]
            //    {
            //        "-ExecutionPolicy RemoteSigned -Command \"c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1 -Members domainadminXXXXXXXXXXX\""
            //    });


            //currentConfig = domainController1.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("configureSites");
            //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("a-rename-default-site");
            //currentCommand.WaitAfterCompletion = 0.ToString();
            //currentCommand.Command.AddCommandLine(  " ",
            //                                        "\"",
            //                                        "Get-ADObject -SearchBase (Get-ADRootDSE).ConfigurationNamingContext -filter {Name -eq 'Default-First-Site-Name'} | Rename-ADObject -NewName AZ1",
            //                                        "\"" );

            //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("b-create-site-2");
            //currentCommand.WaitAfterCompletion = 0.ToString();
            //currentCommand.Command.AddCommandLine(  "\"",
            //                                        "New-ADReplicationSite AZ2",
            //                                        "\"");


            //var currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("c-create-DMZSubnet-1");
            //currentCommand.WaitAfterCompletion = 0.ToString();
            //currentCommand.Command.AddCommandLine(  "-Command New-ADReplicationSubnet -Name ",
            //                                        DMZSubnet,
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

            //var currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("m-set-site-link");
            //currentCommand.WaitAfterCompletion = 0.ToString();
            //currentCommand.Command.AddCommandLine(  "-Command \"",
            //                                        "Get-ADReplicationSiteLink -Filter * | Set-ADReplicationSiteLink -SitesIncluded @{add='DMZ2Subnet'} -ReplicationFrequencyInMinutes 15\"");

        }


        private static void SetupDomainController1SecurityGround(SecurityGroup domainControllerSg1, Vpc vpc, Subnet az2Subnet, SecurityGroup domainMemberSg, Subnet DMZSubnet, Subnet dmzaz2Subnet)
        {
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(vpc, Protocol.Tcp, Ports.WsManagementPowerShell);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Udp,Ports.Ntp);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.WinsManager);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.ActiveDirectoryManagement);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Udp, Ports.NetBios);

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet,Protocol.Tcp|Protocol.Udp, Ports.Smb);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp|Protocol.Udp, Ports.ActiveDirectoryManagement2);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp|Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp|Protocol.Udp, Ports.Ldap);

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.Ldaps);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.Ldap2Begin, Ports.Ldap2End);

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp|Protocol.Udp, Ports.DnsQuery);

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.ActiveDirectoryManagement);

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp|Protocol.Udp, Ports.KerberosKeyDistribution);

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Udp, Ports.DnsLlmnr);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Udp, Ports.NetBt);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.NetBiosNameServices);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Tcp, Ports.ActiveDirectoryFileReplication);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(domainMemberSg, Protocol.Udp,Ports.Ntp);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(domainMemberSg,Protocol.Tcp,Ports.WinsManager);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(domainMemberSg,Protocol.Tcp,Ports.ActiveDirectoryManagement);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(domainMemberSg,Protocol.Udp, Ports.NetBios);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(domainMemberSg,Protocol.Tcp|Protocol.Udp, Ports.Smb);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(domainMemberSg,Protocol.Tcp| Protocol.Udp, Ports.ActiveDirectoryManagement2);

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(domainMemberSg,Protocol.Tcp|Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(domainMemberSg,Protocol.Tcp|Protocol.Udp, Ports.Ldap);

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(domainMemberSg,Protocol.Tcp, Ports.Ldaps);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(domainMemberSg,Protocol.Tcp, Ports.Ldap2Begin, Ports.Ldap2End);

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(domainMemberSg,Protocol.Tcp|Protocol.Udp, Ports.DnsQuery);

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(domainMemberSg,Protocol.Tcp|Protocol.Udp,Ports.KerberosKeyDistribution);

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(DMZSubnet, Protocol.Tcp|Protocol.Udp, Ports.Rdp);

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(DMZSubnet, Protocol.Icmp, Ports.All);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(dmzaz2Subnet, Protocol.Icmp, Ports.All);
        }

        private static void AddNatSecurityGroupIngressRules(SecurityGroup natSecurityGroup, Subnet az1Subnet, Subnet az2Subnet,
            Subnet sqlServer1Subnet, Subnet tfsServer1Subnet, Subnet buildServer1Subnet, Subnet workstationSubnet)
        {

            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld,Protocol.Tcp, Ports.Ssh);

            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Icmp, Ports.All);

            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(az1Subnet, Protocol.All, Ports.Min,Ports.Max);

            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(az1Subnet, Protocol.Icmp, Ports.All);
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(az2Subnet,Protocol.All, Ports.Min, Ports.Max);
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Icmp, Ports.All);
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(sqlServer1Subnet, Protocol.All, Ports.Min,Ports.Max);
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(sqlServer1Subnet,Protocol.Icmp, Ports.All);
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(tfsServer1Subnet, Protocol.All, Ports.Min,Ports.Max);
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(tfsServer1Subnet, Protocol.Icmp, Ports.All);
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(buildServer1Subnet, Protocol.All, Ports.Min,Ports.Max);
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(buildServer1Subnet,Protocol.Icmp, Ports.All);
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(workstationSubnet, Protocol.All, Ports.Min,Ports.Max);
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(workstationSubnet, Protocol.Icmp, Ports.All);
        }

        [TestMethod]
        public void CreateStackTest()
        {
            var t = new Stack.Stack();
            t.CreateStack(GetTemplate());
        }
        [TestMethod]
        public void UpdateStackTest()
        {
            var stackName = "Stack8c949017-cf24-48ff-b973-790d05cba9be";
            var t = new Stack.Stack();
            t.UpdateStack(stackName, GetTemplate());
        }




        [TestMethod]
        public void AddingSameResourceTwiceFails()
        {
            var t = new Template();
            var v = new Vpc(t,"X","10.0.0.0/16");
            var s = t.AddSubnet("Vpc1",v,null,Template.AvailabilityZone.UsEast1A);

            ArgumentException expectedException = null;

            try
            {
                t.AddSubnet("Vpc1", v, null, Template.AvailabilityZone.UsEast1A);
            }
            catch (ArgumentException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
        }
    }
}
