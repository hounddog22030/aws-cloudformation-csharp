using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.Networking;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class Route : ResourceBase
    {
        public Route(Template template, string routeName, InternetGateway gateway, string destinationCidrBlock, RouteTable routeTable)
            : this(template, routeName, destinationCidrBlock, routeTable)
        {
            Gateway = gateway;
        }

        public Route(Template template, string routeName, string destinationCidrBlock, RouteTable routeTable) : base(template, "AWS::EC2::Route", routeName, false)
        {
            DestinationCidrBlock = destinationCidrBlock;
            RouteTable = routeTable;
        }

        [JsonIgnore]
        public string DestinationCidrBlock
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public RouteTable RouteTable
        {
            get
            {
                return this.Properties.GetValue<RouteTable>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public InternetGateway Gateway
        {
            get
            {
                return this.Properties.GetValue<InternetGateway>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public Instance Instance
        {
            get { return this.Properties.GetValue<Instance>(); }
            set { this.Properties.SetValue(value); }
        }



    }
}
