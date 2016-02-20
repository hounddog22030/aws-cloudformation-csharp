using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Stack;
using AWS.CloudFormation.Resource.DirectoryService;
using AWS.CloudFormation.Resource.EC2.Networking;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AWS.CloudFormation.Test.Resource.DirectoryService
{
    [TestClass]
    public class SimpleAdTest
    {
        [TestMethod]
        public void SimplestTest()
        {
            Template t = new Template("corp.getthebuybox.com", "SimplestTestVpc", StackTest.CidrVpc);
            t.StackName = $"SimplestTest{DateTime.Now.Ticks}";
            Vpc vpc = t.Vpcs.First();
            Subnet subnet1 = new Subnet(vpc, "10.0.0.0/24", AvailabilityZone.UsEast1A, true);
            t.Resources.Add(subnet1.LogicalId, subnet1);
            Subnet subnet2 = new Subnet(vpc, "10.0.1.0/24", AvailabilityZone.UsEast1E, true);
            t.Resources.Add(subnet2.LogicalId, subnet2);

            SimpleAd simpleAd = new SimpleAd("alpha.dev.yadayadasoftwarecom.awsdirectory.com", StackTest.GetPassword(),DirectorySize.Small, t.Vpcs.First(), subnet1, subnet2);
            simpleAd.ShortName = "alphadev";
            t.Resources.Add("simpleAd",simpleAd);
            Stack.Stack.CreateStack(t);
        }
    }
}
