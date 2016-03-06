using AWS.CloudFormation.Serialization;

using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class SecurityGroupIngress : SecurityGroupIngressEgressBase
    {
        public SecurityGroupIngress()
        {
        }

        
        internal SecurityGroupIngress(int fromPort, int toPort, string protocol, SecurityGroup securityGroup) : base(fromPort, toPort, protocol)
        {
            SecurityGroup = securityGroup;
        }

        internal SecurityGroupIngress(int fromPort, int toPort, string protocol, object cidr) : base(fromPort, toPort, protocol, cidr)
        {
        }

        public SecurityGroupIngress(ILogicalId logicalId, Protocol protocol, Ports port) : base(logicalId,protocol,port,port)
        {
        }


        //public string SourceSecurityGroupId { get; set; }
        public string SourceSecurityGroupName { get; set; }
        public string SourceSecurityGroupOwnerId { get; set; }

        [JsonConverter(typeof(ResourceAsPropertyConverter))]
        [JsonProperty(PropertyName = "SourceSecurityGroupId")]
        public SecurityGroup SecurityGroup { get; }
    }
}
