﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Instance;
using AWS.CloudFormation.Serializer;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.Networking
{
    public class SecurityGroupIngress : SecurityGroupIngressEgressBase
    {
        public SecurityGroupIngress()
        {
        }

        
        internal SecurityGroupIngress(int fromPort, int toPort, string protocol, SecurityGroup securityGroup) : base(fromPort, toPort, protocol)
        {
            SecurityGroup = securityGroup;
        }

        internal SecurityGroupIngress(int fromPort, int toPort, string protocol, string cidr) : base(fromPort, toPort, protocol, cidr)
        {
        }


        //public string SourceSecurityGroupId { get; set; }
        public string SourceSecurityGroupName { get; set; }
        public string SourceSecurityGroupOwnerId { get; set; }

        [JsonConverter(typeof(ResourceAsPropertyConverter))]
        [JsonProperty(PropertyName = "SourceSecurityGroupId")]
        public SecurityGroup SecurityGroup { get; }
    }
}
