using System;
using System.Linq;
using System.Runtime.Serialization;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.Networking;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AWS.CloudFormation.Resource.EC2.Networking
{

    public class Subnet : ResourceBase, ICidrBlock
    {
        public const string SUBNET_TYPE = "AWS::EC2::Subnet";


        public Subnet(Template template, string logicalId, Vpc vpc, string cidr, AvailabilityZone availabilityZone) : base(template, SUBNET_TYPE,logicalId,true)
        {
            Vpc = vpc;
            CidrBlock = cidr;
            AvailabilityZone = availabilityZone;
        }

        public Subnet(Template template, string logicalId, Vpc vpc, string cidr, AvailabilityZone availabilityZone, bool addInternetGatewayRoute ) : this(template,logicalId,vpc,cidr,availabilityZone)
        {
            if (addInternetGatewayRoute)
            {
                RouteTable routeTable = new RouteTable(template, $"{this.LogicalId}RouteTable", vpc);
                Route route = new Route(template, $"{this.LogicalId}Route", vpc.InternetGateway, "0.0.0.0/0", routeTable);
                SubnetRouteTableAssociation routeTableAssociation = new SubnetRouteTableAssociation(template, this, routeTable);
            }
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
                var enumType = typeof(AvailabilityZone);
                var name = Enum.GetName(enumType, value);
                var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).Single();
                this.Properties.SetValue(enumMemberAttribute.Value);
            }
        }

        public void AddNatGateway(Instance nat, SecurityGroup natSecurityGroup)
        {
            RouteTable routeTable = new RouteTable(this.Template, $"routeTableFor{this.LogicalId}", this.Vpc);
            Route route = new Route(this.Template, $"routeFor{this.LogicalId}", Template.CIDR_IP_THE_WORLD, routeTable);
            SubnetRouteTableAssociation routeTableAssociation = new SubnetRouteTableAssociation(this.Template, this, routeTable);
            route.Instance = nat;
            
            natSecurityGroup.AddIngress((ICidrBlock)this, Protocol.All, Ports.Min, Ports.Max);
            natSecurityGroup.AddIngress((ICidrBlock)this, Protocol.Icmp, Ports.All);
        }
    }
}
