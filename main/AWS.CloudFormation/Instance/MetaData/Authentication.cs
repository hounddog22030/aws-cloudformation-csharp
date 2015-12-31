using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Instance.Metadata
{
    //public abstract class Authentication : ResourceBase
    //{
    //    public Authentication(Template template, string name) : base("AWS::CloudFormation::Authentication",name,false)
    //    {
    //        AuthenticationType = name;
    //    }

    //    [JsonProperty(PropertyName = "type")]
    //    public string AuthenticationType { get; }
    //}

    public class S3Authentication : CloudFormationDictionary
    {
        public S3Authentication(string accessKey, string secretKey)
        {
            this.AccessKeyId = accessKey;
            this.SecretKey = secretKey;
            this.Add("accessKeyId", this.AccessKeyId);
            this.Add("secretKeyId", this.SecretKey);
        }

        [JsonIgnore]
        public string AccessKeyId { get;  }
        [JsonIgnore]
        public string SecretKey { get; }
    }
}
