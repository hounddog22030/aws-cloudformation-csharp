﻿using System;
using System.Text;
using System.Collections.Generic;
using AWS.CloudFormation.Resource.Route53;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AWS.CloudFormation.Test.Route53
{
    /// <summary>
    /// Summary description for HostedZoneTest
    /// </summary>
    [TestClass]
    public class HostedZoneTest
    {
        public HostedZoneTest()
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
        public void CreateHostedZoneByName()
        {
            Template template = StackTest.GetNewBlankTemplateWithVpc(this.TestContext);
            //Z313ZDEZ2F8F0F
            string hostedZoneName = $"{this.TestContext.TestName}{Guid.NewGuid().ToString().Replace("-", string.Empty)}.com";
            HostedZone hz = new HostedZone(template, hostedZoneName, hostedZoneName);
            template.AddResource(hz);
            StackTest.CreateTestStack(template, this.TestContext);
            //
            // TODO: Add test logic here
            //
        }
    }
}