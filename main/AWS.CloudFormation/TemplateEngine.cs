using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation
{
    public class TemplateEngine
    {
        public string CreateTemplateString(Template template)
        {
            if (template == null)
            {
                throw new NullReferenceException();
            }
            return JsonConvert.SerializeObject(template, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        public FileInfo CreateTemplateFile(Template template)
        {
            FileInfo info = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString() + ".template"));
            using (var file = new System.IO.StreamWriter(info.FullName))
            {
                var serialized = CreateTemplateString(template);
                file.Write(serialized);
            }
            return info;
        }
    }
}
