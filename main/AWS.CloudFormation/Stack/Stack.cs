using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

namespace AWS.CloudFormation.Stack
{
    public class Stack
    {
        private Stack(string name)
        {
            this.Name = name;
        }

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

        public static List<Stack> GetActiveStacks()
        {
            AmazonCloudFormationClient client = new AmazonCloudFormationClient(RegionEndpoint.USEast1);
            ListStacksRequest listStacksRequest = new ListStacksRequest();

            listStacksRequest.StackStatusFilter.Add("CREATE_IN_PROGRESS");
            listStacksRequest.StackStatusFilter.Add("CREATE_FAILED");
            listStacksRequest.StackStatusFilter.Add("CREATE_COMPLETE");
            listStacksRequest.StackStatusFilter.Add("ROLLBACK_IN_PROGRESS");
            listStacksRequest.StackStatusFilter.Add("ROLLBACK_FAILED");
            listStacksRequest.StackStatusFilter.Add("ROLLBACK_COMPLETE");
            listStacksRequest.StackStatusFilter.Add("DELETE_IN_PROGRESS");
            listStacksRequest.StackStatusFilter.Add("DELETE_FAILED");
            listStacksRequest.StackStatusFilter.Add("UPDATE_IN_PROGRESS");
            listStacksRequest.StackStatusFilter.Add("UPDATE_COMPLETE_CLEANUP_IN_PROGRESS");
            listStacksRequest.StackStatusFilter.Add("UPDATE_COMPLETE");
            listStacksRequest.StackStatusFilter.Add("UPDATE_ROLLBACK_IN_PROGRESS");
            listStacksRequest.StackStatusFilter.Add("UPDATE_ROLLBACK_FAILED");
            listStacksRequest.StackStatusFilter.Add("UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS");
            listStacksRequest.StackStatusFilter.Add("UPDATE_ROLLBACK_COMPLETE");

            var response = client.ListStacks(listStacksRequest);

            List<Stack> returnValue = new List<Stack>();

            response.StackSummaries.ForEach(s=> returnValue.Add(new Stack(s.StackName)));

            return returnValue;
        }

        public string Name { get; }
    }

    public static class MyExtensions
    {
        public static bool IsOutFileLog(this FileInfo info)
        {
            return info.Name.EndsWith(".log");
        }
    }
}
