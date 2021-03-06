﻿using System;
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

        private readonly bool _addInternetGatewayRoute;

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

        public override string LogicalId {
            get
            {
                if (string.IsNullOrEmpty(base.LogicalId))
                {
                    this.LogicalId = $"Subnet{this.CidrBlock}".Replace(".", string.Empty).Replace("/", string.Empty);
                }
                return base.LogicalId;
            }
            internal set { base.LogicalId = value; }
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
                if (this.NatSecurityGroup != null)
                {
                    this.NatSecurityGroup.AddIngress((ICidrBlock)this, Protocol.All, Ports.Min, Ports.Max);
                    this.NatSecurityGroup.AddIngress((ICidrBlock)this, Protocol.Icmp, Ports.All);
                }
            }


            if (_addInternetGatewayRoute)
            {
                this.RouteTable = new RouteTable(this.Vpc);
                this.RouteTable.LogicalId = $"RouteTable4{this.LogicalId}";
                template.Resources.Add(this.RouteTable.LogicalId, this.RouteTable);
                Route route2 = new Route(this.Vpc.InternetGateway, "0.0.0.0/0", this.RouteTable);
                template.Resources.Add($"Route4{this.LogicalId}", route2);
                SubnetRouteTableAssociation routeTableAssociation2 = new SubnetRouteTableAssociation(this, this.RouteTable);
                template.Resources.Add(routeTableAssociation2.LogicalId, routeTableAssociation2);
            }
        }

        [JsonIgnore]
        public RouteTable RouteTable { get; private set; }

        protected override bool SupportsTags => true;

    }
}
