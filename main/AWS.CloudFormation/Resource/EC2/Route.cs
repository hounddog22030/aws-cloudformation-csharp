using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2
{
    public class Route : ResourceBase
    {
        internal Route(Template template, string routeName, InternetGateway gateway, string destinationCidrBlock, RouteTable routeTable)
            : this(template, routeName, destinationCidrBlock, routeTable)
        {
            InternetGateway = gateway;
        }

        public Route(Template template, string routeName, string destinationCidrBlock, RouteTable routeTable) : base(template, "AWS::EC2::Route", routeName, false)
        {
            DestinationCidrBlock = destinationCidrBlock;
            RouteTable = routeTable;
            RouteTable = routeTable;
        }

        [CloudFormationProperties]
        public string DestinationCidrBlock {get; set; }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "RouteTableId")]
        public RouteTable RouteTable {get;set;}

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "GatewayId")]
        public InternetGateway InternetGateway { get; set; }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "InstanceId")]
        public EC2.Instance Instance { get; set; }

        
    }
}
