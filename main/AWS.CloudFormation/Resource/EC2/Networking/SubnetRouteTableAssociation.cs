using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serializer;
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
                var resourceId = this.Properties.GetValue<CloudFormationDictionary>();
                return resourceId["Ref"] as Subnet;
            }
            set
            {
                var refDictionary = new CloudFormationDictionary();
                refDictionary.Add("Ref", ((ILogicalId)value).LogicalId);
                this.Properties.SetValue(refDictionary);
            }
        }

        [JsonIgnore] public RouteTable RouteTable
        {
            get
            {
                var resourceId = this.Properties.GetValue<CloudFormationDictionary>();
                return resourceId["Ref"] as RouteTable;
            }
            set
            {
                var refDictionary = new CloudFormationDictionary();
                refDictionary.Add("Ref", ((ILogicalId)value).LogicalId);
                this.Properties.SetValue(refDictionary);
            }
        }

    }
}
