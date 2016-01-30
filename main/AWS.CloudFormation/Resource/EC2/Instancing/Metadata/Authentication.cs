using AWS.CloudFormation.Common;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata
{
    public class S3Authentication : CloudFormationDictionary
    {
        public S3Authentication(string accessKey, string secretKey, string[] buckets)
        {
            this.AccessKeyId = accessKey;
            this.SecretKey = secretKey;
            this.Buckets = buckets;
            this.Add("accessKeyId", this.AccessKeyId);
            this.Add("secretKey", this.SecretKey);
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
