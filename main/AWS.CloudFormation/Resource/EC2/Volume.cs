using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2
{
    public class Volume : ResourceBase
    {
        public Volume(Template template, string name) : base(template, "AWS::EC2::Volume", name, true)
        {
        }

        [JsonIgnore]
        public string SnapshotId
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public Template.AvailabilityZone AvailabilityZone
        {
            get { return this.Properties.GetValue<Template.AvailabilityZone>(); }
            set { this.Properties.SetValue(value); }
        }
    }
}
