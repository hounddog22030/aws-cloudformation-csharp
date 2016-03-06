using System;
using System.Text;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.IAM;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AWS.CloudFormation.Test.Lambda
{
    [TestClass]
    public class FunctionTest
    {
        [TestMethod]
        public void FunctionTestTest()
        {
            Template template = new Template($"FunctionTestTest{DateTime.Now.Ticks}", "Create by FunctionTestTest for testing lambda");
            //StringBuilder policyBuilder = new StringBuilder();
            //policyBuilder.Append("{");
            //policyBuilder.Append("\"Version\": \"2012-10-17\",");
            //policyBuilder.Append("\"Statement\": [");
            //policyBuilder.Append("{");
            //policyBuilder.Append("\"Effect\": \"Allow\",");
            //policyBuilder.Append("\"Action\": [");
            //policyBuilder.Append("\"logs:CreateLogGroup\",");
            //policyBuilder.Append("\"logs:CreateLogStream\",");
            //policyBuilder.Append("\"logs:PutLogEvents\"");
            //policyBuilder.Append("],");
            //policyBuilder.Append("\"Resource\": \"arn:aws:logs:*:*:*\"");
            //policyBuilder.Append("}");
            //policyBuilder.Append("]");
            //policyBuilder.Append("}");
            CloudFormationDictionary policy = new CloudFormationDictionary();
            policy.Add("Version", "2012-10-17");
            CloudFormationDictionary effect = new CloudFormationDictionary();
            effect.Add("Effect", "Allow");

            string[] logs = new string[] {"logs:CreateLogGroup", "logs:CreateLogStream", "logs:PutLogEvents"};
            CloudFormationDictionary action = new CloudFormationDictionary();
            action.Add("Action", logs);

            CloudFormationDictionary resource = new CloudFormationDictionary();
            resource.Add("Resource", "arn:aws:logs:*:*:*");

            object[] statementObjects = new[] {effect,action, resource };
            policy.Add("Statement",statementObjects);

            string path = "/";


            CloudFormationDictionary policyDocument = new CloudFormationDictionary();
            policyDocument.Add("Version", "2012-10-17");

            CloudFormationDictionary innerStatement = new CloudFormationDictionary();
            innerStatement.Add("Effect", "Allow");
            innerStatement.Add("Action", "*");
            innerStatement.Add("Resource", "*");

            object[] innerStatementArray = new object[] {innerStatement};
            policyDocument.Add("Statement", innerStatementArray);


            CloudFormationDictionary policyName = new CloudFormationDictionary();
            policyName.Add("PolicyName", "root");




            object[]  policies = new object[] { policyName, policyDocument };


            Role executionRole = new Role(policy, policies,path);
            template.Resources.Add("roleforexecution",executionRole);
            Stack.Stack.CreateStack(template);

        }
    }
}
