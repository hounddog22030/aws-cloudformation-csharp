using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using AWS.CloudFormation.Configuration.Packages;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Resource.RDS;
using AWS.CloudFormation.Resource.Wait;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;

namespace AWS.CloudFormation.Test
{
    [TestClass]
    public class StackTest
    {
        //private const string DomainAdminPassword = "kasdfiajs!!9";
        private const string CidrDmz1 = "10.0.127.0/28";
        private const string CidrDmz2 = "10.0.255.0/28";
        private const string CidrDomainController1Subnet = "10.0.0.0/24";
        private const string CidrDomainController2Subnet = "10.0.128.0/24";
        private const string CidrSqlServer4TfsSubnet = "10.0.1.0/24";
        private const string CidrTfsServerSubnet = "10.0.2.0/24";
        private const string CidrBuildServerSubnet = "10.0.3.0/24";
        private const string CidrWorkstationSubnet = "10.0.4.0/24";
        private const string CidrDatabase4BuildSubnet2 = "10.0.5.0/24";
        private const string KeyPairName = "corp.getthebuybox.com";
        private const string CidrVpc = "10.0.0.0/16";
        public static string DomainDnsName = "yadayada.software";

        private const string DomainAdminUser = "johnny";
        private const string UsEast1AWindows2012R2Ami = "ami-9a0558f0";
        private const string UsEast1AWindows2012R2SqlServerExpressAmi = "ami-a3005dc9";
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

        public static Template GetTemplateWithParameters()
        {
            var template = new Template(KeyPairName, "Vpc", CidrVpc);
            var password = System.Web.Security.Membership.GeneratePassword(8, 0);
            var domainPassword = new ParameterBase(Template.ParameterDomainAdminPassword, "String", password, "Password for domain administrator.")
            {
                NoEcho = true
            };

            template.Parameters.Add(Template.ParameterDomainAdminPassword, domainPassword);
            return template;
        }

        [TestMethod]
        public void TestParameters()
        {
            var template = GetTemplateWithParameters();
            CreateTestStack(template, this.TestContext);
        }

        public static Template GetTemplateFullStack(string version)
        {
            var guid = Guid.NewGuid().ToString().Replace("-", string.Empty);
            var random = new Random(((int)DateTime.Now.Ticks % int.MaxValue));

            string password = string.Empty;

            for (int i = 0; i < 4; i++)
            {
                char charToAdd = ((char)random.Next((int)'A', (int)'Z'));
                password += charToAdd;
            }

            for (int i = 0; i < 4; i++)
            {
                char charToAdd = ((char) random.Next((int) '0', (int) '9'));
                password += charToAdd;
            }

            for (int i = 0; i < 4; i++)
            {
                char charToAdd = ((char)random.Next((int)'a', (int)'z'));
                password += charToAdd;
            }

            Assert.IsFalse(HasGitDifferences());
            var gitHash = GetGitHash();
            var template = new Template(KeyPairName, $"Vpc{version}", CidrVpc,gitHash);

            var domainPassword = new ParameterBase("DomainAdminPassword", "String", password,
                "Password for domain administrator.")
            {
                NoEcho = true
            };

            template.Parameters.Add("DomainAdminPassword", domainPassword);

            Vpc vpc = template.Vpcs.First();
            vpc.EnableDnsHostnames = true;
            vpc.EnableDnsSupport = true;

            var subnetDmz2 = new Subnet(vpc, CidrDmz2, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("SubnetDmz2", subnetDmz2);


            SecurityGroup natSecurityGroup = new SecurityGroup("Enables Ssh access to NAT1 in AZ1 via port 22 and outbound internet access via private subnets", vpc);
            template.Resources.Add("SecurityGroup4Nat", natSecurityGroup);

            natSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.Ssh);
            natSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Icmp, Ports.All);

            var subnetDmz1 = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("SubnetDmz1", subnetDmz1);

            var nat1 = AddNat1(template, subnetDmz1, natSecurityGroup);

            var subnetDomainController1 = new Subnet(vpc, CidrDomainController1Subnet, AvailabilityZone.UsEast1A, nat1, natSecurityGroup);
            template.Resources.Add("Subnet4DomainController1", subnetDomainController1);

            SecurityGroup sqlServer4TfsSecurityGroup = new SecurityGroup("Allows communication to SQLServer Service", vpc);
            template.Resources.Add("SecurityGroup4SqlServer4Tfs", sqlServer4TfsSecurityGroup);

            sqlServer4TfsSecurityGroup.AddIngress((ICidrBlock)subnetDmz1, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            sqlServer4TfsSecurityGroup.AddIngress((ICidrBlock)subnetDmz2, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var subnetSqlServer4Tfs = new Subnet(vpc, CidrSqlServer4TfsSubnet, AvailabilityZone.UsEast1A, nat1, natSecurityGroup);
            template.Resources.Add("Subnet4SqlServer4Tfs", subnetSqlServer4Tfs);


            var subnetTfsServer = new Subnet(vpc, CidrTfsServerSubnet, AvailabilityZone.UsEast1A, nat1, natSecurityGroup);
            template.Resources.Add("Subnet4TfsServer", subnetTfsServer);

            sqlServer4TfsSecurityGroup.AddIngress((ICidrBlock)subnetTfsServer, Protocol.Tcp, Ports.MsSqlServer);
            sqlServer4TfsSecurityGroup.AddIngress((ICidrBlock)subnetTfsServer, Protocol.Tcp, Ports.Smb);

            SecurityGroup tfsServerSecurityGroup = new SecurityGroup("Allows various TFS communication", vpc);
            template.Resources.Add("SecurityGroup4TfsServer", tfsServerSecurityGroup);

            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetDmz1, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetDmz2, Protocol.Tcp, Ports.RemoteDesktopProtocol);

            var subnetBuildServer = new Subnet(vpc, CidrBuildServerSubnet, AvailabilityZone.UsEast1A, nat1, natSecurityGroup);
            template.Resources.Add("Subnet4BuildServer", subnetBuildServer);


            SecurityGroup securityGroupDb4Build = new SecurityGroup("Allows communication to Db", vpc);
            template.Resources.Add("SecurityGroup4Build2Db", securityGroupDb4Build);

            securityGroupDb4Build.AddIngress((ICidrBlock)subnetBuildServer, Protocol.Tcp, Ports.MySql);
            SecurityGroup securityGroupSqlSever4Build = new SecurityGroup("Allows communication to SqlServer", vpc);
            template.Resources.Add("SecurityGroup4Build2SqlSever", securityGroupSqlSever4Build);

            securityGroupSqlSever4Build.AddIngress((ICidrBlock)subnetBuildServer, Protocol.Tcp, Ports.MsSqlServer);

            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetBuildServer, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetBuildServer, Protocol.Tcp, Ports.TeamFoundationServerBuild);

            var subnetDatabase4BuildServer2 = new Subnet(vpc, CidrDatabase4BuildSubnet2, AvailabilityZone.UsEast1E, true);
            template.Resources.Add("Subnet4Build2Database", subnetDatabase4BuildServer2);

            securityGroupDb4Build.AddIngress((ICidrBlock)subnetBuildServer, Protocol.Tcp, Ports.MySql);

            SecurityGroup securityGroupBuildServer = new SecurityGroup("Allows build controller to build agent communication", vpc);
            template.Resources.Add("SecurityGroup4BuildServer", securityGroupBuildServer);

            securityGroupBuildServer.AddIngress((ICidrBlock)subnetDmz1, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            securityGroupBuildServer.AddIngress((ICidrBlock)subnetDmz2, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            securityGroupBuildServer.AddIngress((ICidrBlock)subnetTfsServer, Protocol.Tcp, Ports.TeamFoundationServerBuild);

            SecurityGroup workstationSecurityGroup = new SecurityGroup("Security Group To Contain Workstations", vpc);
            template.Resources.Add("SecurityGroup4Workstation", workstationSecurityGroup);

            tfsServerSecurityGroup.AddIngress(workstationSecurityGroup, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            var subnetWorkstation = new Subnet(vpc, CidrWorkstationSubnet, AvailabilityZone.UsEast1A, nat1, natSecurityGroup);
            template.Resources.Add("Subnet4Workstation", subnetWorkstation);

            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetWorkstation, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetWorkstation, Protocol.Tcp, Ports.TeamFoundationServerBuild);
            // give db access to the workstations
            securityGroupSqlSever4Build.AddIngress((ICidrBlock)subnetWorkstation, Protocol.Tcp, Ports.MsSqlServer);
            securityGroupDb4Build.AddIngress((ICidrBlock)subnetWorkstation, Protocol.Tcp, Ports.MySql);

            var domainAdminPasswordReference = new ReferenceProperty(Template.ParameterDomainAdminPassword);

            var domainInfo = new DomainInfo(DomainDnsName, DomainAdminUser, domainAdminPasswordReference);


            var instanceDomainController = new Instance(subnetDomainController1,InstanceTypes.C4Large,UsEast1AWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add("DomainController", instanceDomainController);


            instanceDomainController.DependsOn.Add(nat1.LogicalId);

            DomainControllerPackage dcPackage = new DomainControllerPackage(domainInfo, subnetDomainController1);
            instanceDomainController.Packages.Add(dcPackage);

            instanceDomainController.Packages.Add(new Chrome());

            FnGetAtt dc1PrivateIp = new FnGetAtt(instanceDomainController, FnGetAttAttribute.AwsEc2InstancePrivateIp);
            object[] elements = new object[] { dc1PrivateIp, "10.0.0.2" };
            FnJoin dnsServers = new FnJoin(FnJoinDelimiter.Comma, elements);
            object[] netBiosServersElements = new object[] { dc1PrivateIp };
            FnJoin netBiosServers = new FnJoin(FnJoinDelimiter.Comma, netBiosServersElements);



            DhcpOptions dhcpOptions = new DhcpOptions($"{StackTest.DomainDnsName}", vpc, dnsServers, netBiosServers);
            template.Resources.Add("DhcpOptions",dhcpOptions);
            dhcpOptions.NetbiosNodeType = "2";


            var instanceRdp = new Instance(subnetDmz1, InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami,
                OperatingSystem.Windows);
            template.Resources.Add($"Rdp{version}", instanceRdp);

            dcPackage.Participate(instanceRdp);
            instanceRdp.Packages.Add(new RemoteDesktopGatewayPackage(domainInfo));

            var instanceTfsSqlServer = AddSql(template, "Sql4Tfs", InstanceTypes.T2Micro, subnetSqlServer4Tfs, dcPackage, sqlServer4TfsSecurityGroup);

            var tfsServer = AddTfsServer(template, InstanceTypes.T2Small, subnetTfsServer, instanceTfsSqlServer, dcPackage, tfsServerSecurityGroup);
            var tfsApplicationTierInstalled = tfsServer.Packages.OfType<TeamFoundationServerApplicationTier>().First().WaitCondition;

            DbSubnetGroup mySqlSubnetGroupForDatabaseForBuild = new DbSubnetGroup("Second subnet for database for build server");
            template.Resources.Add("DbSubnetGroup4Build2Database", mySqlSubnetGroupForDatabaseForBuild);

            mySqlSubnetGroupForDatabaseForBuild.AddSubnet(subnetBuildServer);
            mySqlSubnetGroupForDatabaseForBuild.AddSubnet(subnetDatabase4BuildServer2);
            //DbInstance mySql4Build = null;

            ////mySql4Build = new DbInstance(
            ////    template,
            ////    "sql4build",
            ////    DbInstanceClassEnum.DbT2Micro,
            ////    EngineType.MySql,
            ////    LicenseModelType.GeneralPublicLicense,
            ////    Ebs.VolumeTypes.GeneralPurpose,
            ////    20,
            ////    new ReferenceProperty(TeamFoundationServerBuildServerBase.sqlexpress4build_username_parameter_name),
            ////    new ReferenceProperty(TeamFoundationServerBuildServerBase.sqlexpress4build_password_parameter_name),
            ////    mySqlSubnetGroupForDatabaseForBuild,
            ////    securityGroupDb4Build);

            DbSubnetGroup subnetGroupSqlExpress4Build = new DbSubnetGroup("DbSubnet Group for SQL Server database for build server");
            template.Resources.Add("SubnetGroup4Build2SqlServer", subnetGroupSqlExpress4Build);

            subnetGroupSqlExpress4Build.AddSubnet(subnetBuildServer);
            subnetGroupSqlExpress4Build.AddSubnet(subnetDatabase4BuildServer2);

            DbInstance rdsSqlExpress4Build = null;


            rdsSqlExpress4Build = new DbInstance(DbInstanceClassEnum.DbT2Micro,
                EngineType.SqlServerExpress,
                LicenseModelType.LicenseIncluded,
                Ebs.VolumeTypes.GeneralPurpose,
                30,
                new ReferenceProperty(TeamFoundationServerBuildServerBase.sqlexpress4build_username_parameter_name),
                new ReferenceProperty(TeamFoundationServerBuildServerBase.sqlexpress4build_password_parameter_name))
            {
                DBSubnetGroupName = new ReferenceProperty(subnetGroupSqlExpress4Build)
            };

            template.Resources.Add("SqlServer4Build", rdsSqlExpress4Build);


            rdsSqlExpress4Build.AddVpcSecurityGroup(securityGroupSqlSever4Build);

            template.Parameters.Add(new ParameterBase(TeamFoundationServerBuildServerBase.sqlexpress4build_username_parameter_name, "String", "sqlservermasteruser", "Master User For RDS SqlServer"));
            template.Parameters.Add(new ParameterBase(TeamFoundationServerBuildServerBase.sqlexpress4build_password_parameter_name, "String", "askjd871hdj11", "Password for Master User For RDS SqlServer") { NoEcho = true });

            var buildServer = AddBuildServer(template, InstanceTypes.T2Small, subnetBuildServer, tfsServer, tfsApplicationTierInstalled, dcPackage, securityGroupBuildServer, rdsSqlExpress4Build);

            //uses 33gb
            var workstation = AddWorkstation(template,
                "Workstation",
                subnetWorkstation,
                dcPackage,
                workstationSecurityGroup);


            ////SecurityGroup elbSecurityGroup = new SecurityGroup(template, "ElbSecurityGroup", "Enables access to the ELB", vpc);
            ////elbSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            ////tfsServerSecurityGroup.AddIngress(elbSecurityGroup, Protocol.Tcp, Ports.TeamFoundationServerHttp);

            ////////////LoadBalancer elb = new LoadBalancer(template, "elb1");
            ////////////elb.AddInstance(tfsServer);
            ////////////elb.AddListener("8080", "8080", "http");
            ////////////elb.AddSubnet(DMZSubnet);
            ////////////elb.AddSecurityGroup(elbSecurityGroup);
            ////////////template.AddResource(elb);

            //////////the below is a remote desktop gateway server that can
            ////////// be uncommented to debug domain setup problems
            //AddRdp2(subnetDmz1, template, vpc, dcPackage);


            return template;
        }

        private static void AddRdp2(Subnet subnetDmz1, Template template, Vpc vpc, DomainControllerPackage dcPackage)
        {
            var instanceRdp2 = new Instance(subnetDmz1, InstanceTypes.T2Micro, "ami-e4034a8e", OperatingSystem.Windows);

            template.Resources.Add("rdp2", instanceRdp2);

            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("SecurityGroupForRdp2Rdp2", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);

            instanceRdp2.AddSecurityGroup(rdp);


            instanceRdp2.AddElasticIp();

            dcPackage.AddToDomainMemberSecurityGroup(instanceRdp2);
        }

        private static LaunchConfiguration AddSql(Template template, string instanceName, InstanceTypes instanceSize, 
            Subnet subnet, DomainControllerPackage domainControllerPackage, SecurityGroup sqlServerSecurityGroup)
        {
            var sqlServer = new Instance(subnet, instanceSize, UsEast1AWindows2012R2SqlServerExpressAmi, OperatingSystem.Windows);
            template.Resources.Add(instanceName,sqlServer);
            domainControllerPackage.Participate(sqlServer);
            var sqlServerPackage = new SqlServerExpressFromAmi(BucketNameSoftware);
            sqlServer.Packages.Add(sqlServerPackage);
            sqlServer.AddSecurityGroup(sqlServerSecurityGroup);
            return sqlServer;
        }

        private static Instance AddDomainController(Template template, Subnet subnet)
        {
            //"ami-805d79ea",
            var DomainController = new Instance(subnet,InstanceTypes.T2Micro, UsEast1AWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add("DomainController", DomainController);

            return DomainController;
        }

        public Template GetTemplateVolumeOnly(TestContext testContext)
        {
            Template t = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            Volume v = new Volume();
            t.Resources.Add("Volume1",v);
            v.SnapshotId = "snap-c4d7f7c3";
            v.AvailabilityZone = AvailabilityZone.UsEast1A;

            return t;
        }

        [TestMethod]
        public void CreateAutoScalingGroupTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);


            var launchConfig = new LaunchConfiguration(null, InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, OperatingSystem.Windows, ResourceType.AwsAutoScalingLaunchConfiguration);
            template.Resources.Add("Xyz", launchConfig );
            launchConfig.AssociatePublicIpAddress = true;
            launchConfig.AddSecurityGroup(rdp);



            var launchGroup = new AutoScalingGroup();
            template.Resources.Add("AutoGroup",launchGroup);
            launchGroup.LaunchConfiguration = launchConfig;
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
            Assert.IsTrue(HasGitDifferences());
        }

        public static bool HasGitDifferences()
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

            return output.Length != 0;
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
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet,InstanceTypes.T2Large, UsEast1AWindows2012R2Ami,OperatingSystem.Windows);
            template.Resources.Add("workstation",w);
            w.AddSecurityGroup(rdp);
            w.AddElasticIp();
            Volume v = new Volume() {Size = 1.ToString()};
            template.Resources.Add("Volume1",v);
            v.AvailabilityZone = AvailabilityZone.UsEast1E;
            w.AddDisk(v);
            template.Resources.Add("VolumeAttachment1",v);
            Stack.Stack.CreateStack(template);
        }

        [TestMethod]
        public void CreateStackBlockDeviceMappingFromSnapshotTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(w.LogicalId,w);
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
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet( vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(w.LogicalId, w);

            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-87e3eb87";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-5e27a85a";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-4e69d94b";
            w.AddBlockDeviceMapping(blockDeviceMapping);
            throw new NotImplementedException();
            //w.AddChefExec(BucketNameSoftware, "MountDrives.tar.gz", "MountDrives");
            //w.AddSecurityGroup(rdp);
            //w.AddElasticIp();
            //var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".",string.Empty) ;
            //Stack.Stack.CreateStack(template, name);
        }

        [TestMethod]
        public void UpdateStackWithVisualStudio()
        {
            var template = GetNewBlankTemplateWithVpc($"VpcCreateStackWithVisualStudio");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, OperatingSystem.Windows );
            template.Resources.Add(w.LogicalId, w);


            w.Packages.Add(new VisualStudio(BucketNameSoftware));
            w.Packages.Add(new SqlServerExpress(BucketNameSoftware));

            w.AddSecurityGroup(rdp);
            w.AddElasticIp();
            var name = "CreateStackWithVisualStudio-2016-01-30T1711018619021-0500";
            Stack.Stack.UpdateStack(name, template);
        }

        [TestMethod]
        public void CreateStackWithSimpleCommand()
        {
            var template = GetCreateStackWithSimpleCommand();
            var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);
            Stack.Stack.CreateStack(template, name);
        }
        [TestMethod]
        public void UpdateStackWithSimpleCommand()
        {
            var template = GetCreateStackWithSimpleCommand();
            var name = "CreateStackWithSimpleCommand-2016-01-31T2139494959420-0500";
            Stack.Stack.UpdateStack(name,template);
        }

        private static Template GetCreateStackWithSimpleCommand()
        {
            var template = GetNewBlankTemplateWithVpc($"VpcCreateStackWithVisualStudio");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet,InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(w.LogicalId,w);

            Dir1 d = new Dir1();
            w.Packages.Add(d);
            WaitCondition wc = d.WaitCondition;
            Dir2 d2 = new Dir2();
            w.Packages.Add(d2);
            WaitCondition wc2 = d2.WaitCondition;
            w.AddSecurityGroup(rdp);
            w.AddElasticIp();
            return template;
        }


        [TestMethod]
        public void CreateStackWithVisualStudio()
        {
            var template = GetNewBlankTemplateWithVpc($"VpcCreateStackWithVisualStudio");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, OperatingSystem.Windows );
            template.Resources.Add(w.LogicalId, w);


            w.Packages.Add(new VisualStudio(BucketNameSoftware));
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
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = AddWorkstation(template, "Windows1", DMZSubnet, null, rdp);
            w.AddElasticIp();
            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateGenericInstance()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(w.LogicalId,w);
            w.AddSecurityGroup(rdp);
            w.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateBuildServer()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            var dc1 = AddDomainController(template, DMZSubnet);
            var dcPackage = dc1.Packages.First() as DomainControllerPackage;
            dc1.AddElasticIp();
            dc1.AddSecurityGroup(rdp);
            var w = AddBuildServer(template, InstanceTypes.T2Nano,  DMZSubnet, null, null, dcPackage, rdp,null);
            throw new NotImplementedException();
            //w.AddElasticIp();

            //CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateDomainController()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDomainController1Subnet, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("PrivateSubnet", DMZSubnet);


            Instance w = AddDomainController(template, DMZSubnet);
            w.AddSecurityGroup(rdp);

            w.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateMinimalInstanceTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);


            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(w.LogicalId, w);

            w.AddSecurityGroup(rdp);
            w.AddElasticIp();
            CreateTestStack(template, this.TestContext);

        }

        


        [TestMethod]
        public void CreateISOMaker()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);


            Instance workstation = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(workstation.LogicalId, workstation);

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
            var subnet1 = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("subnet1", subnet1);

            var subnet2 = new Subnet(vpc, CidrDmz2, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("subnet2", subnet2);

            var subnet3 = new Subnet(vpc, CidrDomainController1Subnet, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("subnet3", subnet3);

            var subnet4 = new Subnet(vpc, CidrDomainController2Subnet, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("subnet4", subnet4);


            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        [Timeout(int.MaxValue)]
        public void CfnInitOverWriteTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);


            Instance workstation = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(workstation.LogicalId, workstation);

            workstation.AddSecurityGroup(rdp);
            workstation.AddElasticIp();


            var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);

            CreateTestStack(template, this.TestContext, name);

            throw new NotImplementedException();
            //workstation.Metadata.Init.ConfigSets.GetConfigSet("x").GetConfig("y").Commands.AddCommand<Command>("z").Command.AddCommandLine(true, "dir");

            //Thread.Sleep(new TimeSpan(0, 60, 0));
            //Stack.Stack.UpdateStack(name, template);
        }


        [TestMethod]
        public void SerializerTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("Allows Remote Desktop Access", vpc);
            template.Resources.Add("SecurityGroupRdp", rdp);

            System.Diagnostics.Debug.WriteLine(rdp.Vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            
            Subnet DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            RouteTable dmzRouteTable = new RouteTable(vpc);
            template.Resources.Add("DMZRouteTable", dmzRouteTable);

            Route dmzRoute = new Route(vpc.InternetGateway, "0.0.0.0/0", dmzRouteTable);
            template.Resources.Add("DMZRoute", dmzRoute);

            SubnetRouteTableAssociation DMZSubnetRouteTableAssociation = new SubnetRouteTableAssociation(DMZSubnet, dmzRouteTable);
            template.Resources.Add(DMZSubnetRouteTableAssociation.LogicalId, DMZSubnetRouteTableAssociation);
            Instance workstation = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(workstation.LogicalId, workstation);

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
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEast1AWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(w.LogicalId, w);

            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.AddBlockDeviceMapping(blockDeviceMapping);
            throw new NotImplementedException();
            //w.AddChefExec(BucketNameSoftware, "MountDrives.tar.gz", "MountDrives");


            //w.AddSecurityGroup(rdp);
            //w.AddElasticIp();
            //var name = "CreateStackWithMounterTest-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);
            //Stack.Stack.CreateStack(template, name);
        }


        private static LaunchConfiguration AddBuildServer(
            Template template, 
            InstanceTypes instanceSize, 
            Subnet subnet, 
            Instance tfsServer,
            WaitCondition tfsServerComplete, 
            DomainControllerPackage domainControllerPackage, 
            SecurityGroup buildServerSecurityGroup, 
            DbInstance sqlExpress4Build)
        {

            AutoScalingGroup launchGroup = new AutoScalingGroup();
            template.Resources.Add("BuildServerAutoScalingGroup",launchGroup);
            launchGroup.MinSize = 1.ToString();
            launchGroup.MaxSize = 2.ToString();
            launchGroup.AddAvailabilityZone(AvailabilityZone.UsEast1A);
            launchGroup.AddSubnetToVpcZoneIdentifier(subnet);


            var buildServer = new LaunchConfiguration(null,instanceSize, UsEast1AWindows2012R2Ami, OperatingSystem.Windows, ResourceType.AwsAutoScalingLaunchConfiguration);
            template.Resources.Add("LaunchConfigurationBuildServer",buildServer);

            launchGroup.LaunchConfiguration = buildServer;

            buildServer.AddBlockDeviceMapping("/dev/sda1", 100, Ebs.VolumeTypes.GeneralPurpose);

            domainControllerPackage.Participate(buildServer);
            buildServer.Packages.Add(new VisualStudio(BucketNameSoftware));
            buildServer.Packages.Add(new TeamFoundationServerBuildServerAgentOnly(tfsServer, BucketNameSoftware, sqlExpress4Build));

            if (tfsServerComplete != null)
            {
                buildServer.AddDependsOn(tfsServerComplete);
            }
            if (sqlExpress4Build != null)
            {
                buildServer.DependsOn.Add(sqlExpress4Build.LogicalId);
            }

            var chefNode = buildServer.GetChefNodeJsonContent();
            var domainAdminUserInfoNode = chefNode.AddNode("domainAdmin");
            var domainInfo = new DomainInfo(DomainDnsName, DomainAdminUser, new ReferenceProperty(Template.ParameterDomainAdminPassword));

            domainAdminUserInfoNode.Add("name", domainInfo.DomainNetBiosName + "\\" + DomainAdminUser);
            domainAdminUserInfoNode.Add("password", new ReferenceProperty(Template.ParameterDomainAdminPassword));
            buildServer.AddSecurityGroup(buildServerSecurityGroup);
            //var waitConditionBuildServerAvailable = buildServer.AddFinalizer("waitConditionBuildServerAvailable",TimeoutMax);

            return buildServer;
        }

        private static Instance AddWorkstation(  Template template, 
                                                        string name, 
                                                        Subnet subnet, 
                                                        DomainControllerPackage instanceDomainControllerPackage, 
                                                        SecurityGroup workstationSecurityGroup)
        {
            if (subnet == null) throw new ArgumentNullException(nameof(subnet));

            Instance workstation = new Instance(subnet, InstanceTypes.T2Large, UsEast1AWindows2012R2SqlServerExpressAmi, OperatingSystem.Windows, Ebs.VolumeTypes.GeneralPurpose, 214);
            template.Resources.Add("Workstation",workstation);

            if (instanceDomainControllerPackage != null)
            {
                instanceDomainControllerPackage.Participate(workstation);
            }

            if (workstationSecurityGroup != null)
            {
                workstation.AddSecurityGroup(workstationSecurityGroup);
            }

            //workstation.Packages.Add(new SqlServerExpress(BucketNameSoftware));
            workstation.Packages.Add(new Iis(BucketNameSoftware));
            workstation.Packages.Add(new VisualStudio(BucketNameSoftware));
            workstation.Packages.Add(new ReSharper());
            workstation.Packages.Add(new Chrome());

            //var waitConditionWorkstationAvailable = workstation.AddFinalizer("waitConditionWorkstationAvailable",TimeoutMax);


            return workstation;
        }

        private static Instance AddTfsServer(Template template,
            InstanceTypes instanceSize, 
            Subnet privateSubnet1, 
            LaunchConfiguration sqlServer4Tfs, 
            DomainControllerPackage dc1, 
            SecurityGroup tfsServerSecurityGroup)
        {
            var tfsServer = new Instance(privateSubnet1,instanceSize,UsEast1AWindows2012R2Ami, OperatingSystem.Windows,Ebs.VolumeTypes.GeneralPurpose,
                                                    214);

            template.Resources.Add("Tfs",tfsServer);


            dc1.Participate(tfsServer);
            tfsServer.AddDependsOn(sqlServer4Tfs.Packages.Last().WaitCondition);

            var chefNode = tfsServer.GetChefNodeJsonContent();
            var domainAdminUserInfoNode = chefNode.AddNode("domainAdmin");
            var domainInfo = new DomainInfo(DomainDnsName, DomainAdminUser, new ReferenceProperty(Template.ParameterDomainAdminPassword));
            domainAdminUserInfoNode.Add("name", domainInfo.DomainNetBiosName + "\\" + DomainAdminUser);
            domainAdminUserInfoNode.Add("password", new ReferenceProperty(Template.ParameterDomainAdminPassword));
            tfsServer.AddSecurityGroup(tfsServerSecurityGroup);
            var packageTfsApplicationTier = new TeamFoundationServerApplicationTier(BucketNameSoftware,sqlServer4Tfs);
            tfsServer.Packages.Add(packageTfsApplicationTier);
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
            var nat1 = new Instance(null,InstanceTypes.T2Micro,"ami-4c9e4b24",OperatingSystem.Linux)
            {
                SourceDestCheck = false
            };

            template.Resources.Add("Nat1", nat1);


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
            var version = string.Empty;
            Greek maxVersion = Greek.Alpha;

            foreach (var thisGreek in Enum.GetValues(typeof(Greek)))
            {
                if (stacks.Any(s => s.Name.ToLowerInvariant().StartsWith(thisGreek.ToString().ToLowerInvariant().Replace('.', '-'))))
                {
                    maxVersion = (Greek)thisGreek;
                }
            }
            version = ((Greek)((int) maxVersion + 1)).ToString();
            DomainDnsName = "${version}.yadayada.software";


        var templateToCreateStack = GetTemplateFullStack(version);
            templateToCreateStack.StackName = $"{version}-{StackTest.DomainDnsName}".Replace('.', '-');

            CreateTestStack(templateToCreateStack, this.TestContext);
        }


        [TestMethod]
        public void UpdateDevelopmentTest()
        {
            var stackName = "alphayadayada-software";
            var template = GetTemplateFullStack("alpha");
            ((ParameterBase)template.Parameters[Template.ParameterDomainAdminPassword]).Default = "SWGP2720dtbt";
            Stack.Stack.UpdateStack(stackName,template );
        }



        [TestMethod]
        public void AddingSameResourceTwiceFails()
        {
            var t = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var v = new Vpc("10.0.0.0/16");
            t.Resources.Add("X", v);

            var s = new Subnet(v,null,AvailabilityZone.UsEast1A, true);
            t.Resources.Add("Vpc1", s);


            ArgumentException expectedException = null;

            try
            {
                s = new Subnet(v, null, AvailabilityZone.UsEast1A, true);
                t.Resources.Add("Vpc1", s);

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
