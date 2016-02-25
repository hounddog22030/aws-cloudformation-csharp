using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.DirectoryService;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class VpcPeeringConnection : ResourceBase
    {
        public VpcPeeringConnection(Vpc vpc, object peerVpcId) : base(ResourceType.AwsEc2VpcPeeringConnection)
        {
            this.LogicalId = $"{vpc.LogicalId}To{PeerVpcId}".Replace("-", string.Empty);
            this.VpcId = new ReferenceProperty(vpc.LogicalId);
            this.PeerVpcId = peerVpcId;
        }

        protected override bool SupportsTags => true;

        [JsonIgnore]
        public object VpcId
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            private set { this.Properties.SetValue(value); }
        }
        [JsonIgnore]
        public object PeerVpcId
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            private set { this.Properties.SetValue(value); }
        }
    }
}
