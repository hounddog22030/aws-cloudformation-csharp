using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AWS.CloudFormation.Resource
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

        [CloudFormationPropertiesAttribute]
        [JsonProperty(PropertyName = "VpcId")]
        public Vpc Vpc { get; set; }

        [CloudFormationPropertiesAttribute]
        public string CidrBlock { get; set; }

        [CloudFormationPropertiesAttribute]
        public string AvailabilityZone { get; set; }
    }
}
