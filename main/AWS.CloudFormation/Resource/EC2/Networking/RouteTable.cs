using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class RouteTable : ResourceBase
    {
        public RouteTable(Vpc vpc) : base(ResourceType.AwsEc2RouteTable)
        {
            this.Vpc = vpc;
            if (vpc.InternetGateway != null)
            {
                this.DependsOn.Add(vpc.InternetGateway.LogicalId);
            }
            this.LogicalId = $"RouteTableFor{vpc.LogicalId}";
        }
        public RouteTable(FnGetAtt vpcReference) : base(ResourceType.AwsEc2RouteTable)
        {
            this.Vpc = vpcReference;
        }

        [JsonIgnore]
        [JsonProperty(PropertyName = "VpcId")]
        public object Vpc
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            private set
            {
                this.Properties.SetValue(value);
            }
        }

        protected override bool SupportsTags => true;
    }
}
