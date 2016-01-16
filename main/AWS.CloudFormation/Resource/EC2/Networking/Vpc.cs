﻿using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    [JsonConverter(typeof(ResourceJsonConverter))]
    public class Vpc : ResourceBase, ICidrBlock
    {
        public Vpc(Template template, string name, string cidrBlock) : base(template, "AWS::EC2::VPC", name, true)
        {
            CidrBlock = cidrBlock;
        }

        [CloudFormationProperties]
        public string CidrBlock { get; set; }

        public class VpcGatewayAttachment : ResourceBase
        {

            public VpcGatewayAttachment(Template template, string name)
                : base(template, "AWS::EC2::VPCGatewayAttachment", name, false)
            {

            }


            [JsonProperty(PropertyName = "InternetGatewayId")]
            [CloudFormationProperties]
            public InternetGateway InternetGateway { get; set; }

            [JsonProperty(PropertyName = "VpcId")]
            [CloudFormationProperties]
            public Vpc Vpc { get; set; }
        }
    }
}