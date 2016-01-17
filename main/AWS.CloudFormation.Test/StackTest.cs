using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Configuration.Packages;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;

// created Stackd87ef590-0220-42e8-8e2d-880c9678d181

namespace AWS.CloudFormation.Test
{
    [TestClass]
    public class StackTest
    {
        const string CookbookFileName = "cookbooks-1452723974.tar.gz";
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
        static readonly TimeSpan MaxTimeOut = new TimeSpan(0, 0, 43200);

        const string BuildServerIpAddress = "10.0.12.85";

        public static Template GetTemplateFullStack(TestContext testContext)
        {

            var template = GetNewBlankTemplateWithVpc(testContext);
            Vpc vpc = template.Vpcs.First();


            // ReSharper disable once InconsistentNaming
            var DMZSubnet = template.AddSubnet("DMZSubnet", vpc, DMZ1CIDR, Template.AvailabilityZone.UsEast1A);
            // ReSharper disable once InconsistentNaming
            var DMZ2Subnet = template.AddSubnet("DMZ2Subnet", vpc, DMZ2CIDR, Template.AvailabilityZone.UsEast1A);
            // ReSharper disable once InconsistentNaming
            var PrivateSubnet1 = template.AddSubnet("PrivateSubnet1", vpc, PrivSub1CIDR, Template.AvailabilityZone.UsEast1A);
            // ReSharper disable once InconsistentNaming
            var PrivateSubnet2 = template.AddSubnet("PrivateSubnet2", vpc, PrivSub2CIDR, Template.AvailabilityZone.UsEast1A);

            SecurityGroup elbSecurityGroup = template.GetSecurityGroup("ElbSecurityGroup", vpc, "Enables access to the ELB");
            elbSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.TeamFoundationServerHttp);

            SecurityGroup natSecurityGroup = template.GetSecurityGroup("natSecurityGroup", vpc, "Enables Ssh access to NAT1 in AZ1 via port 22 and outbound internet access via private subnets");
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.Ssh);
            natSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Icmp, Ports.All);

            SecurityGroup tfsServerUsers = template.GetSecurityGroup("TFSUsers", vpc, "Security Group To Contain Users of the TFS Services");

            SecurityGroup tfsServerSecurityGroup = template.GetSecurityGroup("TFSServerSecurityGroup", vpc, "Allows various TFS communication");
            tfsServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(DMZSubnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            tfsServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(DMZ2Subnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            tfsServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(tfsServerUsers, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            tfsServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(elbSecurityGroup, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            


            SecurityGroup buildServerSecurityGroup = template.GetSecurityGroup("BuildServerSecurityGroup", vpc, "Allows build controller to build agent communication");
            buildServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(DMZSubnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            buildServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(DMZ2Subnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            buildServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(tfsServerSecurityGroup, Protocol.Tcp, Ports.TeamFoundationServerBuild);

            SecurityGroup sqlServerSecurityGroup = template.GetSecurityGroup("SqlServer4TfsSecurityGroup", vpc, "Allows communication to SQLServer Service");
            sqlServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(tfsServerSecurityGroup, Protocol.Tcp, Ports.MsSqlServer);
            sqlServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(DMZSubnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            sqlServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(DMZ2Subnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);

            SecurityGroup workstationSecurityGroup = template.GetSecurityGroup("WorkstationSecurityGroup", vpc, "Security Group To Contain Workstations");
            tfsServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(workstationSecurityGroup, Protocol.Tcp, Ports.TeamFoundationServerHttp);







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
            // uses 21gb
            var DomainController = AddDomainController(template, PrivateSubnet1);
            DomainController.CreateAdReplicationSubnet(DMZSubnet);
            DomainController.CreateAdReplicationSubnet(DMZ2Subnet);

            // uses 19gb
            // ReSharper disable once InconsistentNaming
            var RDGateway = new RemoteDesktopGateway(template, "RDGateway", InstanceTypes.T2Micro, StackTest.USEAST1AWINDOWS2012R2AMI, DMZSubnet);
            RDGateway.AddFinalizer(TwoHoursSpan);
            template.AddInstance(RDGateway);
            DomainController.AddToDomain(RDGateway, ThreeHoursSpan);

            //// uses 25gb
            //var tfsSqlServer = new WindowsInstance(template, "Sql4Tfs", InstanceTypes.T2Micro, StackTest.USEAST1AWINDOWS2012R2AMI, PrivateSubnet1, true);
            //DomainController.AddToDomain(tfsSqlServer, ThreeHoursSpan);
            ////tfsSqlServer.AddBlockDeviceMapping("/dev/sda1", 70, "gp2");
            ////tfsSqlServer.AddBlockDeviceMapping("/dev/sdf", 50, "gp2");
            ////tfsSqlServer.AddBlockDeviceMapping("/dev/sdg", 20, "gp2");
            ////tfsSqlServer.AddPackage(SoftwareS3BucketName, new SqlServerExpress());
            ////tfsSqlServer.SecurityGroups.Add(sqlServerSecurityGroup);
            //template.AddInstance(tfsSqlServer);

            //// uses 24gb
            //var tfsServer = AddTfsServer(template, PrivateSubnet1, tfsSqlServer, DomainController, tfsServerSecurityGroup);
            //tfsServer.AddChefExec(SoftwareS3BucketName, CookbookFileName, "TFS::applicationtier");
            //tfsServer.AddBlockDeviceMapping("/dev/sda1", 214, "gp2");

            //var disableFirewallConfigSet = tfsServer.Metadata.Init.ConfigSets.GetConfigSet("disable-win-fw-configSet");
            //var disableFirewallConfig = disableFirewallConfigSet.GetConfig("disable-win-fw-configSet");

            //var disableFirewallCommand = disableFirewallConfig.Commands.AddCommand<PowerShellCommand>("disable-win-fw-command");
            //disableFirewallCommand.WaitAfterCompletion = 0.ToString();
            //disableFirewallCommand.Command.AddCommandLine(new object[] { "-Command \"Get-NetFirewallProfile | Set-NetFirewallProfile -Enabled False\"" });


            //// uses 24gb
            //var buildServer = AddBuildServer(template, PrivateSubnet1, tfsServer, DomainController, buildServerSecurityGroup, IPNetwork.Parse(BuildServerIpAddress + "/32"));
            //buildServer.AddFinalizer(ThreeHoursSpan);
            //tfsServerSecurityGroup.AddIngressEgress<SecurityGroupIngress>(buildServer, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            //buildServer.SecurityGroups.Add(tfsServerUsers);



            //// uses 33gb
            //var workstation = AddWorkstation(template, "workstation3", PrivateSubnet1, buildServer, DomainController, workstationSecurityGroup, tfsServerUsers, true);
            ////var workstation2 = AddWorkstation(template, "workstation2", PrivateSubnet1, buildServer, DomainController, workstationSecurityGroup, tfsServerUsers);


            //// the below is a remote desktop gateway server that can
            //// be uncommented to debug domain setup problems
            ////var RDGateway2 = new RemoteDesktopGateway(template, "RDGateway2", InstanceTypes.T2Micro, "ami-e4034a8e", DMZSubnet);
            ////dc1.AddToDomainMemberSecurityGroup(RDGateway2);
            ////template.AddInstance(RDGateway2);

            //LoadBalancer elb = new LoadBalancer(template, "elb1");
            //elb.AddInstance(tfsServer);
            //elb.AddListener("8080", "8080", "http");
            //elb.AddSubnet(DMZSubnet);
            //elb.SecurityGroups.Add(elbSecurityGroup);
            //template.AddResource(elb);


            return template;
        }

        private static DomainController AddDomainController(Template template, Subnet subnet)
        {
            var DomainController = new DomainController(template,
                ADServerNetBIOSName1,
                InstanceTypes.T2Micro,
                StackTest.USEAST1AWINDOWS2012R2AMI,
                subnet,
                new DomainController.DomainInfo(StackTest.DomainDNSName, StackTest.DomainNetBIOSName, StackTest.DomainAdminUser,
                    StackTest.DomainAdminPassword));
            template.AddInstance(DomainController);
            return DomainController;
        }

        public static Template GetTemplateVolumeOnly(TestContext testContext)
        {
            Template t = GetNewBlankTemplateWithVpc(testContext);
            Volume v = new Volume(t,"Volume1");
            v.SnapshotId = "snap-c4d7f7c3";
            v.AvailabilityZone = "us-east-1a";
            t.AddResource(v);
            return t;
        }

        [TestMethod]
        public void CreateStackVolumeTest()
        {
            Stack.Stack.CreateStack(GetTemplateVolumeOnly(this.TestContext));
        }


        [TestMethod]
        public void TestCidr()
        {
            IPNetwork n = IPNetwork.Parse("8.8.8.8");
            Assert.AreEqual("x",n.Network);

        }
        [TestMethod]
        public void CreateStackVolumeAttachmentTest()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            template.AddResource(rdp);
            rdp.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = template.AddSubnet("DMZSubnet", vpc, DMZ1CIDR, Template.AvailabilityZone.UsEast1A);
            InternetGateway gateway = template.AddInternetGateway("InternetGateway", vpc);
            AddInternetGatewayRouteTable(template, vpc, gateway, DMZSubnet);
            WindowsInstance w = new WindowsInstance(template,"Windows1", InstanceTypes.T2Nano, USEAST1AWINDOWS2012R2AMI, DMZSubnet, false);
            w.SecurityGroups.Add(rdp);
            w.AddElasticIp();
            template.AddResource(w);
            VolumeAttachment va = new VolumeAttachment(template,"VolumeAttachment1","/dev/sdh", new ReferenceProperty() {Ref = w.Name}, "vol-ec768410");
            template.AddResource(va);
            Stack.Stack.CreateStack(template);
        }

        [TestMethod]
        public void CreateStackBlockDeviceMappingFromSnapshotTest()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            template.AddResource(rdp);
            rdp.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = template.AddSubnet("DMZSubnet", vpc, DMZ1CIDR, Template.AvailabilityZone.UsEast1A);
            InternetGateway gateway = template.AddInternetGateway("InternetGateway", vpc);
            AddInternetGatewayRouteTable(template, vpc, gateway, DMZSubnet);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, USEAST1AWINDOWS2012R2AMI, DMZSubnet, false);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "/dev/xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "/dev/xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "/dev/xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            /**
$d = Get-Disk  | Where OperationalStatus -eq 'Offline'
$d.ToString();
$d.Number
Set-Disk $d.Number -IsOffline $False
            **/

            w.SecurityGroups.Add(rdp);
            w.AddElasticIp();
            template.AddResource(w);
            Stack.Stack.CreateStack(template);
        }

        [TestMethod]
        public void CreateStackWithMountedSqlTfsVsIsosTest()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            template.AddResource(rdp);
            rdp.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = template.AddSubnet("DMZSubnet", vpc, DMZ1CIDR, Template.AvailabilityZone.UsEast1A);
            InternetGateway gateway = template.AddInternetGateway("InternetGateway", vpc);
            AddInternetGatewayRouteTable(template, vpc, gateway, DMZSubnet);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, USEAST1AWINDOWS2012R2AMI, DMZSubnet, false);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-87e3eb87";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-5e27a85a";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-4e69d94b";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            



            /**
$d = Get-Disk  | Where OperationalStatus -eq 'Offline'
$d.ToString();
$d.Number
Set-Disk $d.Number -IsOffline $False
            **/

            w.AddChefExec(SoftwareS3BucketName, "MountDrives.tar.gz", "MountDrives");


            w.SecurityGroups.Add(rdp);
            w.AddElasticIp();
            template.AddResource(w);
            var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".",string.Empty) ;
            Stack.Stack.CreateStack(template, name);
        }


        [TestMethod]
        public void CreateStackWithVisualStudio()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            template.AddResource(rdp);
            rdp.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = template.AddSubnet("DMZSubnet", vpc, DMZ1CIDR, Template.AvailabilityZone.UsEast1A);
            InternetGateway gateway = template.AddInternetGateway("InternetGateway", vpc);
            AddInternetGatewayRouteTable(template, vpc, gateway, DMZSubnet);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, USEAST1AWINDOWS2012R2AMI, DMZSubnet, false);

            w.AddPackage(SoftwareS3BucketName, new SqlServerExpress());
            w.AddPackage(SoftwareS3BucketName, new VisualStudio());


            w.SecurityGroups.Add(rdp);
            w.AddElasticIp();
            template.AddResource(w);
            var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);
            Stack.Stack.CreateStack(template, name);
        }

        [TestMethod]
        public void CreateDeveloperWorkstation()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            template.AddResource(rdp);
            rdp.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = template.AddSubnet("DMZSubnet", vpc, DMZ1CIDR, Template.AvailabilityZone.UsEast1A);
            InternetGateway gateway = template.AddInternetGateway("InternetGateway", vpc);
            AddInternetGatewayRouteTable(template, vpc, gateway, DMZSubnet);
            var PrivateSubnet1 = template.AddSubnet("PrivateSubnet1", vpc, PrivSub1CIDR, Template.AvailabilityZone.UsEast1A);
            var dc1 = AddDomainController(template, PrivateSubnet1);
            WindowsInstance w = AddWorkstation(template, "Windows1",DMZSubnet, null,dc1, rdp, null, false);
            w.AddElasticIp();

            CreateTestStack(template,this.TestContext);

        }

        [TestMethod]
        public void CreateGenericInstance()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            template.AddResource(rdp);
            rdp.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = template.AddSubnet("DMZSubnet", vpc, DMZ1CIDR, Template.AvailabilityZone.UsEast1A);
            InternetGateway gateway = template.AddInternetGateway("InternetGateway", vpc);
            AddInternetGatewayRouteTable(template, vpc, gateway, DMZSubnet);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, USEAST1AWINDOWS2012R2AMI,false);
            w.Subnet = DMZSubnet;
            w.SecurityGroups.Add(rdp);
            template.AddInstance(w);
            w.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateBuildServer()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            template.AddResource(rdp);
            rdp.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = template.AddSubnet("DMZSubnet", vpc, DMZ1CIDR, Template.AvailabilityZone.UsEast1A);
            InternetGateway gateway = template.AddInternetGateway("InternetGateway", vpc);
            AddInternetGatewayRouteTable(template, vpc, gateway, DMZSubnet);
            var dc1 = AddDomainController(template, DMZSubnet);
            dc1.AddElasticIp();
            dc1.SecurityGroups.Add(rdp);
            WindowsInstance w = AddBuildServer(template, DMZSubnet, null, dc1, rdp,null);
            w.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateDomainController()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            template.AddResource(rdp);
            rdp.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = template.AddSubnet("PrivateSubnet", vpc, PrivSub1CIDR, Template.AvailabilityZone.UsEast1A);
            InternetGateway gateway = template.AddInternetGateway("InternetGateway", vpc);
            AddInternetGatewayRouteTable(template, vpc, gateway, DMZSubnet);

            WindowsInstance w = AddDomainController(template, DMZSubnet);
            template.AddInstance(w);
            w.SecurityGroups.Add(rdp);

            w.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateISOMaker()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            template.AddResource(rdp);
            rdp.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = template.AddSubnet("DMZSubnet", vpc, DMZ1CIDR, Template.AvailabilityZone.UsEast1A);
            InternetGateway gateway = template.AddInternetGateway("InternetGateway", vpc);
            AddInternetGatewayRouteTable(template, vpc, gateway, DMZSubnet);

            WindowsInstance workstation = new WindowsInstance(template, "ISOMaker", InstanceTypes.T2Nano, USEAST1AWINDOWS2012R2AMI, DMZSubnet, false);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(workstation, "/dev/sda1");
            blockDeviceMapping.Ebs.VolumeType = Ebs.VolumeTypes.gp2;
            blockDeviceMapping.Ebs.VolumeSize = 30;
            workstation.AddBlockDeviceMapping(blockDeviceMapping);
            workstation.AddDisk(Ebs.VolumeTypes.gp2, 6);


            workstation.SecurityGroups.Add(rdp);
            
            template.AddInstance(workstation);


            workstation.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }



        public static void CreateTestStack(Template template,TestContext context)
        {
            var name = context.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);
            Stack.Stack.CreateStack(template, name);

        }



        [TestMethod]
        public void CreateStackWithMounterTest()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            template.AddResource(rdp);
            rdp.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = template.AddSubnet("DMZSubnet", vpc, DMZ1CIDR, Template.AvailabilityZone.UsEast1A);
            InternetGateway gateway = template.AddInternetGateway("InternetGateway", vpc);
            AddInternetGatewayRouteTable(template, vpc, gateway, DMZSubnet);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, USEAST1AWINDOWS2012R2AMI, DMZSubnet, false);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);



            /**
$d = Get-Disk  | Where OperationalStatus -eq 'Offline'
$d.ToString();
$d.Number
Set-Disk $d.Number -IsOffline $False
            **/

            w.AddChefExec(SoftwareS3BucketName, "MountDrives.tar.gz", "MountDrives");


            w.SecurityGroups.Add(rdp);
            w.AddElasticIp();
            template.AddResource(w);
            var name = "CreateStackWithMounterTest-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);
            Stack.Stack.CreateStack(template, name);
        }



        private static WindowsInstance AddBuildServer(Template template, Subnet subnet, WindowsInstance tfsServer, DomainController DomainController, SecurityGroup buildServerSecurityGroup, IPNetwork staticIpAddress)
        {

            var buildServer = new WindowsInstance(template, "build", InstanceTypes.T2Small, StackTest.USEAST1AWINDOWS2012R2AMI, subnet, true);
            buildServer.AddBlockDeviceMapping("/dev/sda1", 30, "gp2");

            buildServer.AddPackage(SoftwareS3BucketName, new VisualStudio());

            if (tfsServer != null)
            {
                buildServer.AddDependsOn(tfsServer, ThreeHoursSpan);
            }

            var chefNode = buildServer.GetChefNodeJsonContent();
            var domainAdminUserInfoNode = chefNode.AddNode("domainAdmin");
            domainAdminUserInfoNode.Add("name", DomainNetBIOSName + "\\" + DomainAdminUser);
            domainAdminUserInfoNode.Add("password", DomainAdminPassword);
            template.AddInstance(buildServer);
            buildServer.SecurityGroups.Add(buildServerSecurityGroup);
            if (staticIpAddress != null)
            {
                buildServer.PrivateIpAddress = staticIpAddress.FirstUsable.ToString();
            }
            
            DomainController.AddToDomain(buildServer, ThreeHoursSpan);
            return buildServer;
        }

        private static WindowsInstance AddWorkstation(  Template template, 
                                                        string name, 
                                                        Subnet subnet, 
                                                        Instance dependsOn, 
                                                        DomainController dc1, 
                                                        SecurityGroup workstationSecurityGroup, 
                                                        SecurityGroup tfsUsers,
                                                        bool rename)
        {
            if (subnet == null) throw new ArgumentNullException(nameof(subnet));

            WindowsInstance workstation = new WindowsInstance(template, name, InstanceTypes.T2Nano, USEAST1AWINDOWS2012R2AMI, subnet, rename);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(workstation, "/dev/sda1");
            blockDeviceMapping.Ebs.VolumeType = Ebs.VolumeTypes.gp2;
            blockDeviceMapping.Ebs.VolumeSize = 214;
            workstation.AddBlockDeviceMapping(blockDeviceMapping);
            workstation.AddDisk(Ebs.VolumeTypes.gp2, 10);
            workstation.AddDisk(Ebs.VolumeTypes.gp2, 5);
            workstation.AddPackage(SoftwareS3BucketName, new SqlServerExpress());
            workstation.AddPackage(SoftwareS3BucketName, new VisualStudio());

            if (dependsOn != null)
            {
                workstation.AddDependsOn(dependsOn, ThreeHoursSpan);
            }

            if (workstationSecurityGroup != null)
            {
                workstation.SecurityGroups.Add(workstationSecurityGroup);
            }

            if (tfsUsers != null)
            {
                workstation.SecurityGroups.Add(tfsUsers);
            }

            workstation.AddFinalizer(MaxTimeOut);

            template.AddInstance(workstation);

            if (dc1 != null)
            {
                dc1.AddToDomain(workstation, ThreeHoursSpan);
            }

            return workstation;
        }

        private static WindowsInstance AddTfsServer(Template template, Subnet privateSubnet1, WindowsInstance tfsSqlServer, DomainController dc1, SecurityGroup tfsServerSecurityGroup)
        {
            var tfsServer = new WindowsInstance(template, "tfsserver1", InstanceTypes.T2Small, StackTest.USEAST1AWINDOWS2012R2AMI, privateSubnet1, true);
            tfsServer.AddDependsOn(tfsSqlServer, ThreeHoursSpan);

            var chefNode = tfsServer.GetChefNodeJsonContent();
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

        private static Instance AddNat1(   Template template, 
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

            var nat1 = new Instance(template,
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
        public void CreatePrimeTest()
        {
            CreateTestStack(GetTemplateFullStack(this.TestContext), this.TestContext);
        }

        [TestMethod]
        public void UpdateStackTest()
        {
            var stackName = "Stack5500cc69-af8a-4574-9539-778c92577437";
            var t = new Stack.Stack();
            t.UpdateStack(stackName, GetTemplateFullStack(this.TestContext));
        }




        [TestMethod]
        public void AddingSameResourceTwiceFails()
        {
            var t = GetNewBlankTemplateWithVpc(this.TestContext);
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

        internal static Template GetNewBlankTemplateWithVpc(TestContext testContext)
        {
            var vpcName = $"Vpc{testContext.TestName}";
            return new Template(KeyPairName, vpcName, VPCCIDR);

        }
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion
    }
}
