using AWS.CloudFormation.Common;

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

        protected override bool SupportsTags => true;
    }
}
