﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Configuration.Packages;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.AutoScaling;
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
        private const string CidrWorkstationSubnet = "10.0.4.0/24";
        private const string KeyPairName = "corp.getthebuybox.com";
        private const string CidrVpc = "10.0.0.0/16";
        public static string DomainDnsName { get; set; } = string.Empty;
        public static string DomainDnsNameSuffix { get; set; } = "yadayada.software";

        private const string DomainAdminUser = "johnny";
        private const string UsEast1AWindows2012R2Ami = "ami-e4034a8e";
        private const string NetBiosNameDomainController1 = "dc1";
        private const string BucketNameSoftware = "gtbb";
        private static readonly TimeSpan Timeout3Hours = new TimeSpan(3, 0, 0);
        private static readonly TimeSpan Timeout2Hours = new TimeSpan(2, 0, 0);
        private static readonly TimeSpan TimeoutMax = new TimeSpan(0, 0, 12*60*60);
        private static readonly TimeSpan Timeout4Hours = new TimeSpan(4, 0, 0);



        public enum ProvisionMode
        {
            Launch,
            Run
        }

        public static Template GetTemplateFullStack(ProvisionMode mode)
        {
            var template = GetNewBlankTemplateWithVpc("Vpc");
            Vpc vpc = template.Vpcs.First();

            //var subnetDomainController2 = new Subnet(template, "subnetDomainController2", vpc, CidrDomainController2Subnet, AvailabilityZone.UsEast1A);
            var subnetDmz2 = new Subnet(template, "subnetDmz2", vpc, CidrDmz2, AvailabilityZone.UsEast1A, true);

            SecurityGroup natSecurityGroup = new SecurityGroup(template,"natSecurityGroup", "Enables Ssh access to NAT1 in AZ1 via port 22 and outbound internet access via private subnets", vpc);
            natSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.Ssh);
            natSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Icmp, Ports.All);

            var subnetDmz1 = new Subnet(template, "subnetDmz1", vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            var nat1 = AddNat1(template, subnetDmz1, natSecurityGroup);

            var subnetDomainController1 = new Subnet(template, "subnetDomainController1", vpc, CidrDomainController1Subnet, AvailabilityZone.UsEast1A);
            subnetDomainController1.AddNatGateway(nat1, natSecurityGroup);

            SecurityGroup sqlServer4TfsSecurityGroup = new SecurityGroup(template, "SqlServer4TfsSecurityGroup", "Allows communication to SQLServer Service", vpc);
            sqlServer4TfsSecurityGroup.AddIngress((ICidrBlock)subnetDmz1, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            sqlServer4TfsSecurityGroup.AddIngress((ICidrBlock)subnetDmz2, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var subnetSqlServer4Tfs = new Subnet(template, "subnetSqlServer4Tfs", vpc, CidrSqlServer4TfsSubnet, AvailabilityZone.UsEast1A);
            subnetSqlServer4Tfs.AddNatGateway(nat1, natSecurityGroup);

            var subnetTfsServer = new Subnet(template, "subnetTfsServer", vpc, CidrTfsServerSubnet, AvailabilityZone.UsEast1A);
            sqlServer4TfsSecurityGroup.AddIngress((ICidrBlock)subnetTfsServer, Protocol.Tcp, Ports.MsSqlServer);
            sqlServer4TfsSecurityGroup.AddIngress((ICidrBlock)subnetTfsServer, Protocol.Tcp, Ports.Smb);
            subnetTfsServer.AddNatGateway(nat1, natSecurityGroup);

            SecurityGroup tfsServerSecurityGroup = new SecurityGroup(template, "TFSServerSecurityGroup", "Allows various TFS communication", vpc);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetDmz1, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetDmz2, Protocol.Tcp, Ports.RemoteDesktopProtocol);

            SecurityGroup securityGroupSqlServer4Build = new SecurityGroup(template, "securityGroupSqlServer4Build", "Allows communication to SQLServer Service", vpc);
            securityGroupSqlServer4Build.AddIngress((ICidrBlock)subnetDmz1, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            securityGroupSqlServer4Build.AddIngress((ICidrBlock)subnetDmz2, Protocol.Tcp, Ports.RemoteDesktopProtocol);

            var subnetBuildServer = new Subnet(template, "subnetBuildServer", vpc, CidrBuildServerSubnet, AvailabilityZone.UsEast1A);
            securityGroupSqlServer4Build.AddIngress((ICidrBlock)subnetBuildServer, Protocol.Tcp, Ports.MsSqlServer);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetBuildServer, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetBuildServer, Protocol.Tcp, Ports.TeamFoundationServerBuild);
            subnetBuildServer.AddNatGateway(nat1, natSecurityGroup);

            SecurityGroup securityGroupBuildServer = new SecurityGroup(template, "BuildServerSecurityGroup", "Allows build controller to build agent communication", vpc);
            securityGroupBuildServer.AddIngress((ICidrBlock)subnetDmz1, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            securityGroupBuildServer.AddIngress((ICidrBlock)subnetDmz2, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            securityGroupBuildServer.AddIngress((ICidrBlock)subnetTfsServer, Protocol.Tcp, Ports.TeamFoundationServerBuild);

            SecurityGroup workstationSecurityGroup = new SecurityGroup(template, "WorkstationSecurityGroup", "Security Group To Contain Workstations", vpc);
            tfsServerSecurityGroup.AddIngress(workstationSecurityGroup, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            var subnetWorkstation = new Subnet(template, "subnetWorkstation", vpc, CidrWorkstationSubnet, AvailabilityZone.UsEast1A);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetWorkstation, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetWorkstation, Protocol.Tcp, Ports.TeamFoundationServerBuild);
            subnetWorkstation.AddNatGateway(nat1, natSecurityGroup);



            var domainInfo = new DomainController.DomainInfo(DomainDnsName, DomainAdminUser, DomainAdminPassword);

            var instanceSize = InstanceTypes.T2Nano;
            if (mode == ProvisionMode.Launch)
            {
                instanceSize = InstanceTypes.C4Large;
            }

            var instanceDomainController = new DomainController(template, NetBiosNameDomainController1, instanceSize, UsEast1AWindows2012R2Ami, subnetDomainController1, domainInfo);
            instanceDomainController.AddFinalizer(TimeoutMax);
            
            FnGetAtt dc1PrivateIp = new FnGetAtt(instanceDomainController,"PrivateIp");
            object[] elements = new object[] {dc1PrivateIp, "10.0.0.2" };
            FnJoin dnsServers = new FnJoin(", ", elements);
            object[] netBiosServersElements = new object[] {dc1PrivateIp};
            FnJoin netBiosServers = new FnJoin(", ", netBiosServersElements);



            DhcpOptions dhcpOptions = new DhcpOptions(template, "dhcpOptions", $"{StackTest.DomainDnsName}", vpc, dnsServers, netBiosServers);
            dhcpOptions.NetbiosNodeType = "2";


            instanceSize = InstanceTypes.T2Nano;
            if (mode == ProvisionMode.Launch)
            {
                instanceSize = InstanceTypes.C4Large;
            }

            var instanceRdp = new RemoteDesktopGateway(template, "rdp", instanceSize, UsEast1AWindows2012R2Ami, subnetDmz1);
            instanceRdp.AddFinalizer(TimeoutMax);
            instanceDomainController.AddToDomain(instanceRdp, TimeoutMax);

            instanceSize = InstanceTypes.T2Micro;
            if (mode == ProvisionMode.Launch)
            {
                instanceSize = InstanceTypes.C4Large;
            }

            var instanceTfsSqlServer = AddSql(template, "sql4tfs", instanceSize, subnetSqlServer4Tfs, instanceDomainController, sqlServer4TfsSecurityGroup);

            instanceSize = InstanceTypes.T2Micro;
            if (mode == ProvisionMode.Launch)
            {
                instanceSize = InstanceTypes.C4Large;
            }



            var tfsServer = AddTfsServer(template, instanceSize, subnetTfsServer, instanceTfsSqlServer, instanceDomainController, tfsServerSecurityGroup);


            //instanceSize = InstanceTypes.T2Micro;
            //if (mode == ProvisionMode.Launch)
            //{
            //    instanceSize = InstanceTypes.C4Large;
            //}

            WindowsInstance sql4Build = null;
            //sql4Build = AddSql(template, "sql4build", instanceSize, subnetBuildServer, instanceDomainController, securityGroupSqlServer4Build);


            instanceSize = InstanceTypes.T2Small;
            if (mode == ProvisionMode.Launch)
            {
                instanceSize = InstanceTypes.C4Large;
            }

            var buildServer = AddBuildServer(template, instanceSize, subnetBuildServer, tfsServer, instanceDomainController, securityGroupBuildServer, sql4Build);
            buildServer.AddFinalizer(TimeoutMax);

            // uses 33gb
            var workstation = AddWorkstation(template, "workstation", subnetWorkstation, instanceDomainController, workstationSecurityGroup, true);
            workstation.AddFinalizer(TimeoutMax);


            SecurityGroup elbSecurityGroup = new SecurityGroup(template, "ElbSecurityGroup", "Enables access to the ELB", vpc);
            elbSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            tfsServerSecurityGroup.AddIngress(elbSecurityGroup, Protocol.Tcp, Ports.TeamFoundationServerHttp);

            //////LoadBalancer elb = new LoadBalancer(template, "elb1");
            //////elb.AddInstance(tfsServer);
            //////elb.AddListener("8080", "8080", "http");
            //////elb.AddSubnet(DMZSubnet);
            //////elb.AddSecurityGroup(elbSecurityGroup);
            //////template.AddResource(elb);

            // the below is a remote desktop gateway server that can
            // be uncommented to debug domain setup problems
            //var instanceRdp2 = new RemoteDesktopGateway(template, "rdp2", InstanceTypes.T2Micro, "ami-e4034a8e", subnetDmz1);
            //instanceDomainController.AddToDomainMemberSecurityGroup(instanceRdp2);

            return template;
        }

        private static WindowsInstance AddSql(Template template, string instanceName, InstanceTypes instanceSize, Subnet subnet, DomainController domainController, SecurityGroup sqlServerSecurityGroup)
        {
            var sqlServer = new WindowsInstance(template, instanceName, instanceSize, UsEast1AWindows2012R2Ami, subnet, true);
            domainController.AddToDomain(sqlServer, TimeoutMax);
            sqlServer.AddPackage(BucketNameSoftware, new SqlServerExpress(sqlServer));
            sqlServer.AddSecurityGroup(sqlServerSecurityGroup);
            return sqlServer;
        }

        private static DomainController AddDomainController(Template template, Subnet subnet)
        {
            //"ami-805d79ea",
            var DomainController = new DomainController(template,
                NetBiosNameDomainController1,
                InstanceTypes.T2Micro,
                UsEast1AWindows2012R2Ami,
                subnet,
                new DomainController.DomainInfo(DomainDnsName, DomainAdminUser,
                    DomainAdminPassword));
            return DomainController;
        }

        public Template GetTemplateVolumeOnly(TestContext testContext)
        {
            Template t = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            Volume v = new Volume(t,"Volume1");
            v.SnapshotId = "snap-c4d7f7c3";
            v.AvailabilityZone = AvailabilityZone.UsEast1A;

            return t;
        }

        [TestMethod]
        public void CreateAutoScalingGroupTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template, "DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);

            var launchConfig = new LaunchConfiguration(template, "Xyz", InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, OperatingSystem.Windows);
            launchConfig.AssociatePublicIpAddress = true;
            launchConfig.AddSecurityGroup(rdp);



            var launchGroup = new AutoScalingGroup(template, "AutoGroup");
            launchGroup.LaunchConfigurationName = new ReferenceProperty(launchConfig);
            launchGroup.MinSize = 1.ToString();
            launchGroup.MaxSize = 2.ToString();
            launchGroup.AddAvailabilityZone(AvailabilityZone.UsEast1A);
            launchGroup.AddSubnetToVpcZoneIdentifier(DMZSubnet);
            Stack.Stack.CreateStack(template,this.TestContext.TestName + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty));
        }

        [TestMethod]
        public void GetHashOfSourceTest()
        {
            var r = GetGitHash();

            StringAssert.Contains(r, "0");

            Assert.AreEqual("0e34235e264c315ab1efa46d3316d84ca21a688f".Length,r.Length);
        }

        public static string GetGitHash()
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "git.exe";
            p.StartInfo.Arguments = "rev-parse HEAD";
            p.Start();

            // To avoid deadlocks, always read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            byte newLineByte = 10;
            char newLine = (char)newLineByte;
            byte nullByte = 0;
            char nullChar = (char) nullByte;
            return output.Replace(newLine.ToString(), string.Empty);

        }

        [TestMethod]
        public void GetGitDiffTest()
        {
            //
            Assert.IsTrue(GetGitDiff());
        }

        public static bool GetGitDiff()
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "git.exe";
            p.StartInfo.Arguments = "diff";
            p.Start();

            // To avoid deadlocks, always read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            return output.Length == 0;
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
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A,true);
            WindowsInstance w = new WindowsInstance(template,"Windows1", InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, DMZSubnet, false);
            w.AddSecurityGroup(rdp);
            w.AddElasticIp();
            VolumeAttachment va = new VolumeAttachment(template,"VolumeAttachment1","/dev/sdh", w, "vol-ec768410");
            Stack.Stack.CreateStack(template);
        }

        [TestMethod]
        public void CreateStackBlockDeviceMappingFromSnapshotTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, DMZSubnet, false);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "/dev/xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "/dev/xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "/dev/xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);
            w.AddSecurityGroup(rdp);

            w.AddElasticIp();
            Stack.Stack.CreateStack(template);
        }

        [TestMethod]
        public void CreateStackWithMountedSqlTfsVsIsosTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, DMZSubnet, false);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-87e3eb87";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-5e27a85a";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-4e69d94b";
            w.AddBlockDeviceMapping(blockDeviceMapping);
            w.AddChefExec(BucketNameSoftware, "MountDrives.tar.gz", "MountDrives");
            w.AddSecurityGroup(rdp);
            w.AddElasticIp();
            var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".",string.Empty) ;
            Stack.Stack.CreateStack(template, name);
        }


        [TestMethod]
        public void CreateStackWithVisualStudio()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A,true);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, DMZSubnet, false);

            w.AddPackage(BucketNameSoftware, new SqlServerExpress(w));
            w.AddPackage(BucketNameSoftware, new VisualStudio());


            w.AddSecurityGroup(rdp);
            w.AddElasticIp();
            var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);
            Stack.Stack.CreateStack(template, name);
        }

        [TestMethod]
        public void CreateDeveloperWorkstation()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            WindowsInstance w = AddWorkstation(template, "Windows1", DMZSubnet, null, rdp, false);
            w.AddElasticIp();
            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateGenericInstance()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A,true);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, false);
            w.Subnet = DMZSubnet;
            w.AddSecurityGroup(rdp);
            w.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateBuildServer()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A,true);
            var dc1 = AddDomainController(template, DMZSubnet);
            dc1.AddElasticIp();
            dc1.AddSecurityGroup(rdp);
            WindowsInstance w = AddBuildServer(template, InstanceTypes.T2Nano,  DMZSubnet, null, dc1, rdp,null);
            w.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateDomainController()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"PrivateSubnet", vpc, CidrDomainController1Subnet, AvailabilityZone.UsEast1A,true);

            WindowsInstance w = AddDomainController(template, DMZSubnet);
            w.AddSecurityGroup(rdp);

            w.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateMinimalInstanceTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template, "DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);

            WindowsInstance workstation = new WindowsInstance(template, "ISOMaker", InstanceTypes.C4Large, UsEast1AWindows2012R2Ami, DMZSubnet, false);
            workstation.AddSecurityGroup(rdp);
            workstation.AddElasticIp();
            CreateTestStack(template, this.TestContext);

        }

        


        [TestMethod]
        public void CreateISOMaker()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A,true);

            WindowsInstance workstation = new WindowsInstance(template, "ISOMaker", InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, DMZSubnet, false);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(workstation, "/dev/sda1");
            blockDeviceMapping.Ebs.VolumeType = Ebs.VolumeTypes.GeneralPurpose;
            blockDeviceMapping.Ebs.VolumeSize = 30;
            workstation.AddBlockDeviceMapping(blockDeviceMapping);
            workstation.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 6);


            workstation.AddSecurityGroup(rdp);



            workstation.AddElasticIp();

            CreateTestStack(template, this.TestContext);
        }

        //ISOMaker

        [TestMethod]
        public void CreateSubnetTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
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
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A,true);

            WindowsInstance workstation = new WindowsInstance(template, "ISOMaker", InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, DMZSubnet, false);
            workstation.AddSecurityGroup(rdp);
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
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "SecurityGroupRdp", "Allows Remote Desktop Access", vpc);
            System.Diagnostics.Debug.WriteLine(rdp.Vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            
            Subnet DMZSubnet = new Subnet(template, "DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A);
            RouteTable dmzRouteTable = new RouteTable(template, "DMZRouteTable", vpc);
            Route dmzRoute = new Route(template, "DMZRoute", vpc.InternetGateway, "0.0.0.0/0", dmzRouteTable);
            SubnetRouteTableAssociation DMZSubnetRouteTableAssociation = new SubnetRouteTableAssociation(template, DMZSubnet, dmzRouteTable);
            WindowsInstance workstation = new WindowsInstance(template, "SerializerTest", InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, DMZSubnet, false);
            workstation.AddSecurityGroup(rdp);
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
            var name = template.StackName;
            if (string.IsNullOrEmpty(name))
            {
                name = $"{context.TestName}";
                using ( var r = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".git", "HEAD")))
                {
                    var line = r.ReadLine();
                    var parts = line.Split('/');
                    name += $"-{parts[parts.Length - 1]}-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}";
                }
            }
            CreateTestStack(template, context, name);

        }
        public static void CreateTestStack(Template template, TestContext context, string name)
        {
            Stack.Stack.CreateStack(template, name);

        }



        [TestMethod]
        public void CreateStackWithMounterTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup(template, "rdp", "rdp", vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(template,"DMZSubnet", vpc, CidrDmz1, AvailabilityZone.UsEast1A,true);
            WindowsInstance w = new WindowsInstance(template, "Windows1", InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, DMZSubnet, false);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);
            w.AddChefExec(BucketNameSoftware, "MountDrives.tar.gz", "MountDrives");


            w.AddSecurityGroup(rdp);
            w.AddElasticIp();
            var name = "CreateStackWithMounterTest-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);
            Stack.Stack.CreateStack(template, name);
        }


        private static WindowsInstance AddBuildServer(Template template, InstanceTypes instanceSize, Subnet subnet, WindowsInstance tfsServer, DomainController domainController, SecurityGroup buildServerSecurityGroup, WindowsInstance sql4Build)
        {

            var buildServer = new WindowsInstance(template, $"build", instanceSize, UsEast1AWindows2012R2Ami, subnet, false, DefinitionType.LaunchConfiguration);

            buildServer.AddBlockDeviceMapping("/dev/sda1", 100, Ebs.VolumeTypes.GeneralPurpose);

            buildServer.AddPackage(BucketNameSoftware, new VisualStudio());
            buildServer.AddPackage(BucketNameSoftware, new TeamFoundationServerBuildServerAgentOnly(buildServer, tfsServer));

            if (tfsServer != null)
            {
                buildServer.AddDependsOn(tfsServer, TimeoutMax);
            }
            if (sql4Build != null)
            {
                buildServer.DependsOn.Add(sql4Build.LogicalId);
            }

            var chefNode = buildServer.GetChefNodeJsonContent();
            var domainAdminUserInfoNode = chefNode.AddNode("domainAdmin");
            var domainInfo = new DomainController.DomainInfo(DomainDnsName, DomainAdminUser, DomainAdminPassword);

            domainAdminUserInfoNode.Add("name", domainInfo.DomainNetBiosName + "\\" + DomainAdminUser);
            domainAdminUserInfoNode.Add("password", DomainAdminPassword);
            buildServer.AddSecurityGroup(buildServerSecurityGroup);
            domainController.AddToDomain(buildServer, TimeoutMax);
            buildServer.AddFinalizer(TimeoutMax);

            AutoScalingGroup launchGroup = new AutoScalingGroup(template, "BuildServerAutoScalingGroup");
            launchGroup.LaunchConfigurationName = new ReferenceProperty(buildServer);
            launchGroup.MinSize = 1.ToString();
            launchGroup.MaxSize = 2.ToString();
            launchGroup.AddAvailabilityZone(AvailabilityZone.UsEast1A);
            launchGroup.AddSubnetToVpcZoneIdentifier(subnet);

            return buildServer;
        }

        private static WindowsInstance AddWorkstation(  Template template, 
                                                        string name, 
                                                        Subnet subnet, 
                                                        DomainController instanceDomainController, 
                                                        SecurityGroup workstationSecurityGroup, 
                                                        bool rename)
        {
            if (subnet == null) throw new ArgumentNullException(nameof(subnet));

            WindowsInstance workstation = new WindowsInstance(template, name, InstanceTypes.C4Large, UsEast1AWindows2012R2Ami, subnet, rename, Ebs.VolumeTypes.GeneralPurpose, 214);

            workstation.AddPackage(BucketNameSoftware, new SqlServerExpress(workstation));
            workstation.AddPackage(BucketNameSoftware, new VisualStudio());

            if (workstationSecurityGroup != null)
            {
                workstation.AddSecurityGroup(workstationSecurityGroup);
            }

            workstation.AddFinalizer(TimeoutMax);

            if (instanceDomainController != null)
            {
                instanceDomainController.AddToDomain(workstation, TimeoutMax);
            }

            return workstation;
        }

        private static WindowsInstance AddTfsServer(Template template, InstanceTypes instanceSize, Subnet privateSubnet1, WindowsInstance tfsSqlServer, DomainController dc1, SecurityGroup tfsServerSecurityGroup)
        {
            var tfsServer = new WindowsInstance(    template, 
                                                    "tfs",
                                                    instanceSize, 
                                                    UsEast1AWindows2012R2Ami, 
                                                    privateSubnet1, 
                                                    true, 
                                                    Ebs.VolumeTypes.GeneralPurpose,
                                                    214);

            
            tfsServer.AddDependsOn(tfsSqlServer, TimeoutMax);
            var chefNode = tfsServer.GetChefNodeJsonContent();
            var domainAdminUserInfoNode = chefNode.AddNode("domainAdmin");
            var domainInfo = new DomainController.DomainInfo(DomainDnsName, DomainAdminUser, DomainAdminPassword);
            domainAdminUserInfoNode.Add("name", domainInfo.DomainNetBiosName + "\\" + DomainAdminUser);
            domainAdminUserInfoNode.Add("password", DomainAdminPassword);
            tfsServer.AddSecurityGroup(tfsServerSecurityGroup);
            tfsServer.AddPackage(BucketNameSoftware, new TeamFoundationServerApplicationTier(tfsServer));
            dc1.AddToDomain(tfsServer, TimeoutMax);
            return tfsServer;
        }

        //private static void AddInternetGatewayRouteTable(Template template, Vpc vpc, InternetGateway gateway, Subnet subnet)
        //{
        //    RouteTable routeTable = new RouteTable(template, $"{subnet.LogicalId}RouteTable", vpc);
        //    Route route = new Route(template,$"{subnet.LogicalId}Route", gateway, "0.0.0.0/0", routeTable);
        //    SubnetRouteTableAssociation routeTableAssociation = new SubnetRouteTableAssociation(template, subnet, routeTable);
        //}

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

        enum Greek
        {
            Alpha,
            Beta,
            Gamma,
            Delta,
            Epsilon,
            Zeta,
            Eta,
            Theta,
            Iota,
            Kappa,
            Lambda,
            Mu,
            Nu,
            Xi,
            Omicron,
            Pi,
            Rho,
            Sigma,
            Tau,
            Upsilon,
            Phi,
            Chi,
            Psi,
            Omega,
            None
        }

        [TestMethod]
        public void CreateDevelopmentTest()
        {
            var stacks = Stack.Stack.GetActiveStacks();
            var name = string.Empty;

            foreach (var thisGreek in Enum.GetNames(typeof(Greek)))
            {
                name = (thisGreek + "." + DomainDnsNameSuffix).ToLower();
                if (!stacks.Any(s => s.Name.StartsWith(name.Replace('.', '-'))))
                {
                    break;
                }
            }

            StackTest.DomainDnsName = name;
            var templateToCreateStack = GetTemplateFullStack(ProvisionMode.Run);
            templateToCreateStack.StackName = StackTest.DomainDnsName.Replace('.', '-');

            CreateTestStack(templateToCreateStack, this.TestContext);
        }

        [TestMethod]
        public void UpdateDevelopmentTest()
        {
            var stackName = "alpha-yadayada-software";

            StackTest.DomainDnsName = "alpha.yadayada.software";

            Stack.Stack.UpdateStack(stackName, GetTemplateFullStack(ProvisionMode.Run));
        }



        [TestMethod]
        public void AddingSameResourceTwiceFails()
        {
            var t = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
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

        [TestMethod]
        public void GetStacksTest()
        {
            List<Stack.Stack> stacks = Stack.Stack.GetActiveStacks();
            Assert.IsNotNull(stacks);
            Assert.IsTrue(stacks.Any());
            var create = stacks.Where(r => r.Name.Contains("Create"));
            Assert.IsTrue(create.Any());
        }

        internal static Template GetNewBlankTemplateWithVpc(string vpcName)
        {
            if (string.IsNullOrEmpty(vpcName))
            {
                throw new ArgumentNullException(nameof(vpcName));
            }
            return new Template(KeyPairName, vpcName, CidrVpc);

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
