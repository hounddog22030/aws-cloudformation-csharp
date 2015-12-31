using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serializer;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Instance
{
    public class NetworkInterface
    {
        public NetworkInterface(Subnet subnet)
        {
            Subnet = subnet;
            GroupSet = new CollectionThatSerializesAsIds<SecurityGroup>();

        }

        public bool AssociatePublicIpAddress { get; set; }
        public ushort DeviceIndex { get; set; }
        public bool DeleteOnTermination { get; set; }

        [JsonConverter(typeof(ResourceAsPropertyConverter))]
        [JsonProperty(PropertyName = "SubnetId")]
        public Subnet Subnet { get; set; }

        public CollectionThatSerializesAsIds<SecurityGroup> GroupSet { get; private set; }
    }

}
