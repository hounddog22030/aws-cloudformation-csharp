using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Serialization;

using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class NetworkInterface
    {
        public NetworkInterface(Subnet subnet)
        {
            Subnet = subnet;
            GroupSet = new IdCollection<SecurityGroup>();

        }

        public NetworkInterface(NetworkInterfaceResource networkInterface)
        {
            this.DeviceIndex = 0;
            this.NetworkInterfaceId = new ReferenceProperty() { Ref = networkInterface.LogicalId};
        }

        public ReferenceProperty NetworkInterfaceId { get; private set; }

        //public bool AssociatePublicIpAddress { get; set; }
        public ushort DeviceIndex { get; set; }
        public bool DeleteOnTermination { get; set; }

        [JsonConverter(typeof(ResourceAsPropertyConverter))]
        [JsonProperty(PropertyName = "SubnetId")]
        public Subnet Subnet { get; set; }

        public IdCollection<SecurityGroup> GroupSet { get; private set; }
    }

}
