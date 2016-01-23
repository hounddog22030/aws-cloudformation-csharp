using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Configuration.Packages;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.EC2;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;

namespace AWS.CloudFormation.Test
{
    [TestClass]
    public class StackTest
    {
        private const string DomainAdminPassword = "kasdfiajs!!9";
        private const string CidrDmz1 = "10.0.127.0/28";
        private const string CidrDmz2 = "10.0.255.0/28";
        private const string CidrDomainController1Subnet = "10.0.0.0/24";
        private const string CidrDomainController2Subnet = "10.0.128.0/24";
        private const string CidrSqlServer4TfsSubnet = "10.0.1.0/24";
        private const string CidrTfsServerSubnet = "10.0.2.0/24";
        private const string CidrBuildServerSubnet = "10.0.3.0/24";
        private const string KeyPairName = "corp.getthebuybox.com";
        private const string CidrVpc = "10.0.0.0/16";
        private const string DomainDnsName = "prime.getthebuybox.com";
        private const string DomainAdminUser = "johnny";
        private const string DomainNetBiosName = "prime";
        const string UsEast1aWindows2012R2Ami = "ami-e4034a8e";
        private const string NetBiosNameDomainController1 = "dc1";
        private const string bucketNameSoftware = "gtbb";
        static readonly TimeSpan Timeout3Hours = new TimeSpan(3, 0, 0);
        static readonly TimeSpan Timeout2Hours = new TimeSpan(2, 0, 0);
        static readonly TimeSpan TimeoutMax = new TimeSpan(0, 0, 12*60*60);


        public static Template GetTemplateFullStack(TestContext testContext)
        {
            return GetTemplateFullStack(testContext, null);
        }

        private static Template GetTemplateFullStack(TestContext testContext, string vpcName)
        {
            if (string.IsNullOrEmpty(vpcName))
            {
                vpcName = $"Vpc{testContext.TestName}";
            }

            var template = GetNewBlankTemplateWithVpc(testContext,vpcName);
            Vpc vpc = template.Vpcs.First();

            var subnetDmz1 = new Subnet(template, "subnetDmz1", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            var subnetDmz2 = new Subnet(template,"subnetDmz2", vpc, CidrDmz2, AvailabilityZone.UsEast1A);
            var subnetDomainController1 = new Subnet(template,"subnetDomainController1", vpc, CidrDomainController1Subnet, AvailabilityZone.UsEast1A);
            var subnetSqlServer4Tfs = new Subnet(template, "subnetSqlServer4Tfs", vpc, CidrSqlServer4TfsSubnet,AvailabilityZone.UsEast1A);
            var subnetDomainController2 = new Subnet(template, "subnetDomainController2", vpc, CidrDomainController2Subnet, AvailabilityZone.UsEast1A);
            var subnetTfsServer = new Subnet(template, "subnetTfsServer", vpc, CidrTfsServerSubnet, AvailabilityZone.UsEast1A);
            var subnetBuildServer = new Subnet(template, "subnetBuildServer", vpc, CidrBuildServerSubnet, AvailabilityZone.UsEast1A);

            SecurityGroup elbSecurityGroup = new SecurityGroup(template, "ElbSecurityGroup", "Enables access to the ELB", vpc);
            elbSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.TeamFoundationServerHttp);

            SecurityGroup natSecurityGroup = new SecurityGroup(template,"natSecurityGroup", "Enables Ssh access to NAT1 in AZ1 via port 22 and outbound internet access via private subnets", vpc);
            natSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.Ssh);
            natSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Icmp, Ports.All);

            SecurityGroup tfsServerUsers = new SecurityGroup(template, "TFSUsers", "Security Group To Contain Users of the TFS Services", vpc);

            SecurityGroup tfsServerSecurityGroup = new SecurityGroup(template, "TFSServerSecurityGroup", "Allows various TFS communication", vpc);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetDmz1, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetDmz2, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            tfsServerSecurityGroup.AddIngress(tfsServerUsers, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            tfsServerSecurityGroup.AddIngress(elbSecurityGroup, Protocol.Tcp, Ports.TeamFoundationServerHttp);

            SecurityGroup securityGroupBuildServer = new SecurityGroup(template, "BuildServerSecurityGroup", "Allows build controller to build agent communication", vpc);
            securityGroupBuildServer.AddIngress((ICidrBlock) subnetDmz1, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            securityGroupBuildServer.AddIngress((ICidrBlock) subnetDmz2, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            securityGroupBuildServer.AddIngress((ICidrBlock) subnetTfsServer, Protocol.Tcp, Ports.TeamFoundationServerBuild);

            SecurityGroup sqlServerSecurityGroup = new SecurityGroup(template, "SqlServer4TfsSecurityGroup", "Allows communication to SQLServer Service", vpc);
            sqlServerSecurityGroup.AddIngress((ICidrBlock) subnetDmz1, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            sqlServerSecurityGroup.AddIngress((ICidrBlock) subnetDmz2, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            sqlServerSecurityGroup.AddIngress((ICidrBlock) subnetTfsServer, Protocol.Tcp, Ports.MsSqlServer);

            SecurityGroup workstationSecurityGroup = new SecurityGroup(template,"WorkstationSecurityGroup", "Security Group To Contain Workstations", vpc);
            tfsServerSecurityGroup.AddIngress(workstationSecurityGroup, Protocol.Tcp, Ports.TeamFoundationServerHttp);

            AddInternetGatewayRouteTable(template, vpc, vpc.InternetGateway, subnetDmz1);

            var nat1 = AddNat1(template, subnetDmz1, natSecurityGroup);

            subnetDomainController1.AddNatGateway(nat1, natSecurityGroup);
            subnetSqlServer4Tfs.AddNatGateway(nat1, natSecurityGroup);
            subnetTfsServer.AddNatGateway(nat1, natSecurityGroup);
            subnetBuildServer.AddNatGateway(nat1, natSecurityGroup);

            // ReSharper disable once InconsistentNaming
            // uses 21gb
            var domainInfo = new DomainController.DomainInfo(DomainDnsName, DomainNetBiosName, DomainAdminUser, DomainAdminPassword);
            var instanceDomainController = new DomainController(template, NetBiosNameDomainController1, InstanceTypes.T2Micro, UsEast1aWindows2012R2Ami, subnetDomainController1, domainInfo);


            // uses 19gb
            // ReSharper disable once InconsistentNaming
            var RDGateway = new RemoteDesktopGateway(template, "RDGateway", InstanceTypes.T2Micro, UsEast1aWindows2012R2Ami, subnetDmz1);
            RDGateway.AddFinalizer(Timeout2Hours);
            instanceDomainController.AddToDomain(RDGateway, Timeout3Hours);

            //// uses 25gb
            var tfsSqlServer = AddSql4Tfs(template, subnetSqlServer4Tfs, instanceDomainController, sqlServerSecurityGroup);

            ////// uses 24gb
            var tfsServer = AddTfsServer(template, subnetTfsServer, tfsSqlServer, instanceDomainController, tfsServerSecurityGroup);


            //// uses 24gb
            var buildServer = AddBuildServer(template, subnetBuildServer, tfsServer, instanceDomainController, securityGroupBuildServer);
            buildServer.AddFinalizer(Timeout3Hours);



            //// uses 33gb
            //var workstation = AddWorkstation(template, "workstation3", PrivateSubnet1, buildServer, DomainController, workstationSecurityGroup, tfsServerUsers, true);
            ////var workstation2 = AddWorkstation(template, "workstation2", PrivateSubnet1, buildServer, DomainController, workstationSecurityGroup, tfsServerUsers);


            // the below is a remote desktop gateway server that can
            // be uncommented to debug domain setup problems
            //var RDGateway2 = new RemoteDesktopGateway(template, "RDGateway2", InstanceTypes.T2Micro, "ami-e4034a8e", subnetDmz1);
            //DomainController.AddToDomainMemberSecurityGroup(RDGateway2);

            //LoadBalancer elb = new LoadBalancer(template, "elb1");
            //elb.AddInstance(tfsServer);
            //elb.AddListener("8080", "8080", "http");
            //elb.AddSubnet(DMZSubnet);
            //elb.SecurityGroupIds.Add(elbSecurityGroup);
            //template.AddResource(elb);


            return template;
        }

        private static WindowsInstance AddSql4Tfs(Template template, Subnet PrivateSubnet1, DomainController DomainController,
            SecurityGroup sqlServerSecurityGroup)
        {
            var tfsSqlServer = new WindowsInstance(template, "sql1", InstanceTypes.T2Micro, UsEast1aWindows2012R2Ami, PrivateSubnet1, true);
            DomainController.AddToDomain(tfsSqlServer, Timeout3Hours);
            tfsSqlServer.AddPackage(bucketNameSoftware, new SqlServerExpress(tfsSqlServer));
            tfsSqlServer.SecurityGroupIds.Add(sqlServerSecurityGroup);
            return tfsSqlServer;
        }

        private static DomainController AddDomainController(Template template, Subnet subnet)
        {
            //"ami-805d79ea",
            var DomainController = new DomainController(template,
                NetBiosNameDomainController1,
                InstanceTypes.T2Micro,
                UsEast1aWindows2012R2Ami,
                subnet,
                new DomainController.DomainInfo(DomainDnsName, DomainNetBiosName, DomainAdminUser,
                    DomainAdminPassword));
            return DomainController;
        }

        public static Template GetTemplateVolumeOnly(TestContext testContext)
        {
            Template t = GetNewBlankTemplateWithVpc(testContext);
            Volume v = new Volume(t,"Volume1");
            v.SnapshotId = "snap-c4d7f7c3";
            v.AvailabilityZone = AvailabilityZone.UsEast1A;
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
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            AddInternetGatewayRouteTable(template, vpc, vpc.InternetGateway, DMZSubnet);
            WindowsInstance w = new WindowsInstance(template,"Windows1", InstanceTypes.T2Nano, UsEast1aWindows2012R2Ami, DMZSubnet, false);
            w.SecurityGroupIds.Add(rdp);
            w.AddElasticIp();
            VolumeAttachment va = new VolumeAttachment(template,"VolumeAttachment1","/dev/sdh", w, "vol-ec768410");
            Stack.Stack.CreateStack(template);
        }

        [TestMethod]
        public void CreateStackBlockDeviceMappingFromSnapshotTest()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            AddInternetGatewayRouteTable(template, vpc, vpc.InternetGateway, DMZSubnet);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, UsEast1aWindows2012R2Ami, DMZSubnet, false);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "/dev/xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "/dev/xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "/dev/xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);
            w.SecurityGroupIds.Add(rdp);

            w.AddElasticIp();
            Stack.Stack.CreateStack(template);
        }

        [TestMethod]
        public void CreateStackWithMountedSqlTfsVsIsosTest()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            AddInternetGatewayRouteTable(template, vpc, vpc.InternetGateway, DMZSubnet);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, UsEast1aWindows2012R2Ami, DMZSubnet, false);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-87e3eb87";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-5e27a85a";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-4e69d94b";
            w.AddBlockDeviceMapping(blockDeviceMapping);
            w.AddChefExec(bucketNameSoftware, "MountDrives.tar.gz", "MountDrives");
            w.SecurityGroupIds.Add(rdp);
            w.AddElasticIp();
            var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".",string.Empty) ;
            Stack.Stack.CreateStack(template, name);
        }


        [TestMethod]
        public void CreateStackWithVisualStudio()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            AddInternetGatewayRouteTable(template, vpc, vpc.InternetGateway, DMZSubnet);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, UsEast1aWindows2012R2Ami, DMZSubnet, false);

            w.AddPackage(bucketNameSoftware, new SqlServerExpress(w));
            w.AddPackage(bucketNameSoftware, new VisualStudio());


            w.SecurityGroupIds.Add(rdp);
            w.AddElasticIp();
            var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);
            Stack.Stack.CreateStack(template, name);
        }

        [TestMethod]
        public void CreateDeveloperWorkstation()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            AddInternetGatewayRouteTable(template, vpc, vpc.InternetGateway, DMZSubnet);
            var PrivateSubnet1 = new Subnet(template,"PrivateSubnet1", vpc, CidrDomainController1Subnet, AvailabilityZone.UsEast1A);
            var dc1 = AddDomainController(template, PrivateSubnet1);
            WindowsInstance w = AddWorkstation(template, "Windows1", DMZSubnet, null, dc1, rdp, null, false);
            w.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateGenericInstance()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            AddInternetGatewayRouteTable(template, vpc, vpc.InternetGateway, DMZSubnet);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, UsEast1aWindows2012R2Ami, false);
            w.Subnet = DMZSubnet;
            w.SecurityGroupIds.Add(rdp);
            w.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateBuildServer()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            AddInternetGatewayRouteTable(template, vpc, vpc.InternetGateway, DMZSubnet);
            var dc1 = AddDomainController(template, DMZSubnet);
            dc1.AddElasticIp();
            dc1.SecurityGroupIds.Add(rdp);
            WindowsInstance w = AddBuildServer(template, DMZSubnet, null, dc1, rdp);
            w.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateDomainController()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"PrivateSubnet", vpc, CidrDomainController1Subnet, AvailabilityZone.UsEast1A);
            AddInternetGatewayRouteTable(template, vpc, vpc.InternetGateway, DMZSubnet);

            WindowsInstance w = AddDomainController(template, DMZSubnet);
            w.SecurityGroupIds.Add(rdp);

            w.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateISOMaker()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            AddInternetGatewayRouteTable(template, vpc, vpc.InternetGateway, DMZSubnet);

            WindowsInstance workstation = new WindowsInstance(template, "ISOMaker", InstanceTypes.T2Nano, UsEast1aWindows2012R2Ami, DMZSubnet, false);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(workstation, "/dev/sda1");
            blockDeviceMapping.Ebs.VolumeType = Ebs.VolumeTypes.GeneralPurpose;
            blockDeviceMapping.Ebs.VolumeSize = 30;
            workstation.AddBlockDeviceMapping(blockDeviceMapping);
            workstation.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 6);


            workstation.SecurityGroupIds.Add(rdp);



            workstation.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateSubnetTest()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            var subnet1 = new Subnet(template,"subnet1", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            var subnet2 = new Subnet(template,"subnet2", vpc, CidrDmz2, AvailabilityZone.UsEast1A);
            var subnet3 = new Subnet(template,"subnet3", vpc, CidrDomainController1Subnet, AvailabilityZone.UsEast1A);
            var subnet4 = new Subnet(template,"subnet4", vpc, CidrDomainController2Subnet, AvailabilityZone.UsEast1A);

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        [Timeout(int.MaxValue)]
        public void CfnInitOverWriteTest()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            AddInternetGatewayRouteTable(template, vpc, vpc.InternetGateway, DMZSubnet);

            WindowsInstance workstation = new WindowsInstance(template, "ISOMaker", InstanceTypes.T2Nano, UsEast1aWindows2012R2Ami, DMZSubnet, false);
            workstation.SecurityGroupIds.Add(rdp);
            workstation.AddElasticIp();


            var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);

            CreateTestStack(template, this.TestContext, name);

            workstation.Metadata.Init.ConfigSets.GetConfigSet("x").GetConfig("y").Commands.AddCommand<Command>("z").Command.AddCommandLine(true, "dir");

            Thread.Sleep(new TimeSpan(0, 60, 0));


            //do
            //{
            //    try
            //    {
            Stack.Stack.UpdateStack(name, template);
            //        break;
            //    }
            //    catch (Exception)
            //    {
            //        Thread.Sleep(new TimeSpan(0,5,0));
            //    }

            //} while (true);
        }


        [TestMethod]
        public void SerializerTest()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "SecurityGroupRdp", "Allows Remote Desktop Access", vpc);
            System.Diagnostics.Debug.WriteLine(rdp.Vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            
            Subnet DMZSubnet = new Subnet(template, "DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            RouteTable dmzRouteTable = new RouteTable(template, "DMZRouteTable", vpc);
            Route dmzRoute = new Route(template, "DMZRoute", vpc.InternetGateway, "0.0.0.0/0", dmzRouteTable);
            SubnetRouteTableAssociation DMZSubnetRouteTableAssociation = new SubnetRouteTableAssociation(template, DMZSubnet, dmzRouteTable);
            WindowsInstance workstation = new WindowsInstance(template, "SerializerTest", InstanceTypes.T2Nano, UsEast1aWindows2012R2Ami, DMZSubnet, false);
            workstation.SecurityGroupIds.Add(rdp);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(workstation, "/dev/sda1");
            blockDeviceMapping.Ebs.VolumeType = Ebs.VolumeTypes.GeneralPurpose;
            blockDeviceMapping.Ebs.VolumeSize = 30;
            workstation.AddBlockDeviceMapping(blockDeviceMapping);
            workstation.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 6);
            workstation.AddElasticIp();


            CreateTestStack(template, this.TestContext);

        }


        public static void CreateTestStack(Template template, TestContext context)
        {
            var name = context.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);
            CreateTestStack(template, context, name);

        }
        public static void CreateTestStack(Template template, TestContext context, string name)
        {
            Stack.Stack.CreateStack(template, name);

        }



        [TestMethod]
        public void CreateStackWithMounterTest()
        {
            var template = GetNewBlankTemplateWithVpc(this.TestContext);
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            AddInternetGatewayRouteTable(template, vpc, vpc.InternetGateway, DMZSubnet);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, UsEast1aWindows2012R2Ami, DMZSubnet, false);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);
            w.AddChefExec(bucketNameSoftware, "MountDrives.tar.gz", "MountDrives");


            w.SecurityGroupIds.Add(rdp);
            w.AddElasticIp();
            var name = "CreateStackWithMounterTest-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);
            Stack.Stack.CreateStack(template, name);
        }



        private static WindowsInstance AddBuildServer(Template template, Subnet subnet, WindowsInstance tfsServer, DomainController DomainController, SecurityGroup buildServerSecurityGroup)
        {

            var buildServer = new WindowsInstance(template, "build", InstanceTypes.T2Small, UsEast1aWindows2012R2Ami, subnet, true);
            buildServer.AddBlockDeviceMapping("/dev/sda1", 30, Ebs.VolumeTypes.GeneralPurpose);

            buildServer.AddPackage(bucketNameSoftware, new VisualStudio());
            buildServer.AddPackage(bucketNameSoftware, new TeamFoundationServerBuildServer());

            if (tfsServer != null)
            {
                buildServer.AddDependsOn(tfsServer, Timeout3Hours);
            }

            var chefNode = buildServer.GetChefNodeJsonContent();
            var domainAdminUserInfoNode = chefNode.AddNode("domainAdmin");
            domainAdminUserInfoNode.Add("name", DomainNetBiosName + "\\" + DomainAdminUser);
            domainAdminUserInfoNode.Add("password", DomainAdminPassword);
            buildServer.SecurityGroupIds.Add(buildServerSecurityGroup);
            DomainController.AddToDomain(buildServer, Timeout3Hours);
            buildServer.AddFinalizer(Timeout3Hours);
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

            WindowsInstance workstation = new WindowsInstance(template, name, InstanceTypes.T2Nano, UsEast1aWindows2012R2Ami, subnet, rename);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(workstation, "/dev/sda1");
            blockDeviceMapping.Ebs.VolumeType = Ebs.VolumeTypes.GeneralPurpose;
            blockDeviceMapping.Ebs.VolumeSize = 214;
            workstation.AddBlockDeviceMapping(blockDeviceMapping);
            workstation.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 10);
            workstation.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 5);
            workstation.AddPackage(bucketNameSoftware, new SqlServerExpress(workstation));
            workstation.AddPackage(bucketNameSoftware, new VisualStudio());

            if (dependsOn != null)
            {
                workstation.AddDependsOn(dependsOn, Timeout3Hours);
            }

            if (workstationSecurityGroup != null)
            {
                workstation.SecurityGroupIds.Add(workstationSecurityGroup);
            }

            if (tfsUsers != null)
            {
                workstation.SecurityGroupIds.Add(tfsUsers);
            }

            workstation.AddFinalizer(TimeoutMax);


            if (dc1 != null)
            {
                dc1.AddToDomain(workstation, Timeout3Hours);
            }

            return workstation;
        }

        private static WindowsInstance AddTfsServer(Template template, Subnet privateSubnet1, WindowsInstance tfsSqlServer, DomainController dc1, SecurityGroup tfsServerSecurityGroup)
        {
            var tfsServer = new WindowsInstance(    template, 
                                                    "tfsserver1", 
                                                    InstanceTypes.T2Small, 
                                                    UsEast1aWindows2012R2Ami, 
                                                    privateSubnet1, 
                                                    true, 
                                                    Ebs.VolumeTypes.GeneralPurpose,
                                                    214);

            
            tfsServer.AddDependsOn(tfsSqlServer, TimeoutMax);
            var chefNode = tfsServer.GetChefNodeJsonContent();
            var domainAdminUserInfoNode = chefNode.AddNode("domainAdmin");
            domainAdminUserInfoNode.Add("name", DomainNetBiosName + "\\" + DomainAdminUser);
            domainAdminUserInfoNode.Add("password", DomainAdminPassword);
            tfsServer.SecurityGroupIds.Add(tfsServerSecurityGroup);
            tfsServer.AddPackage(bucketNameSoftware, new TeamFoundationServerApplicationTier());
            dc1.AddToDomain(tfsServer, Timeout3Hours);
            return tfsServer;
        }

        private static void AddInternetGatewayRouteTable(Template template, Vpc vpc, InternetGateway gateway, Subnet subnet)
        {
            RouteTable routeTable = new RouteTable(template, $"{subnet.LogicalId}RouteTable", vpc);
            Route route = new Route(template,$"{subnet.LogicalId}Route", gateway, "0.0.0.0/0", routeTable);
            SubnetRouteTableAssociation routeTableAssociation = new SubnetRouteTableAssociation(template, subnet, routeTable);
        }

        private static Instance AddNat1(   Template template, 
                                                    Subnet DMZSubnet,
                                                    SecurityGroup natSecurityGroup)
        {
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
            return nat1;
        }

        private static void SetupDomainController1SecurityGround(SecurityGroup domainControllerSg1, Vpc vpc, Subnet az2Subnet, SecurityGroup domainMemberSg, Subnet DMZSubnet, Subnet dmzaz2Subnet)
        {
            domainControllerSg1.AddIngress((ICidrBlock)vpc, Protocol.Tcp, Ports.WsManagementPowerShell);
            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Udp,Ports.Ntp);
            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Tcp, Ports.WinsManager);
            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Tcp, Ports.ActiveDirectoryManagement);
            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Udp, Ports.NetBios);

            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet,Protocol.Tcp|Protocol.Udp, Ports.Smb);
            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Tcp|Protocol.Udp, Ports.ActiveDirectoryManagement2);
            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Tcp|Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Tcp|Protocol.Udp, Ports.Ldap);

            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Tcp, Ports.Ldaps);
            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Tcp, Ports.Ldap2Begin, Ports.Ldap2End);

            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Tcp|Protocol.Udp, Ports.DnsQuery);

            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Tcp, Ports.ActiveDirectoryManagement);

            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Tcp|Protocol.Udp, Ports.KerberosKeyDistribution);

            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Udp, Ports.DnsLlmnr);
            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Udp, Ports.NetBt);
            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Tcp, Ports.NetBiosNameServices);
            domainControllerSg1.AddIngress((ICidrBlock)az2Subnet, Protocol.Tcp, Ports.ActiveDirectoryFileReplication);
            domainControllerSg1.AddIngress(domainMemberSg, Protocol.Udp,Ports.Ntp);
            domainControllerSg1.AddIngress(domainMemberSg,Protocol.Tcp,Ports.WinsManager);
            domainControllerSg1.AddIngress(domainMemberSg,Protocol.Tcp,Ports.ActiveDirectoryManagement);
            domainControllerSg1.AddIngress(domainMemberSg,Protocol.Udp, Ports.NetBios);
            domainControllerSg1.AddIngress(domainMemberSg,Protocol.Tcp|Protocol.Udp, Ports.Smb);
            domainControllerSg1.AddIngress(domainMemberSg,Protocol.Tcp| Protocol.Udp, Ports.ActiveDirectoryManagement2);

            domainControllerSg1.AddIngress(domainMemberSg,Protocol.Tcp|Protocol.Udp, Ports.DnsBegin, Ports.DnsEnd);
            domainControllerSg1.AddIngress(domainMemberSg,Protocol.Tcp|Protocol.Udp, Ports.Ldap);

            domainControllerSg1.AddIngress(domainMemberSg,Protocol.Tcp, Ports.Ldaps);
            domainControllerSg1.AddIngress(domainMemberSg,Protocol.Tcp, Ports.Ldap2Begin, Ports.Ldap2End);

            domainControllerSg1.AddIngress(domainMemberSg,Protocol.Tcp|Protocol.Udp, Ports.DnsQuery);

            domainControllerSg1.AddIngress(domainMemberSg,Protocol.Tcp|Protocol.Udp,Ports.KerberosKeyDistribution);

            domainControllerSg1.AddIngress((ICidrBlock)DMZSubnet, Protocol.Tcp|Protocol.Udp, Ports.RemoteDesktopProtocol);

            domainControllerSg1.AddIngress((ICidrBlock)DMZSubnet, Protocol.Icmp, Ports.All);
            domainControllerSg1.AddIngress((ICidrBlock)dmzaz2Subnet, Protocol.Icmp, Ports.All);
        }

        [TestMethod]
        public void CreatePrimeTest()
        {
            CreateTestStack(GetTemplateFullStack(this.TestContext), this.TestContext);
        }

        [TestMethod]
        public void UpdatePrimeTest()
        {
            var stackName = "CreatePrimeTest-2016-01-23T0048421600675-0500";
            
            Stack.Stack.UpdateStack(stackName, GetTemplateFullStack(this.TestContext, "VpcCreatePrimeTest"));
        }



        [TestMethod]
        public void AddingSameResourceTwiceFails()
        {
            var t = GetNewBlankTemplateWithVpc(this.TestContext);
            var v = new Vpc(t,"X","10.0.0.0/16");
            var s = new Subnet(t,"Vpc1",v,null,AvailabilityZone.UsEast1A);

            ArgumentException expectedException = null;

            try
            {
                new Subnet(t,"Vpc1", v, null, AvailabilityZone.UsEast1A);
            }
            catch (ArgumentException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
        }

        internal static Template GetNewBlankTemplateWithVpc(TestContext testContext, string vpcName)
        {
            if (string.IsNullOrEmpty(vpcName))
            {
                vpcName = $"Vpc{testContext.TestName}";
            }
            return new Template(KeyPairName, vpcName, CidrVpc);

        }
        internal static Template GetNewBlankTemplateWithVpc(TestContext testContext)
        {
            return GetNewBlankTemplateWithVpc(testContext, null);

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
