using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serializer;
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
                var vpcId = this.Properties.GetValue<CloudFormationDictionary>();
                return vpcId["Ref"] as RouteTable;
            }
            set
            {
                var refDictionary = new CloudFormationDictionary();
                refDictionary.Add("Ref", ((ILogicalId)value).LogicalId);
                this.Properties.SetValue(refDictionary);
            }
        }

        [JsonIgnore]
        public InternetGateway Gateway
        {
            get
            {
                var vpcId = this.Properties.GetValue<CloudFormationDictionary>();
                return vpcId["Ref"] as InternetGateway;
            }
            set
            {
                var refDictionary = new CloudFormationDictionary();
                refDictionary.Add("Ref", ((ILogicalId)value).LogicalId);
                this.Properties.SetValue(refDictionary);
            }
        }

        public Instance Instance
        {
            get { return this.Properties.GetValue<Instance>(); }
            set { this.Properties.SetValue(value); }
        }



    }
}
