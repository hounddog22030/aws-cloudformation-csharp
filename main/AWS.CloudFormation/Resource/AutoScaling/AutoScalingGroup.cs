using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.AutoScaling
{
    public class AutoScalingGroup : ResourceBase
    {
        public AutoScalingGroup(Template template, string name) : base(template, name)
        {
            
        }

        [JsonIgnore]
        public object LaunchConfigurationName
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public string MinSize
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
        public string MaxSize
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

        protected override bool SupportsTags {
            get { return true; }
        }
        public override string Type {
            get { return "AWS::AutoScaling::AutoScalingGroup"; }
        }
    }
}
