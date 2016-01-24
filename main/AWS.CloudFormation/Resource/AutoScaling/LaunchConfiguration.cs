using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.AutoScaling
{
    public class LaunchConfiguration : ResourceBase
    {
        public LaunchConfiguration(Template template,
                                string name,
                                InstanceTypes instanceType,
                                string imageId)
            : base(template, name)
        {
            this.InstanceType = instanceType;
            this.ImageId = imageId;
            SecurityGroups = new List<ReferenceProperty>();
        }

        public override string Type => "AWS::AutoScaling::LaunchConfiguration";
        protected override bool SupportsTags => false;

        [JsonIgnore]
        public InstanceTypes InstanceType
        {
            get
            {
                return this.Properties.GetValue<InstanceTypes>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public string ImageId
        {
            get
            {
                return this.Properties.GetValue<string>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public List<ReferenceProperty> SecurityGroups
        {
            get
            {
                return this.Properties.GetValue<List<ReferenceProperty>>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

    }
}
