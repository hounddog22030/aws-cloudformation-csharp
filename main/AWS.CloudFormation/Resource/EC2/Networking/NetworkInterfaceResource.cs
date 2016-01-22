using AWS.CloudFormation.Common;
using AWS.CloudFormation.Serialization;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class NetworkInterfaceResource : ResourceBase
    {
        public NetworkInterfaceResource(Template template, string logicalId, Subnet subnet) : base(template, "AWS::EC2::NetworkInterface", logicalId, true)
        {
            Subnet = subnet;
            //GroupSet = new IdCollection<SecurityGroup>();

        }


        [JsonProperty(PropertyName = "SubnetId")]
        [JsonIgnore]
        public Subnet Subnet
        {
            get
            {
                return this.Properties.GetValue<Subnet>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        //public IdCollection<SecurityGroup> GroupSet { get; private set; }
    }

}
