using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.CloudFormation.Resource.Networking
{
    public class SecurityGroupEgress : SecurityGroupIngressEgressBase
    {
        internal SecurityGroupEgress(int fromPort, int toPort, string protocol, string cidr) : base(fromPort, toPort, protocol, cidr)
        {
        }
        public string DestinationSecurityGroupId { get; set; }
    }
}
