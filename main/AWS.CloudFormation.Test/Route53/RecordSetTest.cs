using System;
using System.Text;
using System.Collections.Generic;
using AWS.CloudFormation.Resource.Route53;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AWS.CloudFormation.Test.Route53
{
    /// <summary>
    /// Summary description for RecordSetTest
    /// </summary>
    [TestClass]
    public class RecordSetTest
    {
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

        [TestMethod]
        public void RecordSetTestByZoneId()
        {
            Template template = StackTest.GetNewBlankTemplateWithVpc(this.TestContext);
            string recordSetName = $"A{DateTime.Now.Ticks.ToString().Substring(10, 5)}";
            recordSetName = "abc";
            var target = RecordSet.CreateByHostedZoneId(template, recordSetName, "Z1H285MI71YUD0", recordSetName + ".sircupsalot.com.");
            target.RecordSetType = RecordSet.RecordSetTypeEnum.A.ToString();
            target.AddResourceRecord("206.190.36.45");
            template.AddResource(target);
            StackTest.CreateTestStack(template, this.TestContext);
        }

        [TestMethod]
        public void RecordSetTestByZoneName()
        {
            Template template = StackTest.GetNewBlankTemplateWithVpc(this.TestContext);
            string recordSetName = $"A{DateTime.Now.Ticks.ToString().Substring(10, 5)}";
            recordSetName = "abc";
            var target = RecordSet.CreateByHostedZoneName(template, recordSetName, "sircupsalot.com.", recordSetName + ".sircupsalot.com.");
            target.RecordSetType = RecordSet.RecordSetTypeEnum.A.ToString();
            target.AddResourceRecord("192.168.0.1");
            target.AddResourceRecord("192.168.0.2");
            template.AddResource(target);
            StackTest.CreateTestStack(template, this.TestContext);
        }
    }
}
