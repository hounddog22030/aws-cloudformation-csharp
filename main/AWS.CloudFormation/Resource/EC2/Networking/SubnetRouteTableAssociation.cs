using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.Networking;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class SubnetRouteTableAssociation : ResourceBase
    {
        public SubnetRouteTableAssociation(Template template, Subnet subnet, RouteTable routeTable)
            : base(template, "AWS::EC2::SubnetRouteTableAssociation", $"SubnetRouteTableAssociation{subnet.LogicalId}" , false)
        {
            RouteTable = routeTable;
            Subnet = subnet;
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

    }
}
