using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.Route53
{
    public class HostedZone : ResourceBase
    {
        public HostedZone(string hostedZoneName) 
            : base(ResourceType.AwsRoute53HostedZone)
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
        public object[] VPCs
        {
            get { return this.Properties.GetValue<object[]>(); }
            private set { this.Properties.SetValue(value); }
        }

        public void AddVpc(Vpc vpc, Region region)
        {
            var temp = new List<object>();
            if (this.VPCs != null && this.VPCs.Any())
            {
                temp.AddRange(this.VPCs);
            }
            var hostedVpc = new HostedZoneVPC()
            {
                VPCId = new ReferenceProperty(vpc),
                VPCRegion = region
            };
            temp.Add(hostedVpc);
            this.VPCs = temp.ToArray();
        }


        [JsonIgnore]
        //[JsonProperty(PropertyName = "Name")]
        public string Name
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        protected override bool SupportsTags {
            get { return false; }
        }

        protected class HostedZoneVPC
        {
            public ReferenceProperty VPCId { get; set; }
            public Region VPCRegion { get; set; }
        }
    }
}
