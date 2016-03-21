using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Networking;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2
{
    //AWS::EC2::NatGateway
    public class NatGateway : ResourceBase
    {
        public NatGateway(ElasticIp elasticIp, Subnet subnet) : base(ResourceType.AwsEc2NatGateway)
        {
            AllocationId = new ReferenceProperty(elasticIp);
            SubnetId = new ReferenceProperty(subnet);
            this.DependsOn.Add(elasticIp.LogicalId);
            this.DependsOn.Add(subnet.LogicalId);
        }



        public override string LogicalId
        {
            get
            {
                if (base.LogicalId == null)
                {
                    var logicalId = "NatGateway";
                    if (this.SubnetId != null)
                    {
                        logicalId += $"4{this.SubnetId}";
                    }
                    else
                    {
                        logicalId += DateTime.Now.Ticks;
                    }
                    this.LogicalId = logicalId;
                }
                return base.LogicalId;
            }
            internal set { base.LogicalId = value; }
        }


        protected override bool SupportsTags => false;

        [JsonIgnore]
        public object AllocationId
        {
            get { return this.Properties.GetValue<object>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public object SubnetId
        {
            get { return this.Properties.GetValue<object>(); }
            set { this.Properties.SetValue(value); }
        }
    }
}
