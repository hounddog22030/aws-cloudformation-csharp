﻿using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.Networking;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class SubnetRouteTableAssociation : ResourceBase
    {
        public SubnetRouteTableAssociation(Template template, Subnet subnet, RouteTable routeTable)
            : base(template, $"SubnetRouteTableAssociation4{subnet.LogicalId}4{routeTable.LogicalId}", ResourceType.AwsEc2SubnetRouteTableAssociation)
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

        protected override bool SupportsTags => false;

    }
}
