using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class NetworkInterfaceAttachment : ResourceBase
    {
        public NetworkInterfaceAttachment(Template template, string name, Instance instance, NetworkInterfaceResource networkInterface) : base(template, "AWS::EC2::NetworkInterfaceAttachment", name, false)
        {
            this.InstanceId = new ReferenceProperty() {Ref = instance.LogicalId};

            this.NetworkInterfaceId = new ReferenceProperty() {Ref = networkInterface.LogicalId};

            instance.AddNetworkInterface(networkInterface);



        }

        [JsonIgnore]
        public string DeviceIndex
        {
            get
            {
                return this.Properties.GetValue<string>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public ReferenceProperty InstanceId
        {
            get
            {
                return this.Properties.GetValue<ReferenceProperty>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public ReferenceProperty NetworkInterfaceId
        {
            get
            {
                return this.Properties.GetValue<ReferenceProperty>();
            }
            set
            {
                this.Properties.SetValue(value);
            }

        }
    }
}
