using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2
{
    public class VpcDhcpOptionsAssociation : ResourceBase
    {
        public VpcDhcpOptionsAssociation(Template template, string name, DhcpOptions dhcpOptions, Vpc vpc) : base(template, name, ResourceType.VpcDhcpOptionsAssociation)
        {
            this.DhcpOptionsId = new ReferenceProperty(dhcpOptions);
            this.VpcId = new ReferenceProperty(vpc);
        }

        protected override bool SupportsTags => false;

        [JsonIgnore]
        public ReferenceProperty DhcpOptionsId
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
        public ReferenceProperty VpcId
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
