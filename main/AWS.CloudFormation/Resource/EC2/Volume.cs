// 0e34235e264c315ab1efa46d3316d84ca21a688f
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
        public Volume() : base(ResourceType.AwsEc2Volume)
        {
        }

        public Volume(int size): this()
        {
            this.Size = size.ToString();
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

        [JsonIgnore]
        public string Size
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        protected override bool SupportsTags => true;

    }
}
