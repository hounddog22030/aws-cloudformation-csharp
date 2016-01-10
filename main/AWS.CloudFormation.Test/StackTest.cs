using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Instance;
using AWS.CloudFormation.Instance.Metadata;
using AWS.CloudFormation.Instance.Metadata.Config;
using AWS.CloudFormation.Instance.Metadata.Config.Command;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.ElasticLoadBalancing;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;

// created Stackd87ef590-0220-42e8-8e2d-880c9678d181

namespace AWS.CloudFormation.Test
{
    [TestClass]
    public class StackTest
    {
        const string CookbookFileName = "cookbooks-1452465964.tar.gz";
        // ReSharper disable once InconsistentNaming
        const string DomainAdminPassword = "kasdfiajs!!9";
        // ReSharper disable once InconsistentNaming
        const string DMZ1CIDR = "10.0.32.0/20";
        // ReSharper disable once InconsistentNaming
        const string DMZ2CIDR = "10.0.96.0/20";
        // ReSharper disable once InconsistentNaming
        const string PrivSub1CIDR = "10.0.0.0/19";
        // ReSharper disable once InconsistentNaming
        const string PrivSub2CIDR = "10.0.64.0/19";
        // ReSharper disable once InconsistentNaming
        const string SQL4TFSSUBNETCIDR = "10.0.5.0/24";
        // ReSharper disable once InconsistentNaming
        const string TFSSERVER1SUBNETCIDR = "10.0.6.0/24";
        // ReSharper disable once InconsistentNaming
        const string BUILDSERVER1SUBNETCIDR = "10.0.3.0/24";
        // ReSharper disable once InconsistentNaming
        const string WORKSTATIONSUBNETCIDR = "10.0.1.0/24";
        // ReSharper disable once InconsistentNaming
        const string KeyPairName = "corp.getthebuybox.com";
        // ReSharper disable once InconsistentNaming
        const string AD1PrivateIp = "10.0.0.10";
        // ReSharper disable once InconsistentNaming
        const string AD2PrivateIp = "10.0.64.10";
        // ReSharper disable once InconsistentNaming
        const string VPCCIDR = "10.0.0.0/16";
        // ReSharper disable once InconsistentNaming
        const string DomainDNSName = "prime.getthebuybox.com";
        // ReSharper disable once InconsistentNaming
        private const string DomainAdminUser = "johnny";
        // ReSharper disable once InconsistentNaming
        const string DomainNetBIOSName = "prime";
        // ReSharper disable once InconsistentNaming
        const string USEAST1AWINDOWS2012R2AMI = "ami-e4034a8e";
        // ReSharper disable once InconsistentNaming
        const string ADServerNetBIOSName1 = "dc1";
        const string SoftwareS3BucketName = "gtbb";
        static readonly TimeSpan ThreeHoursSpan = new TimeSpan(3, 0, 0);
        static readonly TimeSpan TwoHoursSpan = new TimeSpan(2, 0, 0);

        public static Template GetTemplate()
        {

            var template = new Template(KeyPairName);
            var vpc = template.AddVpc(template, DomainNetBIOSName, VPCCIDR);


            // ReSharper disable once InconsistentNaming
            var DMZSubnet = template.AddSubnet("DMZSubnet", vpc, DMZ1CIDR, Template.AvailabilityZone.UsEast1A);
            // ReSharper disable once InconsistentNaming
            var DMZ2Subnet = template.AddSubnet("DMZ2Subnet", vpc, DMZ2CIDR, Template.AvailabilityZone.UsEast1A);
            // ReSharper disable once InconsistentNaming
            var PrivateSubnet1 = template.AddSubnet("PrivateSubnet1", vpc, PrivSub1CIDR, Template.AvailabilityZone.UsEast1A);
            // ReSharper disable once InconsistentNaming
            var PrivateSubnet2 = template.AddSubnet("PrivateSubnet2", vpc, PrivSub2CIDR, Template.AvailabilityZone.UsEast1A);

            SecurityGroup natSecurityGroup = template.GetSecurityGroup("natSecurityGroup", vpc, "Enables Ssh access to NAT1 in AZ1 via port 22 and outbound internet access via private subnets");
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.Ssh);
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Icmp, Ports.All);

            SecurityGroup tfsServerSecurityGroup = template.GetSecurityGroup("TFSServerSecurityGroup", vpc, "Allows various TFS communication");
            tfsServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(DMZSubnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            tfsServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(DMZ2Subnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);

            SecurityGroup buildServerSecurityGroup = template.GetSecurityGroup("BuildServerSecurityGroup", vpc, "Allows build controller to build agent communication");
            buildServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(DMZSubnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            buildServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(DMZ2Subnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            buildServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(tfsServerSecurityGroup, Protocol.Tcp, Ports.TeamFoundationServerBuild);

            SecurityGroup sqlServerSecurityGroup = template.GetSecurityGroup("SqlServer4TfsSecurityGroup", vpc, "Allows communication to SQLServer Service");
            sqlServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(tfsServerSecurityGroup, Protocol.Tcp, Ports.MsSqlServer);
            sqlServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(DMZSubnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            sqlServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(DMZ2Subnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);

            SecurityGroup workstationSecurityGroup = template.GetSecurityGroup("WorkstationSecurityGroup", vpc, "Security Group To Contain Workstations");
            tfsServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(workstationSecurityGroup, Protocol.Tcp, Ports.TeamFoundationServerGeneral);
            



            InternetGateway gateway = template.AddInternetGateway("InternetGateway", vpc);
            AddInternetGatewayRouteTable(template, vpc, gateway, DMZSubnet);

            // ReSharper disable once InconsistentNaming
            RouteTable PrivateRouteTable = template.AddRouteTable("PrivateRouteTable", vpc);
            PrivateRouteTable.AddTag("Network", "AZ1 Private");

            // ReSharper disable once InconsistentNaming
            Route PrivateRoute = template.AddRoute("PrivateRoute", Template.CIDR_IP_THE_WORLD, PrivateRouteTable);

            SubnetRouteTableAssociation PrivateSubnetRouteTableAssociation1 = new SubnetRouteTableAssociation(    
                template,
                "PrivateSubnetRouteTableAssociation1", 
                PrivateSubnet1, 
                PrivateRouteTable);

            template.Resources.Add("PrivateSubnetRouteTableAssociation1", PrivateSubnetRouteTableAssociation1);


            Subnet[] subnetsToAddToNatSecurityGroup = new Subnet[] {PrivateSubnet1, PrivateSubnet2};

            foreach (var subnet in subnetsToAddToNatSecurityGroup)
            {
                natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(subnet, Protocol.All, Ports.Min, Ports.Max);
                natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(subnet, Protocol.Icmp, Ports.All);
            }

            var nat1 = AddNat1(template, DMZSubnet, natSecurityGroup);
            PrivateRoute.Instance = nat1;

            // ReSharper disable once InconsistentNaming
            var DomainController = new DomainController(template,
                ADServerNetBIOSName1,
                InstanceTypes.T2Micro,
                StackTest.USEAST1AWINDOWS2012R2AMI,
                PrivateSubnet1,
                AD1PrivateIp,
                new DomainController.DomainInfo(StackTest.DomainDNSName, StackTest.DomainNetBIOSName, StackTest.DomainAdminUser, StackTest.DomainAdminPassword));


            DomainController.CreateAdReplicationSubnet(DMZSubnet);
            DomainController.CreateAdReplicationSubnet(DMZ2Subnet);
            template.AddInstance(DomainController);

            // ReSharper disable once InconsistentNaming
            var RDGateway = new RemoteDesktopGateway(template, "RDGateway", InstanceTypes.T2Micro, StackTest.USEAST1AWINDOWS2012R2AMI, DMZSubnet);
            RDGateway.AddFinalizer(TwoHoursSpan);
            template.AddInstance(RDGateway);
            DomainController.AddToDomain(RDGateway, ThreeHoursSpan);

            var tfsSqlServer = new WindowsInstance(template, "sql1", InstanceTypes.T2Micro, StackTest.USEAST1AWINDOWS2012R2AMI, PrivateSubnet1);
            tfsSqlServer.AddBlockDeviceMapping("/dev/sda1", 70, "gp2");
            tfsSqlServer.AddBlockDeviceMapping("/dev/sdf", 50, "gp2");
            tfsSqlServer.AddBlockDeviceMapping("/dev/sdg", 20, "gp2");
            tfsSqlServer.AddChefExec(SoftwareS3BucketName, CookbookFileName, "SQL2014::express");
            tfsSqlServer.SecurityGroups.Add(sqlServerSecurityGroup);

            template.AddInstance(tfsSqlServer);
            DomainController.AddToDomain(tfsSqlServer, ThreeHoursSpan);

            var tfsServer = AddTfsServer(template, PrivateSubnet1, tfsSqlServer, DomainController, tfsServerSecurityGroup);
            tfsServer.AddChefExec(SoftwareS3BucketName, CookbookFileName, "TFS::applicationtier");

            var buildServer = AddBuildServer(template, PrivateSubnet1, tfsServer, DomainController, buildServerSecurityGroup);
            buildServer.AddChefExec(SoftwareS3BucketName, CookbookFileName, "TFS::build");

            var workstation = AddWorkstation(template, PrivateSubnet1, buildServer, DomainController, workstationSecurityGroup);
            workstation.AddChefExec(SoftwareS3BucketName, CookbookFileName, "VisualStudio");
            workstation.AddFinalizer(ThreeHoursSpan);


            // the below is a remote desktop gateway server that can
            // be uncommented to debug domain setup problems
            //var RDGateway2 = new RemoteDesktopGateway(template, "RDGateway2", InstanceTypes.T2Micro, "ami-e4034a8e", DMZSubnet);
            //dc1.AddToDomainMemberSecurityGroup(RDGateway2);
            //template.AddInstance(RDGateway2);

            LoadBalancer elb = new LoadBalancer(template,"elb1");
            elb.AddInstance(tfsServer);
            elb.AddListener("8080", "8080", "http");
            elb.AddSubnet(DMZSubnet);
            //elb.AddListener("8080", "8080", "https");
            template.AddResource(elb);


            return template;
        }

        private static WindowsInstance AddBuildServer(Template template, Subnet privateSubnet1, WindowsInstance tfsServer, DomainController DomainController, SecurityGroup buildServerSecurityGroup)
        {
            var buildServer = new WindowsInstance(template, "build", InstanceTypes.T2Small, StackTest.USEAST1AWINDOWS2012R2AMI, privateSubnet1);
            buildServer.AddBlockDeviceMapping("/dev/sda1", 30, "gp2");

            buildServer.AddDependsOn(tfsServer, ThreeHoursSpan);

            var chefNode = buildServer.GetChefNodeJsonContent(SoftwareS3BucketName, CookbookFileName);
            var domainAdminUserInfoNode = chefNode.AddNode("domainAdmin");
            domainAdminUserInfoNode.Add("name", DomainNetBIOSName + "\\" + DomainAdminUser);
            domainAdminUserInfoNode.Add("password", DomainAdminPassword);
            template.AddInstance(buildServer);
            buildServer.SecurityGroups.Add(buildServerSecurityGroup);
            DomainController.AddToDomain(buildServer, ThreeHoursSpan);
            return buildServer;
        }

        private static WindowsInstance AddWorkstation(Template template, Subnet privateSubnet1, Resource.EC2.Instance dependsOn, DomainController dc1, SecurityGroup workstationSecurityGroup)
        {
            var workstation = new WindowsInstance(template, "workstation", InstanceTypes.T2Small, StackTest.USEAST1AWINDOWS2012R2AMI, privateSubnet1);
            workstation.AddBlockDeviceMapping("/dev/sda1", 40, "gp2");
            workstation.AddBlockDeviceMapping("/dev/sdf", 20, "gp2");
            workstation.AddBlockDeviceMapping("/dev/sdg", 10, "gp2");

            workstation.AddDependsOn(dependsOn, ThreeHoursSpan);
            workstation.SecurityGroups.Add(workstationSecurityGroup);

            var chefNode = workstation.GetChefNodeJsonContent(SoftwareS3BucketName, CookbookFileName);
            var domainAdminUserInfoNode = chefNode.AddNode("domainAdmin");
            domainAdminUserInfoNode.Add("name", DomainNetBIOSName + "\\" + DomainAdminUser);
            domainAdminUserInfoNode.Add("password", DomainAdminPassword);
            template.AddInstance(workstation);

            dc1.AddToDomain(workstation, ThreeHoursSpan);
            return workstation;
        }

        private static WindowsInstance AddTfsServer(Template template, Subnet privateSubnet1, WindowsInstance tfsSqlServer, DomainController dc1, SecurityGroup tfsServerSecurityGroup)
        {
            var tfsServer = new WindowsInstance(template, "tfsserver1", InstanceTypes.T2Small, StackTest.USEAST1AWINDOWS2012R2AMI, privateSubnet1);
            tfsServer.AddDependsOn(tfsSqlServer, ThreeHoursSpan);

            var chefNode = tfsServer.GetChefNodeJsonContent(SoftwareS3BucketName, CookbookFileName);
            var domainAdminUserInfoNode = chefNode.AddNode("domainAdmin");
            domainAdminUserInfoNode.Add("name", DomainNetBIOSName + "\\" + DomainAdminUser);
            domainAdminUserInfoNode.Add("password", DomainAdminPassword);
            template.AddInstance(tfsServer);
            tfsServer.SecurityGroups.Add(tfsServerSecurityGroup);
            dc1.AddToDomain(tfsServer, ThreeHoursSpan);
            return tfsServer;
        }

        private static void AddInternetGatewayRouteTable(Template template, Vpc vpc, InternetGateway gateway, Subnet subnet)
        {
            RouteTable dmzRouteTable = template.AddRouteTable("DMZRouteTable", vpc);
            template.AddRoute("DMZRoute", gateway, "0.0.0.0/0", dmzRouteTable);
            SubnetRouteTableAssociation DMZSubnetRouteTableAssociation = new SubnetRouteTableAssociation(template,
                "DMZSubnetRouteTableAssociation" + subnet.Name, subnet, dmzRouteTable);
            template.AddResource(DMZSubnetRouteTableAssociation);
        }

        private static Resource.EC2.Instance AddNat1(   Template template, 
                                                    Subnet DMZSubnet,
                                                    SecurityGroup natSecurityGroup)
        {
            //SecurityGroup natSecurityGroup = template.GetSecurityGroup("natSecurityGroup", vpc,
            //    "Enables Ssh access to NAT1 in AZ1 via port 22 and outbound internet access via private subnets");

            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.Ssh);
            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Icmp, Ports.All);

            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(az1Subnet, Protocol.All, Ports.Min, Ports.Max);
            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(az1Subnet, Protocol.Icmp, Ports.All);

            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.All, Ports.Min, Ports.Max);
            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Icmp, Ports.All);

            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(SQL4TFSSubnet, Protocol.All, Ports.Min, Ports.Max);
            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(SQL4TFSSubnet, Protocol.Icmp, Ports.All);

            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(tfsServer1Subnet, Protocol.All, Ports.Min, Ports.Max);
            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(tfsServer1Subnet, Protocol.Icmp, Ports.All);

            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(buildServer1Subnet, Protocol.All, Ports.Min, Ports.Max);
            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(buildServer1Subnet, Protocol.Icmp, Ports.All);

            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(workstationSubnet, Protocol.All, Ports.Min, Ports.Max);
            //natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(workstationSubnet, Protocol.Icmp, Ports.All);

            var nat1 = new Resource.EC2.Instance(template,
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


        //private static void AddDomainControllerInitAndFinalize(DomainController domainController1)
        //{
        //    //var setup = domainController1.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("setup");

        //    //var setupFiles = setup.Files;

        //    //setupFiles.GetFile("c:\\cfn\\scripts\\Set-StaticIP.ps1")
        //    //    .Content.SetFnJoin(
        //    //        "$netip = Get-NetIPConfiguration;",
        //    //        "$ipconfig = Get-NetIPAddress | ?{$_.IpAddress -eq $netip.IPv4Address.IpAddress};",
        //    //        "Get-NetAdapter | Set-NetIPInterface -DHCP Disabled;",
        //    //        "Get-NetAdapter | New-NetIPAddress -AddressFamily IPv4 -IPAddress $netip.IPv4Address.IpAddress -PrefixLength $ipconfig.PrefixLength -DefaultGateway $netip.IPv4DefaultGateway.NextHop;",
        //    //        "Get-NetAdapter | Set-DnsClientServerAddress -ServerAddresses $netip.DNSServer.ServerAddresses;",
        //    //        "\n");

        //    //ConfigFile file = setupFiles.GetFile("c:\\cfn\\scripts\\New-LabADUser.ps1");
        //    //file.Source = "https://s3.amazonaws.com/CFN_WS2012_Scripts/AD/New-LabADUser.ps1";

        //    //file = setupFiles.GetFile("c:\\cfn\\scripts\\users.csv");
        //    //file.Source = "https://s3.amazonaws.com/CFN_WS2012_Scripts/AD/users.csv";

        //    //file = setupFiles.GetFile("c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1");
        //    //file.Source = "https://s3.amazonaws.com/quickstart-reference/microsoft/activedirectory/latest/scripts/ConvertTo-EnterpriseAdmin.ps1";

        //    ////powershell - Command "Get-NetFirewallProfile | Set-NetFirewallProfile - Enabled False" > c:\cfn\log\a-disable-win-fw.log

        //    //var disableFirewallCommand = setup.Commands.AddCommand<PowerShellCommand>("a-disable-win-fw");
        //    //disableFirewallCommand.WaitAfterCompletion = 0.ToString();
        //    //disableFirewallCommand.Command.AddCommandLine( new object[] { "-Command \"Get-NetFirewallProfile | Set-NetFirewallProfile -Enabled False\"" });

        //    //var setStaticIpCommand = domainController1.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("rename").Commands.AddCommand<PowerShellCommand>("a-set-static-ip");
        //    //setStaticIpCommand.WaitAfterCompletion = 45.ToString();
        //    //setStaticIpCommand.Command.AddCommandLine( "-ExecutionPolicy RemoteSigned -Command \"c:\\cfn\\scripts\\Set-StaticIP.ps1\"");

        //    var currentConfig = domainController1.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("installADDS");
        //    //var currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("1-install-prereqsz");

        //    //currentCommand.WaitAfterCompletion = 0.ToString();
        //    //currentCommand.Command.AddCommandLine( "-Command \"Install-WindowsFeature AD-Domain-Services, rsat-adds -IncludeAllSubFeature\"");

        //    //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("2-install-adds");
        //    //currentCommand.WaitAfterCompletion = "forever";
        //    //currentCommand.Command.AddCommandLine(
        //    //    "-Command \"Install-ADDSForest -DomainName XXXXXXXXXXXXXXXXXXXXXXX -SafeModeAdministratorPassword (convertto-securestring jhkjhsdf338! -asplaintext -force) -DomainMode Win2012 -DomainNetbiosName XXXXXXXXXXXXXXXXXXXXXX -ForestMode Win2012 -Confirm:$false -Force\"");


        //    //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("3-restart-service");
        //    //currentCommand.WaitAfterCompletion = 20.ToString();
        //    //currentCommand.Command.AddCommandLine(
        //    //    new object[]
        //    //    {
        //    //        "-Command \"Restart-Service NetLogon -EA 0\""
        //    //    }
        //    //    );

        //    //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("4 - create - adminuser");
        //    //currentCommand.WaitAfterCompletion = "0";
        //    //currentCommand.Command.AddCommandLine(
        //    //    new object[]
        //    //    {
        //    //    "-Command \"",
        //    //    "New-ADUser ",
        //    //    "-Name domainadminXXXXXXXXXXXXXXXX",
        //    //    //{
        //    //    //    "Ref" : "DomainAdminUser"
        //    //    //},
        //    //    " -UserPrincipalName ",
        //    //    " domainadminXXXXXXXXXXXXXXXX",
        //    //    //{
        //    //    //    "Ref" : "DomainAdminUser"
        //    //    //},
        //    //    "@XXXX.XXXXX.com",
        //    //    //{
        //    //    //    "Ref" : "DomainDNSName"
        //    //    //},
        //    //    " ",
        //    //    "-AccountPassword (ConvertTo-SecureString oldpassword123",
        //    //    //{
        //    //    //    "Ref" : "DomainAdminPassword"
        //    //    //},
        //    //    " -AsPlainText -Force) ",
        //    //    "-Enabled $true ",
        //    //    "-PasswordNeverExpires $true\""});

        //    //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("5 - update - adminuser");
        //    //currentCommand.WaitAfterCompletion = "0";
        //    //currentCommand.Command.AddCommandLine(
        //    //    new object[]
        //    //    {
        //    //        "-ExecutionPolicy RemoteSigned -Command \"c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1 -Members domainadminXXXXXXXXXXX\""
        //    //    });


        //    //currentConfig = domainController1.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("configureSites");
        //    //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("a-rename-default-site");
        //    //currentCommand.WaitAfterCompletion = 0.ToString();
        //    //currentCommand.Command.AddCommandLine(  " ",
        //    //                                        "\"",
        //    //                                        "Get-ADObject -SearchBase (Get-ADRootDSE).ConfigurationNamingContext -filter {Name -eq 'Default-First-Site-Name'} | Rename-ADObject -NewName AZ1",
        //    //                                        "\"" );

        //    //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("b-create-site-2");
        //    //currentCommand.WaitAfterCompletion = 0.ToString();
        //    //currentCommand.Command.AddCommandLine(  "\"",
        //    //                                        "New-ADReplicationSite AZ2",
        //    //                                        "\"");


        //    //var currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("c-create-DMZSubnet-1");
        //    //currentCommand.WaitAfterCompletion = 0.ToString();
        //    //currentCommand.Command.AddCommandLine(  "-Command New-ADReplicationSubnet -Name ",
        //    //                                        DMZSubnet,
        //    //                                        " -Site AZ1");

        //    //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("d-create-DMZSubnet-2");
        //    //currentCommand.WaitAfterCompletion = 0.ToString();
        //    //currentCommand.Command.AddCommandLine("-Command New-ADReplicationSubnet -Name ",
        //    //                                        DmzAz2Cidr,
        //    //                                        " -Site AZ2");

        //    //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("e-create-subnet-1");
        //    //currentCommand.WaitAfterCompletion = 0.ToString();
        //    //currentCommand.Command.AddCommandLine("-Command New-ADReplicationSubnet -Name ",
        //    //                                        Az1SubnetCidr,
        //    //                                        " -Site AZ1");

        //    //currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("f-create-subnet-2");
        //    //currentCommand.WaitAfterCompletion = 0.ToString();
        //    //currentCommand.Command.AddCommandLine("-Command New-ADReplicationSubnet -Name ",
        //    //                                        Az2SubnetCidr,
        //    //                                        " -Site AZ2");

        //    //var currentCommand = currentConfig.Commands.AddCommand<PowerShellCommand>("m-set-site-link");
        //    //currentCommand.WaitAfterCompletion = 0.ToString();
        //    //currentCommand.Command.AddCommandLine(  "-Command \"",
        //    //                                        "Get-ADReplicationSiteLink -Filter * | Set-ADReplicationSiteLink -SitesIncluded @{add='DMZ2Subnet'} -ReplicationFrequencyInMinutes 15\"");

        //}


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

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(DMZSubnet, Protocol.Tcp|Protocol.Udp, Ports.RemoteDesktopProtocol);

            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(DMZSubnet, Protocol.Icmp, Ports.All);
            domainControllerSg1.AddIngressEgress<SecurityGroupIngress>(dmzaz2Subnet, Protocol.Icmp, Ports.All);
        }

        //private static void AddNatSecurityGroupIngressRules(SecurityGroup natSecurityGroup, Subnet az1Subnet, Subnet az2Subnet,
        //    Subnet SQL4TFSSubnet, Subnet tfsServer1Subnet, Subnet buildServer1Subnet, Subnet workstationSubnet)
        //{

        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld,Protocol.Tcp, Ports.Ssh);
        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Icmp, Ports.All);

        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(az1Subnet, Protocol.All, Ports.Min,Ports.Max);
        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(az1Subnet, Protocol.Icmp, Ports.All);

        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(az2Subnet,Protocol.All, Ports.Min, Ports.Max);
        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(az2Subnet, Protocol.Icmp, Ports.All);

        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(SQL4TFSSubnet, Protocol.All, Ports.Min,Ports.Max);
        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(SQL4TFSSubnet,Protocol.Icmp, Ports.All);

        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(tfsServer1Subnet, Protocol.All, Ports.Min,Ports.Max);
        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(tfsServer1Subnet, Protocol.Icmp, Ports.All);

        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(buildServer1Subnet, Protocol.All, Ports.Min,Ports.Max);
        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(buildServer1Subnet,Protocol.Icmp, Ports.All);

        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(workstationSubnet, Protocol.All, Ports.Min,Ports.Max);
        //    natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(workstationSubnet, Protocol.Icmp, Ports.All);
        //}

        [TestMethod]
        public void CreateStackTest()
        {
            var t = new Stack.Stack();
            t.CreateStack(GetTemplate());
        }
        [TestMethod]
        public void UpdateStackTest()
        {
            var stackName = "Stackdf54b49a-3769-4c5b-8141-3e54ddd93df3";
            var t = new Stack.Stack();
            t.UpdateStack(stackName, GetTemplate());
        }




        [TestMethod]
        public void AddingSameResourceTwiceFails()
        {
            var t = new Template(Guid.NewGuid().ToString());
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
