using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.Networking;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class Route : ResourceBase
    {
        public Route(InternetGateway gateway, string destinationCidrBlock, RouteTable routeTable)
            : this(destinationCidrBlock, routeTable)
        {
            if (gateway != null)
            {
                Gateway = gateway;
                this.DependsOn.Add(gateway.LogicalId);
            }
        }
        public Route(VpcPeeringConnection vpcPeeringConnection, string destinationCidrBlock, RouteTable routeTable)
            : this(destinationCidrBlock, routeTable)
        {
            VpcPeeringConnectionId = new ReferenceProperty(vpcPeeringConnection);
            this.DependsOn.Add(vpcPeeringConnection.LogicalId);
        }

        [JsonIgnore]
        public object VpcPeeringConnectionId
        {
            get { return this.Properties.GetValue<object>(); }
            set { this.Properties.SetValue(value); }
        }

        public Route(string destinationCidrBlock, RouteTable routeTable) : base(ResourceType.AwsEc2Route)
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
        public object Gateway
        {
            get
            {
                return this.Properties.GetValue<object>();
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


        protected override bool SupportsTags {
            get { return false; }
        }
    }
}
