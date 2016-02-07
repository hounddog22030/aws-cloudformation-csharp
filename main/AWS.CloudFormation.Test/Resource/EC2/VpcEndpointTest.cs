using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

        private Template GetEndpointTemplate(string nameBase)
        {
            var template = new Template(StackTest.KeyPairName,$"Vpc{nameBase}",StackTest.CidrVpc);
            VpcEndpoint endpoint = new VpcEndpoint("s3",template.Vpcs.First());
            template.Resources.Add($"VpcEndpoint4{nameBase}",endpoint);
            return template;


        }
    }
}
