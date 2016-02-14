using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using AWS.CloudFormation.Configuration.Packages;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.ElasticLoadBalancing;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;

namespace AWS.CloudFormation.Test
{

    /// <summary>
    /// Summary description for CreateTestEnvironment
    /// </summary>
    [TestClass]
    public class CreateTestEnvironment
    {

        [TestMethod]
        public void CreateTestEnvironmentTest()
        {
            var baseName = "test.app1.yadayadasoftware.com";
            var stackBaseName = baseName.Replace('.', '-');
            var stacks = Stack.Stack.GetActiveStacks().Where(s => s.Name.Contains(stackBaseName)).Select(s => s.Name);
            StackTest.Greek maxVersion = StackTest.Greek.None;

            foreach (var stack in stacks)
            {
                var stackVersionString = stack.Split('-')[0];
                maxVersion = (StackTest.Greek)System.Enum.Parse(typeof(StackTest.Greek), stackVersionString, true);
            }
            maxVersion++;
            var name = $"{maxVersion}.{baseName}";

            Template t = GetTestEnvironmentTemplate(name);
            Stack.Stack.CreateStack(t);
        }

        [TestMethod]
        public void UpdateTestEnvironmentTest()
        {
            var name = "Delta.test.app1.yadayadasoftware.com";
            Template t = GetTestEnvironmentTemplate(name);
            Stack.Stack.UpdateStack(t);
        }
        private static Template GetTestEnvironmentTemplate(string domain)
        {
            Template returnTemplate = new Template(domain, "TestApp1YadayadaSoftwareComVpc", StackTest.CidrVpc);
            returnTemplate.StackName = domain.Replace('.','-');

            Vpc vpc = returnTemplate.Vpcs.Last();

            Subnet subnetDmz = new Subnet(vpc,"10.0.0.0/24", AvailabilityZone.UsEast1A, true);
            returnTemplate.Resources.Add("subnetDmz",subnetDmz);
            Instance instanceWebServer = new Instance(subnetDmz,InstanceTypes.T2Nano, StackTest.UsEastWindows2012R2Ami,OperatingSystem.Windows);
            returnTemplate.Resources.Add("instanceWebServer",instanceWebServer);
            instanceWebServer.Packages.Add(new InternetInformationServerPackage(null, "gtbb", "yadayada_iis"));

            SecurityGroup securityGroupLoadBalancer = new SecurityGroup("Security Group for ELB",vpc);
            securityGroupLoadBalancer.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.Ssl);
            returnTemplate.Resources.Add(securityGroupLoadBalancer.LogicalId, securityGroupLoadBalancer);

            LoadBalancer loadBalancer = new LoadBalancer();
            loadBalancer.Subnets.Add(new ReferenceProperty(subnetDmz));
            loadBalancer.SecurityGroups.Add(securityGroupLoadBalancer);
            LoadBalancer.Listener listenerHttps = new LoadBalancer.Listener((int)Ports.Ssl, (int)Ports.Http,"https");
            listenerHttps.SSLCertificateId = "arn:aws:acm:us-east-1:570182474766:certificate/5249aee1-0a70-4a45-a1f9-914173ba7a98";

            loadBalancer.Listeners.Add(listenerHttps);
            returnTemplate.Resources.Add("LoadBalancer", loadBalancer);

            

            SecurityGroup securityGroupElbToWebServer = new SecurityGroup("Allows Elb To Web Server",vpc);
            returnTemplate.Resources.Add(securityGroupElbToWebServer.LogicalId, securityGroupElbToWebServer);
            securityGroupElbToWebServer.AddIngress(securityGroupLoadBalancer, Protocol.Tcp, Ports.Http);
            instanceWebServer.AddSecurityGroup(securityGroupElbToWebServer);

            var x = instanceWebServer.Packages.Last().WaitCondition;

            instanceWebServer.AddElasticIp();
            SecurityGroup securityGroupRdpFromFairfaxToWebServer = new SecurityGroup("Allows RDP access from Fairfax",vpc);
            returnTemplate.Resources.Add(securityGroupRdpFromFairfaxToWebServer.LogicalId, securityGroupRdpFromFairfaxToWebServer);
            securityGroupRdpFromFairfaxToWebServer.AddIngress(new Fairfax(), Protocol.All, Ports.RemoteDesktopProtocol);
            instanceWebServer.AddSecurityGroup(securityGroupRdpFromFairfaxToWebServer);



            return returnTemplate;

        }

        public class Fairfax : ICidrBlock
        {
            public string CidrBlock {
                get { return "96.231.30.130/32"; }
                set { throw new NotSupportedException(); }
            }
        }

        #region "TestStuff"
        public CreateTestEnvironment()
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
        #endregion

    }

}
