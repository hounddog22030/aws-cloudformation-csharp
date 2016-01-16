using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.Route53
{
    public class HostedZone : ResourceBase
    {
        public HostedZone(Template template, string resourceName,string hostedZoneName) : base(template, "AWS::Route53::HostedZone", resourceName, false)
        {
            HostedZoneName = hostedZoneName;
        }

        [CloudFormationProperties]
        public object HostedZoneConfig { get; set; }

        [CloudFormationProperties]
        public CloudFormationDictionary HostedZoneTags { get; set; }

        [CloudFormationProperties]
        public object VPCs { get; set; }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "Name")]
        public string HostedZoneName { get; }
    }
}
