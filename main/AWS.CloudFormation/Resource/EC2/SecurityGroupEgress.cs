using AWS.CloudFormation.Resource.Networking;

namespace AWS.CloudFormation.Resource.EC2
{
    public class SecurityGroupEgress : SecurityGroupIngressEgressBase
    {
        internal SecurityGroupEgress(int fromPort, int toPort, string protocol, string cidr) : base(fromPort, toPort, protocol, cidr)
        {
        }
        public string DestinationSecurityGroupId { get; set; }
    }
}
