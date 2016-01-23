using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.Route53
{
    public class HostedZone : ResourceBase
    {
        public HostedZone(Template template, string resourceName,string hostedZoneName) : base(template, "AWS::Route53::HostedZone", resourceName, false)
        {
            Name = hostedZoneName;
        }

        [JsonIgnore]
        public object HostedZoneConfig
        {
            get { return this.Properties.GetValue<object>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public CloudFormationDictionary HostedZoneTags
        {
            get { return this.Properties.GetValue<CloudFormationDictionary>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public object VPCs
        {
            get { return this.Properties.GetValue<object>(); }
            set { this.Properties.SetValue(value); }
        }


        [JsonIgnore]
        //[JsonProperty(PropertyName = "Name")]
        public string Name
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }
    }
}
