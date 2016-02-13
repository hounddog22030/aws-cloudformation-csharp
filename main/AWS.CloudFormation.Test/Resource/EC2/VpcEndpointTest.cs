﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using AWS.CloudFormation.Configuration.Packages;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;

namespace AWS.CloudFormation.Test.Resource.EC2
{
    /// <summary>
    /// Summary description for VpcEndpoint
    /// </summary>
    [TestClass]
    public class VpcEndpointTest
    {
        public VpcEndpointTest()
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
        public void BasicVpcEnpointTest()
        {
            var name = this.TestContext.TestName + DateTime.Now.Ticks;
            Stack.Stack.CreateStack(GetEndpointTemplate(name), this.TestContext.TestName + DateTime.Now.Ticks);
        }

        [TestMethod]
        public void UpdateBasicVpcEnpointTest()
        {
            var name = "BasicVpcEnpointTest635909036787858272";
            Stack.Stack.UpdateStack(name, GetEndpointTemplate(name));
        }

        private Template GetEndpointTemplate(string nameBase)
        {
            var template = new Template(StackTest.KeyPairName,$"Vpc{nameBase}",StackTest.CidrVpc, "Vpc Description");

            var password = "a1111sdjfkAAAA";

            var domainPassword = new ParameterBase(DomainControllerPackage.DomainAdminPasswordParameterName, "String", password, "Password for domain administrator.")
            {
                NoEcho = true
            };

            template.Parameters.Add(domainPassword);
            template.Parameters.Add(new ParameterBase(DomainControllerPackage.DomainTopLevelNameParameterName, "String", "nothing.nothing", "Top level domain name for the stack (e.g. example.com)"));
            template.Parameters.Add(new ParameterBase(DomainControllerPackage.DomainAppNameParameterName, "String", "nothing", "Name of the application (e.g. Dev,Test,Prod)"));
            template.Parameters.Add(new ParameterBase(DomainControllerPackage.DomainVersionParameterName, "String", StackTest.Greek.Alpha, "Fully qualified domain name for the stack (e.g. example.com)"));
            template.Parameters.Add(new ParameterBase(DomainControllerPackage.DomainNetBiosNameParameterName, "String", StackTest.Greek.Alpha + "nothing", "NetBIOS name of the domain for the stack.  (e.g. Dev,Test,Production)"));
            template.Parameters.Add(new ParameterBase(DomainControllerPackage.DomainAdminUsernameParameterName, "String", "johnny", "Domain Admin User"));
            template.Parameters.Add(new ParameterBase(TeamFoundationServerBuildServerBase.TfsServiceAccountNameParameterName, "String", "tfsservice", "Account name for Tfs Application Server Service and Tfs SqlServer Service"));
            template.Parameters.Add(new ParameterBase(TeamFoundationServerBuildServerBase.TfsServicePasswordParameterName, "String", "Hello12345.", "Password for Tfs Application Server Service and Tfs SqlServer Service Account "));
            template.Parameters.Add(new ParameterBase(TeamFoundationServerBuildServerBase.sqlexpress4build_username_parameter_name, "String", "sqlservermasteruser", "Master User For RDS SqlServer"));
            template.Parameters.Add(new ParameterBase(TeamFoundationServerBuildServerBase.sqlexpress4build_password_parameter_name, "String", "askjd871hdj11", "Password for Master User For RDS SqlServer") { NoEcho = true });

            var vpc = template.Vpcs.Last();

            RouteTable routeTableForDomainControllerSubnet = new RouteTable(vpc);
            template.Resources.Add($"RouteTable1", routeTableForDomainControllerSubnet);
            

            VpcEndpoint endpoint = new VpcEndpoint("s3",template.Vpcs.First(), routeTableForDomainControllerSubnet);
            template.Resources.Add($"VpcEndpoint4{nameBase}",endpoint);

            //Route routeForSubnetDomainController1ToVpcEndpoint = new Route("0.0.0.0/0", routeTableForSubnetsToNat1)
            //{
            //    Gateway = endpoint
            //};
            //routeForSubnetDomainController1ToVpcEndpoint.DependsOn.Add(endpoint.LogicalId);

            //template.Resources.Add("routeForSubnetDomainController1ToVpcEndpoint", routeForSubnetDomainController1ToVpcEndpoint);

            Subnet subnetDomainController1 = StackTest.AddSubnet4DomainController(vpc, routeTableForDomainControllerSubnet, null, template);
            //SubnetRouteTableAssociation srta = new SubnetRouteTableAssociation(subnetDomainController1,routeTableForSubnetsToNat1);
            //template.Resources.Add("srta", srta);


            Instance instanceDomainController = new Instance(subnetDomainController1, InstanceTypes.T2Nano, StackTest.UsEastWindows2012R2Ami, OperatingSystem.Windows, Ebs.VolumeTypes.GeneralPurpose, 50);
            template.Resources.Add("DomainController", instanceDomainController);
            var dcPackage = new DomainControllerPackage(subnetDomainController1);
            instanceDomainController.Packages.Add(dcPackage);

            var DMZSubnet = new Subnet(vpc, StackTest.CidrDmz1, AvailabilityZone.UsEast1A,true);
            template.Resources.Add("DMZSubnet", DMZSubnet);
            StackTest.AddRdp2(DMZSubnet, template, vpc, dcPackage);

            return template;
        }
    }
}
