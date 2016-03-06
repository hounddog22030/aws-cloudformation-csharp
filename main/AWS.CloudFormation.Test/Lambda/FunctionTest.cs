using System;
using System.Collections.Generic;
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
            Role executionRole = new Role()
            {
                Path = "/"
            };
            
            var statement = new Statement();
            statement.Effect = "Allow";
            var actions = new List<string>();
            actions.Add("sts:AssumeRole");
            statement.Action = actions;
            statement.Principal = new Principal();
            statement.Principal.Service.Add("lambda.amazonaws.com");

            var trust = new PolicyDocument();
            trust.Statement.Add(statement);


            executionRole.AssumeRolePolicyDocument = trust;

            var rootPolicy = new Policy();
            rootPolicy.PolicyName = "root";
            var rootPolicyDocument = new PolicyDocument();
            rootPolicy.PolicyDocument = rootPolicyDocument;
            var allowStatement = new Statement();
            allowStatement.Effect = "Allow";
            allowStatement.Action = "*";
            allowStatement.Resource = "*";
            rootPolicyDocument.Statement.Add(allowStatement);
            executionRole.Policies.Add(rootPolicy);

            template.Resources.Add("roleforexecution",executionRole);
            Stack.Stack.CreateStack(template);

        }
    }
}
