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
    public class EipAssociation : ResourceBase
    {
        public EipAssociation(Template template, string name, NetworkInterfaceResource networkInterface, ElasticIp elasticIp, Instance instance) : base(template, "AWS::EC2::EIPAssociation", name, false)
        {
            this.NetworkInterfaceId = new ReferenceProperty() {Ref = networkInterface.LogicalId};
            this.EIP = new ReferenceProperty() {Ref = elasticIp.LogicalId};
            this.InstanceId = new ReferenceProperty() {Ref = instance.LogicalId};
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
        [JsonIgnore]
        public ReferenceProperty EIP
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
