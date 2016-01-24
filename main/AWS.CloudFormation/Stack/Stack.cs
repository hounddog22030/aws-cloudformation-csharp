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
        public static CreateStackResponse CreateStack(Template template,string name)
        {
            var templateUri = TemplateEngine.UploadTemplate(template, "gtbb/templates");

            AmazonCloudFormationClient client = new AmazonCloudFormationClient(RegionEndpoint.USEast1);

            CreateStackRequest request = new CreateStackRequest
            {
                DisableRollback = true,
                StackName = name,
                TemplateURL = templateUri.AbsoluteUri
            };

            try
            {
                var response = client.CreateStack(request);

                if (response.HttpStatusCode < HttpStatusCode.OK || response.HttpStatusCode >= HttpStatusCode.MultipleChoices)
                {
                    throw new Exception(response.ToString());
                }
                return response;
            }
            catch (Amazon.Runtime.Internal.HttpErrorResponseException ex)
            {
                throw new Exception(ex.Message);
            }

        }
        public static CreateStackResponse CreateStack(Template template)
        {
            return CreateStack(template,$"Stack{Guid.NewGuid().ToString().Replace("{",string.Empty).Replace("}",string.Empty)}");
        }

        public static void UpdateStack(string stackName, Template template)
        {
            var templateUri = TemplateEngine.UploadTemplate(template, "gtbb/templates");
            AmazonCloudFormationClient client = new AmazonCloudFormationClient(RegionEndpoint.USEast1);

            UpdateStackRequest request = new UpdateStackRequest
            {
                StackName = stackName,
                TemplateURL = templateUri.AbsoluteUri
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
