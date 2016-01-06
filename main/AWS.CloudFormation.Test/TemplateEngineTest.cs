using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using AWS.CloudFormation.Instance;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperatingSystem = AWS.CloudFormation.Instance.OperatingSystem;

namespace AWS.CloudFormation.Test
{
    /// <summary>
    /// Summary description for TemplateEngineTest
    /// </summary>
    [TestClass]
    public class TemplateEngineTest
    {
        public TemplateEngineTest()
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
            var template = GetTemplate();

            var t = new TemplateEngine();
            var result = t.CreateTemplateString(template);


            Assert.IsNotNull(result);

        }

        [TestMethod]
        public void CreateTemplateFileTest()
        {
            var template = GetTemplate();

            var t = new TemplateEngine();
            FileInfo file = t.CreateTemplateFile(template);
            Assert.IsNotNull(file);
            Assert.IsTrue(file.Exists);
        }

        [TestMethod]
        public void NoDefaultKeyName()
        {
            InvalidOperationException o = null;
            try
            {
                var template = new Template(Guid.NewGuid().ToString());
                template.Parameters.Clear();
                var i1 = new Instance.Instance(template, Guid.NewGuid().ToString(), InstanceTypes.T2Nano, "ami-b17f35db", OperatingSystem.Windows, false);
            }
            catch (InvalidOperationException e)
            {
                o = e;
            }
            Assert.IsNotNull(o);
        }

        private static Template GetTemplate()
        {
            string defaultKeyName = "InvalidKeyName";
            var template = new Template(defaultKeyName);
            var i1 = new Instance.Instance(template,Guid.NewGuid().ToString(), InstanceTypes.T2Nano, "ami-b17f35db", OperatingSystem.Windows, false);
            template.Resources.Add("instance1", i1);
            var vpc = new Vpc(template,"Vpc","0.0.0.0/0");
            template.Resources.Add("VPC", vpc);
            return template;
        }
    }
}
