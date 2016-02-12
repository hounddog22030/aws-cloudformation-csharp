using System;
using System.Linq;
using System.Runtime.Serialization;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.Networking;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AWS.CloudFormation.Resource.EC2.Networking
{

    public class Subnet : ResourceBase, ICidrBlock
    {


        public Subnet(Vpc vpc, string cidr, AvailabilityZone availabilityZone, RouteTable routeTableForGateway, SecurityGroup natSecurityGroup) : this(vpc, cidr, availabilityZone, false)
        {
            this.RouteTableForGateway = routeTableForGateway;
            this.NatSecurityGroup = natSecurityGroup;
        }

        private bool _addInternetGatewayRoute;

        public Subnet(Vpc vpc, string cidr, AvailabilityZone availabilityZone, bool addInternetGatewayRoute) : base(ResourceType.AwsEc2Subnet)
        {
            _addInternetGatewayRoute = addInternetGatewayRoute;
            Vpc = vpc;
            CidrBlock = cidr;
            AvailabilityZone = availabilityZone;
        }


        [JsonIgnore]
        public Vpc Vpc
        {
            get
            {
                return this.Properties.GetValue<Vpc>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public string CidrBlock
        {
            get { return (string)this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public AvailabilityZone AvailabilityZone
        {
            get { return this.Properties.GetValue<AvailabilityZone>(); }
            set
            {
                this.Properties.SetValue(value);
            }
        }


        [JsonIgnore]
        public SecurityGroup NatSecurityGroup { get; private set; }


        [JsonIgnore]
        public RouteTable RouteTableForGateway { get; set; }

        protected override void OnTemplateSet(Template template)
        {
            base.OnTemplateSet(template);

            if (this.RouteTableForGateway != null)
            {
                SubnetRouteTableAssociation routeTableAssociation = new SubnetRouteTableAssociation(this, this.RouteTableForGateway);
                this.Template.Resources.Add(routeTableAssociation.LogicalId, routeTableAssociation);
                this.NatSecurityGroup.AddIngress((ICidrBlock)this, Protocol.All, Ports.Min, Ports.Max);
                this.NatSecurityGroup.AddIngress((ICidrBlock)this, Protocol.Icmp, Ports.All);
            }


            if (_addInternetGatewayRoute)
            {
                RouteTable routeTable4 = new RouteTable(this.Vpc);
                template.Resources.Add($"RouteTable4{this.LogicalId}", routeTable4);
                Route route2 = new Route(this.Vpc.InternetGateway, "0.0.0.0/0", routeTable4);
                template.Resources.Add($"Route4{this.LogicalId}", route2);
                SubnetRouteTableAssociation routeTableAssociation2 = new SubnetRouteTableAssociation(this, routeTable4);
                template.Resources.Add(routeTableAssociation2.LogicalId, routeTableAssociation2);
            }
        }

        protected override bool SupportsTags => true;

    }
}
