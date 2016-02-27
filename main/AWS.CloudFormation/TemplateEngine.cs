using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation
{
    public class TemplateEngine
    {
        public static string CreateTemplateString(Template template)
        {
            if (template == null)
            {
                throw new NullReferenceException();
            }
            return JsonConvert.SerializeObject(template, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        public static FileInfo CreateTemplateFile(Template template)
        {
            FileInfo info = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), $"{template.StackName}.template"));
            using (var file = new System.IO.StreamWriter(info.FullName))
            {
                var serialized = CreateTemplateString(template);
                file.Write(serialized);
            }
            return info;
        }

        public static Uri UploadTemplate(Template template, string path)
        {
            template.AddOutputs();
            var file = CreateTemplateFile(template);

            string filePath = file.FullName;

            TransferUtility fileTransferUtility = new
                TransferUtility(new AmazonS3Client(Amazon.RegionEndpoint.USEast1));

            // 1. Upload a file, file name is used as the object key name.
            fileTransferUtility.Upload(filePath, path);
            Console.WriteLine("Upload 1 completed");
            return new Uri($"https://s3.amazonaws.com/{path}/{file.Name}");
        }
    }
}
