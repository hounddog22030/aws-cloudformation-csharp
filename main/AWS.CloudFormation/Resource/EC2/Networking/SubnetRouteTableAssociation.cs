using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.Networking;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class SubnetRouteTableAssociation : ResourceBase
    {
        //$"SubnetRouteTableAssociation4{subnet.LogicalId}4{routeTable.LogicalId}"
        public SubnetRouteTableAssociation(Subnet subnet, RouteTable routeTable)
            : base(ResourceType.AwsEc2SubnetRouteTableAssociation)
        {
            RouteTable = routeTable;
            Subnet = subnet;
            this.LogicalId = $"SubnetRouteTableAssociation4{subnet.LogicalId}{routeTable.LogicalId}";
        }
        [JsonIgnore] public Subnet Subnet
        {
            get
            {
                return this.Properties.GetValue<Subnet>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore] public RouteTable RouteTable
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

        protected override bool SupportsTags => false;

    }
}
