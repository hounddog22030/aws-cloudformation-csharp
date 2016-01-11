using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class RouteTable : ResourceBase
    {
        internal RouteTable(Template template, string name, Vpc vpc)
            : base(template, "AWS::EC2::RouteTable", name, true)
        {
            this.Vpc = vpc;
        }

        [JsonProperty(PropertyName = "VpcId")]
        [CloudFormationProperties]
        public Vpc Vpc { get; private set; }
    }
}
