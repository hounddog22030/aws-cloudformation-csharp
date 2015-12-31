using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource
{
    [JsonConverter(typeof(ResourceJsonConverter))]
    public class Vpc : ResourceBase, ICidrBlock
    {
        public Vpc(Template template, string name, string cidrBlock) : base(template, "AWS::EC2::VPC", name, true)
        {
            CidrBlock = cidrBlock;
        }

        [CloudFormationPropertiesAttribute]
        public string CidrBlock { get; set; }
    }
}
