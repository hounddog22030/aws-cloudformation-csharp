//using System;
//using System.Text;
//using System.Collections.Generic;
//using System.IO;
//using AWS.CloudFormation.Resource;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

//namespace AWS.CloudFormation.Test
//{
//    /// <summary>
//    /// Summary description for TemplateEngineTest
//    /// </summary>
//    [TestClass]
//    public class TemplateEngineTest
//    {
//        public TemplateEngineTest()
//        {
//            //
//            // TODO: Add constructor logic here
//            //
//        }

//        private TestContext testContextInstance;

//        /// <summary>
//        ///Gets or sets the test context which provides
//        ///information about and functionality for the current test run.
//        ///</summary>
//        public TestContext TestContext
//        {
//            get
//            {
//                return testContextInstance;
//            }
//            set
//            {
//                testContextInstance = value;
//            }
//        }

//        #region Additional test attributes
//        //
//        // You can use the following additional attributes as you write your tests:
//        //
//        // Use ClassInitialize to run code before running the first test in the class
//        // [ClassInitialize()]
//        // public static void MyClassInitialize(TestContext testContext) { }
//        //
//        // Use ClassCleanup to run code after all tests in a class have run
//        // [ClassCleanup()]
//        // public static void MyClassCleanup() { }
//        //
//        // Use TestInitialize to run code before running each test 
//        // [TestInitialize()]
//        // public void MyTestInitialize() { }
//        //
//        // Use TestCleanup to run code after each test has run
//        // [TestCleanup()]
//        // public void MyTestCleanup() { }
//        //
//        #endregion

//        [TestMethod]
//        public void TestMethod1()
//        {
//            var template = GetTemplate();

//            var t = new TemplateEngine();
//            var result = t.CreateTemplateString(template);


//            Assert.IsNotNull(result);

//        }

//        [TestMethod]
//        public void CreateTemplateFileTest()
//        {
//            var template = GetTemplate();

//            var t = new TemplateEngine();
//            FileInfo file = t.CreateTemplateFile(template);
//            Assert.IsNotNull(file);
//            Assert.IsTrue(file.Exists);
//        }

//        private static Template GetTemplate()
//        {
//            var template = new Template()
//            {
//                AwsTemplateFormatVersion = "2010-09-09"
//            };

//            var i1 = new Instance();
//            i1.Properties.Add("ImageId", "ami-b17f35db");
//            template.Resources.Add("instance1", i1);
//            var vpc = new VPC();
//            vpc.Properties.Add("CidrBlock","VPCCIDR");
//            template.Resources.Add("VPC",vpc);
//            return template;
//        }
//    }
//}
