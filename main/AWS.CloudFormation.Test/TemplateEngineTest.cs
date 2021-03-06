﻿using System;
using System.IO;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;

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

            var result = TemplateEngine.CreateTemplateString(template);


            Assert.IsNotNull(result);

        }

        [TestMethod]
        public void CreateTemplateFileTest()
        {
            var template = StackTest.GetTemplateFullStack("yadayadasoftware.com", "dev", StackTest.Greek.Xi, StackTest.Create.FullStack, "No Suffix");

            FileInfo file = TemplateEngine.CreateTemplateFile(template);
            Assert.IsNotNull(file);
            Assert.IsTrue(file.Exists);
        }

        [TestMethod]
        public void UpdateTemplateTest()
        {
            var template = GetTemplate();
            Uri uri = TemplateEngine.UploadTemplate(template, "gtbb/software/cf");
            Assert.IsNotNull(uri);
            Assert.AreEqual("https", uri.Scheme);
            Assert.AreEqual("s3.amazonaws.com", uri.DnsSafeHost);
            StringAssert.StartsWith(uri.AbsolutePath, "/gtbb/software/cf/");
            StringAssert.StartsWith(uri.AbsoluteUri, "https://s3.amazonaws.com");
        }

        [TestMethod]
        public void NoDefaultKeyName()
        {
            InvalidOperationException o = null;
            try
            {
                var template = new Template(Guid.NewGuid().ToString(),null,null,null);
                template.Parameters.Clear();
                var i1 = new Instance(null,  InstanceTypes.T2Nano, "ami-b17f35db", OperatingSystem.Windows);
                template.Resources.Add(Guid.NewGuid().ToString(),i1);
            }
            catch (InvalidOperationException e)
            {
                o = e;
            }
            Assert.IsNotNull(o);
        }

        [TestMethod]
        public void UploadTemplateThrowsExceptionOnNullName()
        {
            Template t = new Template("bob.pem","none","10.0.0.0/16","name","description");
            ArgumentNullException caught = null;
            try
            {
                TemplateEngine.UploadTemplate(t, "gtbb/templates");
            }
            catch (ArgumentNullException ex)
            {
                caught = ex;
            }
            Assert.IsNotNull(caught);
        }

        private static Template GetTemplate()
        {
            string defaultKeyName = "InvalidKeyName";
            var template = new Template(defaultKeyName,null,null,null);
            var i1 = new Instance(null,InstanceTypes.T2Nano, "ami-b17f35db", OperatingSystem.Windows);
            template.Resources.Add("instance1", i1);
            var vpc = new Vpc("0.0.0.0/0");
            template.Resources.Add("Vpc", vpc);

            template.Resources.Add("VPC", vpc);
            return template;
        }
    }
}
