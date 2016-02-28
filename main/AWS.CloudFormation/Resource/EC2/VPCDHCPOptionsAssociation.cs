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
        public VpcDhcpOptionsAssociation(DhcpOptions dhcpOptions, Vpc vpc) : this(dhcpOptions, (object)vpc)
        {
        }

        public VpcDhcpOptionsAssociation(DhcpOptions dhcpOptions, FnGetAtt vpc) : this(dhcpOptions,(object)vpc)
        {
        }

        public VpcDhcpOptionsAssociation(DhcpOptions dhcpOptions, object vpc) : this(new ReferenceProperty(dhcpOptions), vpc )
        {
        }

        public VpcDhcpOptionsAssociation(FnGetAtt dhcpOptions, FnGetAtt vpc) : this(dhcpOptions, (object)vpc)
        {
        }

        private VpcDhcpOptionsAssociation(object dhcpOptions, object vpc) : base(ResourceType.VpcDhcpOptionsAssociation)
        {
            this.DhcpOptionsId = dhcpOptions;
            this.VpcId = vpc;

        }

        protected override bool SupportsTags => false;

        [JsonIgnore]
        public object DhcpOptionsId
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }


        [JsonIgnore]
        public object VpcId
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
