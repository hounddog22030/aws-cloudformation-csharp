using System;
using System.IO;
using System.Net;
using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

namespace AWS.CloudFormation.Stack
{
    public class Stack
    {
        public void CreateStack(Template template)
        {
            Amazon.CloudFormation.AmazonCloudFormationClient client = new AmazonCloudFormationClient(RegionEndpoint.USEast1);

            CreateStackRequest request = new CreateStackRequest
            {
                DisableRollback = true,
                StackName = "Stack" + Guid.NewGuid(),
                TemplateBody = new TemplateEngine().CreateTemplateString(template)
            };

            try
            {
                var response = client.CreateStack(request);

                if (response.HttpStatusCode < HttpStatusCode.OK || response.HttpStatusCode >= HttpStatusCode.MultipleChoices)
                {
                    throw new Exception(response.ToString());
                }

            }
            catch (Amazon.Runtime.Internal.HttpErrorResponseException ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void UpdateStack(string stackName, Template template)
        {
            Amazon.CloudFormation.AmazonCloudFormationClient client = new AmazonCloudFormationClient(RegionEndpoint.USEast1);

            UpdateStackRequest request = new UpdateStackRequest
            {
                StackName = stackName,
                TemplateBody = new TemplateEngine().CreateTemplateString(template)
            };
            
            try
            {
                var response = client.UpdateStack(request);

                if (response.HttpStatusCode < HttpStatusCode.OK || response.HttpStatusCode >= HttpStatusCode.MultipleChoices)
                {
                    throw new Exception(response.ToString());
                }

            }
            catch (Amazon.Runtime.Internal.HttpErrorResponseException ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }

    public static class MyExtensions
    {
        public static bool IsOutFileLog(this FileInfo info)
        {
            return info.Name.EndsWith(".log");
        }
    }
}
