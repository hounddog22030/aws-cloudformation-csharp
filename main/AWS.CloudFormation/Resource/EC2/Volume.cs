using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2
{
    public class Volume : ResourceBase
    {
        public Volume(Template template, string name) : base(template, name)
        {
        }

        [JsonIgnore]
        public string SnapshotId
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public AvailabilityZone AvailabilityZone
        {
            get { return this.Properties.GetValue<AvailabilityZone>(); }
            set { this.Properties.SetValue(value); }
        }
        protected override bool SupportsTags => true;

        public override string Type => "AWS::EC2::Volume";
    }
}
