using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class VpcEndpoint : ResourceBase
    {

        public VpcEndpoint(string serviceName, Vpc vpc, RouteTable routeTableForSubnetsToNat1) : base(ResourceType.AwsEc2VpcEndpoint)
        {

            
            this.ServiceName = new FnJoin(FnJoinDelimiter.None,
                "com.amazonaws.",
                new ReferenceProperty("AWS::Region"),
                ".",
                serviceName);
            this.Vpc = vpc;
            this.RouteTableIds.Add(new ReferenceProperty(routeTableForSubnetsToNat1.LogicalId));
        }

        protected override bool SupportsTags => false;



        [JsonIgnore]
        public object PolicyDocument
        {
            get { return this.Properties.GetValue<object>(); }
            set { this.Properties.SetValue(value); }
        }
        [JsonIgnore]
        public List<object> RouteTableIds
        {
            get
            {
                if (this.Properties.GetValue<List<object>>() == null)
                {
                    this.RouteTableIds = new List<object>();
                }
                return this.Properties.GetValue<List<object>>();
            }
            set { this.Properties.SetValue(value); }
        }
        [JsonIgnore]
        public object ServiceName
        {
            get { return this.Properties.GetValue<object>(); }
            set { this.Properties.SetValue(value); }
        }
        [JsonIgnore]
        public Vpc Vpc
        {
            get { return this.Properties.GetValue<Vpc>("VpcId"); }
            set { this.Properties.SetValue("VpcId",value); }
        }
    }
}
