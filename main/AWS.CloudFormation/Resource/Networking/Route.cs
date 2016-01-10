using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.Networking
{
        //"DMZRoute"                  : {
        //    "Type" : "AWS::EC2::Route",
        //    "Properties" : {
        //        "RouteTableId" : {
        //            "Ref" : "DMZRouteTable"
        //        },
        //        "DestinationCidrBlock" : "0.0.0.0/0",
        //        "GatewayId"            : {
        //            "Ref" : "VpcInternetGateway"
        //        }
        //    }
        //},
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
