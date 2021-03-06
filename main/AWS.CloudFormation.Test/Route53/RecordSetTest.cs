﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Route53;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;


namespace AWS.CloudFormation.Test.Route53
{
    /// <summary>
    /// Summary description for RecordSetTest
    /// </summary>
    [TestClass]
    public class RecordSetTest
    {


        [TestMethod]
        public void RecordSetByZoneIdTest()
        {
            Template template = StackTest.GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            string recordSetName = $"A{DateTime.Now.Ticks.ToString().Substring(10, 5)}";
            recordSetName = "abc";
            var target = RecordSet.AddByHostedZoneId(template, recordSetName, "Z1H285MI71YUD0", recordSetName + ".sircupsalot.com.", RecordSet.RecordSetTypeEnum.A);
            target.RecordSetType = RecordSet.RecordSetTypeEnum.A.ToString();
            target.AddResourceRecord("206.190.36.45");
            StackTest.CreateTestStack(template, this.TestContext);
        }

        [TestMethod]
        public void RecordSetByZoneNameTest()
        {
            Template template = StackTest.GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            string recordSetName = $"A{DateTime.Now.Ticks.ToString().Substring(10, 5)}";
            recordSetName = "abc";
            var target = RecordSet.AddByHostedZoneName(template, recordSetName, "sircupsalot.com.", recordSetName + ".sircupsalot.com.", RecordSet.RecordSetTypeEnum.A);
            target.RecordSetType = RecordSet.RecordSetTypeEnum.A.ToString();
            target.AddResourceRecord("192.168.0.1");
            target.AddResourceRecord("192.168.0.2");
            StackTest.CreateTestStack(template, this.TestContext);
        }

        [TestMethod]
        public void RecordSetMappedToEipTest()
        {
            Template template = StackTest.GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var DMZSubnet = new Subnet(template.Vpcs.First(), "10.0.0.0/20", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance testBox = new Instance(DMZSubnet,InstanceTypes.T2Micro, "ami-60b6c60a", OperatingSystem.Linux);
            template.Resources.Add("testbox", testBox);
            var eip = testBox.AddElasticIp();
            var target = RecordSet.AddByHostedZoneName(template, "test", "getthebuybox.com.", "test.test.getthebuybox.com.", RecordSet.RecordSetTypeEnum.A);
            target.TTL = "60";
            target.RecordSetType = RecordSet.RecordSetTypeEnum.A.ToString();
            target.AddResourceRecord(new ReferenceProperty(eip));
            StackTest.CreateTestStack(template, this.TestContext);
        }

        [TestMethod]
        public void RecordSetByNewHostZoneTest()
        {
            Template template = StackTest.GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            HostedZone hz = new HostedZone("zeta.yadayada.software.");
            template.Resources.Add("hostedZoneRecordSetByNewHostZoneTest", hz);
            hz.AddVpc(template.Vpcs.First(), Region.UsEast1);
            var target = RecordSet.AddByHostedZone(template, "test", hz, "test.zeta.yadayada.software.", RecordSet.RecordSetTypeEnum.A);
            target.TTL = "60";
            target.RecordSetType = RecordSet.RecordSetTypeEnum.A.ToString();
            var DMZSubnet = new Subnet(template.Vpcs.First(), "10.0.0.0/20", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance testBox = new Instance(DMZSubnet,InstanceTypes.T2Micro, "ami-60b6c60a", OperatingSystem.Linux);
            template.Resources.Add("testbox", testBox);
            var eip = testBox.AddElasticIp();
            target.AddResourceRecord(eip);
            StackTest.CreateTestStack(template, this.TestContext);
        }

        #region "TestStuff"
        public RecordSetTest()
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
