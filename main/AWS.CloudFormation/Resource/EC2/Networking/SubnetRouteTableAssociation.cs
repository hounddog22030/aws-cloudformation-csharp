using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class SubnetRouteTableAssociation : ResourceBase
    {
        public SubnetRouteTableAssociation(Template template, string name, Subnet subnet, RouteTable routeTable)
            : base(template, "AWS::EC2::SubnetRouteTableAssociation", name, false)
        {
            RouteTable = routeTable;
            Subnet = subnet;
        }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "SubnetId")]
        public Subnet Subnet { get; private set; }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "RouteTableId")]
        public RouteTable RouteTable { get; private set; }
        //RouteTableId

    }
}
