using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2
{

    [JsonConverter(typeof(ResourceJsonConverter))]
    [JsonObject(IsReference = true)]
    public class Subnet : ResourceBase, ICidrBlock
    {
        public const string AVAILIBILITY_ZONE_US_EAST_1A = "us-east-1a";
        public const string SUBNET_TYPE = "AWS::EC2::Subnet";

        internal Subnet(Template template, string name)
            : base(template, SUBNET_TYPE, name, true)
        {
        }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "VpcId")]
        public Vpc Vpc { get; set; }

        [CloudFormationProperties]
        public string CidrBlock { get; set; }

        [CloudFormationProperties]
        public string AvailabilityZone { get; set; }
    }
}
