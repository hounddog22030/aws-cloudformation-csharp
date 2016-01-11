using AWS.CloudFormation.Serializer;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2
{
    public class ElasticIp : ResourceBase
    {
        public ElasticIp(EC2.Instance instance, string name) : base(instance.Template, "AWS::EC2::EIP", name, false)
        {
            Instance = instance;
            this.Domain = "vpc";
        }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "InstanceId")]
        public EC2.Instance Instance { get; }

        [CloudFormationProperties]
        public string Domain { get; private set; }
    }
}
