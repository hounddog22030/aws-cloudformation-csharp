using AWS.CloudFormation.Serializer;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class ElasticIp : ResourceBase
    {
        public ElasticIp(Instancing.Instance instance, string name) : base(instance.Template, "AWS::EC2::EIP", name, false)
        {
            Instance = instance;
            this.Domain = "vpc";
        }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "InstanceId")]
        public Instancing.Instance Instance { get; }

        [CloudFormationProperties]
        public string Domain { get; private set; }
    }
}
