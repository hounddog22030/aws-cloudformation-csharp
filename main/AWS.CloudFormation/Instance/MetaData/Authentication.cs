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
        public S3Authentication(string accessKey, string secretKey, string[] buckets)
        {
            this.AccessKeyId = accessKey;
            this.SecretKey = secretKey;
            this.Buckets = buckets;
            this.Add("accessKeyId", this.AccessKeyId);
            this.Add("secretKeyId", this.SecretKey);
            this.Add("buckets", this.Buckets);
        }

        [JsonIgnore]
        public string AccessKeyId { get;  }
        [JsonIgnore]
        public string SecretKey { get; }
        [JsonIgnore]
        public string[] Buckets { get; }

    }
}
