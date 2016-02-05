using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.RDS;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AWS.CloudFormation.Test.Resource.RDS
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class DbInstanceTest
    {


        [TestMethod]
        public void SqlExpressDbInstanceTest()
        {
            Stack.Stack.CreateStack(GetSqlExpressDbTemplate(), this.TestContext.TestName + DateTime.Now.Ticks );
        }

        [TestMethod]
        public void AuroraDbInstanceTest()
        {
            Stack.Stack.CreateStack(GetSingleDbAuroraTemplate(), this.TestContext.TestName + DateTime.Now.Ticks);
        }

        [TestMethod]
        public void MySqlDbInstanceTest()
        {
            Stack.Stack.CreateStack(GetMySqlTemplate(), this.TestContext.TestName + DateTime.Now.Ticks);
        }

        private Template GetSqlExpressDbTemplate()
        {
            var template = new Template("corp.getthebuybox.com", "vpcBasicDbInstanceTest", "10.0.0.0/16");

            var vpc = template.Vpcs.First();
            vpc.EnableDnsHostnames = true;
            vpc.EnableDnsSupport = true;

            var subnet1 = new Subnet(vpc, "10.0.128.0/28", AvailabilityZone.UsEast1A);
            template.Resources.Add("subnetDb1",subnet1);
            RouteTable routeTable4Subnet1 = new RouteTable(vpc);
            template.Resources.Add("routeTable4Subnet1", routeTable4Subnet1);

            Route route4subnet1 = new Route(vpc.InternetGateway, "0.0.0.0/0", routeTable4Subnet1);
            template.Resources.Add("route4subnet1", route4subnet1);

            SubnetRouteTableAssociation subnetRouteTableAssociationSubnet1 = new SubnetRouteTableAssociation(subnet1, routeTable4Subnet1);
            template.Resources.Add(subnetRouteTableAssociationSubnet1.LogicalId, subnetRouteTableAssociationSubnet1);


            var subnet2 = new Subnet(template.Vpcs.First(), "10.0.64.0/28", AvailabilityZone.UsEast1E);
            template.Resources.Add("subnetDb2", subnet2);

            RouteTable routeTable4Subnet2 = new RouteTable(vpc);
            template.Resources.Add("routeTable4Subnet2", routeTable4Subnet2);

            Route route4subnet2 = new Route(vpc.InternetGateway, "0.0.0.0/0", routeTable4Subnet2);
            template.Resources.Add("route4subnet2", route4subnet2);


            SubnetRouteTableAssociation subnetRouteTableAssociationSubnet2 = new SubnetRouteTableAssociation(subnet2, routeTable4Subnet2);
            template.Resources.Add(subnetRouteTableAssociationSubnet2.LogicalId, subnetRouteTableAssociationSubnet2);


            var subnetGroup = new DbSubnetGroup("this is my subnet group description");
            template.Resources.Add("dbSubnetGroup", subnetGroup);
            subnetGroup.AddSubnet(subnet1);
            subnetGroup.AddSubnet(subnet2);

            SecurityGroup securityGroup = new SecurityGroup("Allows access to SqlServer from everywhere", vpc);
            template.Resources.Add("securityGroupWorldWide", securityGroup);

            securityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.MsSqlServer);

            var dbSecurityGroup = new DbSecurityGroup(vpc, "Why is a description required?");
            template.Resources.Add("dbSecurityGroup", dbSecurityGroup);
            var dbSecurityGroupIngress = new DbSecurityGroupIngress();
            dbSecurityGroupIngress.CIDRIP = "0.0.0.0/0";
            dbSecurityGroup.AddDbSecurityGroupIngress(dbSecurityGroupIngress);

            DbInstance instance = new DbInstance(DbInstanceClassEnum.DbT2Micro,
                EngineType.SqlServerExpress,
                LicenseModelType.LicenseIncluded,
                Ebs.VolumeTypes.GeneralPurpose,
                20,
                "MyMasterUsername",
                "YellowBeard123",
                subnetGroup,
                dbSecurityGroup);

            template.Resources.Add("instanceBasicDbInstanceTest",instance);



            instance.PubliclyAccessible = true.ToString().ToLowerInvariant();
            return template;
        }

        private Template GetMySqlTemplate()
        {
            var template = new Template("corp.getthebuybox.com", "vpcBasicDbInstanceTest", "10.0.0.0/16");

            var vpc = template.Vpcs.First();
            vpc.EnableDnsHostnames = true;
            vpc.EnableDnsSupport = true;

            var subnet1 = new Subnet(vpc, "10.0.128.0/28", AvailabilityZone.UsEast1A);
            template.Resources.Add("subnetDb1", subnet1);

            RouteTable routeTable4Subnet1 = new RouteTable(vpc);
            template.Resources.Add("routeTable4Subnet1", routeTable4Subnet1);

            Route route4subnet1 = new Route(vpc.InternetGateway, "0.0.0.0/0", routeTable4Subnet1);
            template.Resources.Add("route4subnet1", route4subnet1);

            SubnetRouteTableAssociation subnetRouteTableAssociationSubnet1 = new SubnetRouteTableAssociation(subnet1, routeTable4Subnet1);
            template.Resources.Add(subnetRouteTableAssociationSubnet1.LogicalId, subnetRouteTableAssociationSubnet1);

            var subnet2 = new Subnet(template.Vpcs.First(), "10.0.64.0/28", AvailabilityZone.UsEast1E);
            template.Resources.Add("subnetDb2", subnet2);

            RouteTable routeTable4Subnet2 = new RouteTable(vpc);
            template.Resources.Add("routeTable4Subnet2", routeTable4Subnet2);

            Route route4subnet2 = new Route(vpc.InternetGateway, "0.0.0.0/0", routeTable4Subnet2);
            template.Resources.Add("route4subnet2", route4subnet2);

            SubnetRouteTableAssociation subnetRouteTableAssociationSubnet2 = new SubnetRouteTableAssociation(subnet2, routeTable4Subnet2);
            template.Resources.Add(subnetRouteTableAssociationSubnet2.LogicalId, subnetRouteTableAssociationSubnet2);

            var subnetGroup = new DbSubnetGroup("this is my subnet group description");
            template.Resources.Add("dbSubnetGroup", subnetGroup);

            subnetGroup.AddSubnet(subnet1);
            subnetGroup.AddSubnet(subnet2);

            SecurityGroup securityGroup = new SecurityGroup("Allows access to SqlServer from everywhere", vpc);
            template.Resources.Add("securityGroupWorldWide", securityGroup);
            securityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.MySql);

            var dbSecurityGroup = new DbSecurityGroup(vpc, "Why is a description required?");
            template.Resources.Add("dbSecurityGroup", dbSecurityGroup);

            var dbSecurityGroupIngress = new DbSecurityGroupIngress();
            dbSecurityGroupIngress.CIDRIP = "0.0.0.0/0";
            dbSecurityGroup.AddDbSecurityGroupIngress(dbSecurityGroupIngress);

            DbInstance instance = new DbInstance(DbInstanceClassEnum.DbT2Micro,
                EngineType.MySql,
                LicenseModelType.GeneralPublicLicense,
                  Ebs.VolumeTypes.GeneralPurpose,
                  20,
                "MyMasterUsername",
                "YellowBeard123",
                subnetGroup,
                dbSecurityGroup);

            template.Resources.Add("instanceBasicDbInstanceTest", instance);
            instance.PubliclyAccessible = true.ToString().ToLowerInvariant();
            return template;
        }
        private Template GetSingleDbAuroraTemplate()
        {
            var template = new Template("corp.getthebuybox.com", "vpcBasicDbInstanceTest", "10.0.0.0/16");

            var vpc = template.Vpcs.First();
            vpc.EnableDnsHostnames = true;
            vpc.EnableDnsSupport = true;

            var subnet1 = new Subnet(vpc, "10.0.128.0/28", AvailabilityZone.UsEast1A);
            template.Resources.Add("subnetDb1", subnet1);

            RouteTable routeTable4Subnet1 = new RouteTable(vpc);
            template.Resources.Add("routeTable4Subnet1", routeTable4Subnet1);

            Route route4subnet1 = new Route(vpc.InternetGateway, "0.0.0.0/0", routeTable4Subnet1);
            template.Resources.Add("route4subnet1", route4subnet1);

            SubnetRouteTableAssociation subnetRouteTableAssociationSubnet1 = new SubnetRouteTableAssociation(subnet1, routeTable4Subnet1);
            template.Resources.Add(subnetRouteTableAssociationSubnet1.LogicalId, subnetRouteTableAssociationSubnet1);

            var subnet2 = new Subnet(template.Vpcs.First(), "10.0.64.0/28", AvailabilityZone.UsEast1E);
            template.Resources.Add("subnetDb2", subnet2);

            RouteTable routeTable4Subnet2 = new RouteTable(vpc);
            template.Resources.Add("routeTable4Subnet2", routeTable4Subnet2);

            Route route4subnet2 = new Route(vpc.InternetGateway, "0.0.0.0/0", routeTable4Subnet2);
            template.Resources.Add("route4subnet2", route4subnet2);

            SubnetRouteTableAssociation subnetRouteTableAssociationSubnet2 = new SubnetRouteTableAssociation(subnet2, routeTable4Subnet2);
            template.Resources.Add(subnetRouteTableAssociationSubnet2.LogicalId, subnetRouteTableAssociationSubnet2);

            var subnetGroup = new DbSubnetGroup("this is my subnet group description");
            template.Resources.Add("dbSubnetGroup", subnetGroup);

            subnetGroup.AddSubnet(subnet1);
            subnetGroup.AddSubnet(subnet2);

            SecurityGroup securityGroup = new SecurityGroup("Allows access to SqlServer from everywhere", vpc);
            template.Resources.Add("securityGroupWorldWide", securityGroup);

            securityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.MsSqlServer);

            var dbSecurityGroup = new DbSecurityGroup(vpc, "Why is a description required?");
            template.Resources.Add("dbSecurityGroup", dbSecurityGroup);


            var dbSecurityGroupIngress = new DbSecurityGroupIngress();
            dbSecurityGroupIngress.CIDRIP = "0.0.0.0/0";
            dbSecurityGroup.AddDbSecurityGroupIngress(dbSecurityGroupIngress);

            DbInstance instance = new DbInstance(DbInstanceClassEnum.DbR3Large,
                EngineType.Aurora,
                LicenseModelType.GeneralPublicLicense,
                Ebs.VolumeTypes.GeneralPurpose,
                100,
                "MyMasterUsername",
                "YellowBeard123",
                subnetGroup,
                dbSecurityGroup);

            template.Resources.Add("instanceBasicDbInstanceTest", instance);

            instance.PubliclyAccessible = true.ToString().ToLowerInvariant();
            return template;
        }

        [TestMethod]
        public void UpdateDbTest()
        {
            var stackName = "BasicDbInstanceTest635896256671970189";
            Stack.Stack.UpdateStack(stackName, GetSqlExpressDbTemplate());
        }


        [TestMethod]
        public void UpdateMySql()
        {
            var stackName = "MySqlDbInstanceTest635896351095707209";
            Stack.Stack.UpdateStack(stackName, GetSqlExpressDbTemplate());
        }


        #region "Test Stuff"
        public DbInstanceTest()
        {
            //
            // TODO: Add constructor logic here
            //
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

        [TestMethod]
        public void TestMethod1()
        {
            //
            // TODO: Add test logic here
            //
        }
        #endregion
    }
}
