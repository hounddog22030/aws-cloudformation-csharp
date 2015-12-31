using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.Networking
{
    public class VPCGatewayAttachment : ResourceBase
    {

        public VPCGatewayAttachment(Template template, string name)
            : base(template,"AWS::EC2::VPCGatewayAttachment",name, false)
        {
            
        }


        [JsonProperty(PropertyName = "InternetGatewayId")]
        [CloudFormationProperties]
        public InternetGateway InternetGateway { get; set; }

        [JsonProperty(PropertyName = "VpcId")]
        [CloudFormationPropertiesAttribute]
        public Vpc Vpc { get; set; }
    }

}
