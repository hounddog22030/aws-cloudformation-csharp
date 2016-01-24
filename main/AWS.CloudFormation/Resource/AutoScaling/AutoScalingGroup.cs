using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.AutoScaling
{
    public class AutoScalingGroup : ResourceBase
    {
        public AutoScalingGroup(Template template, string name) : base(template, name)
        {
            this.Properties["AvailabilityZones"] = new List<string>();
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

        protected override bool SupportsTags => false;
        public override string Type => "AWS::AutoScaling::AutoScalingGroup";

        [JsonIgnore]
        public string[] AvailabilityZones
        {
            get
            {
                return this.Properties.GetValue<List<string>>().ToArray();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        public void AddAvailabilityZone(AvailabilityZone zone)
        {
            MemberInfo[] memberInfos = typeof(AvailabilityZone).GetMembers(BindingFlags.Public | BindingFlags.Static);
            var theMember = memberInfos.Single(r => r.Name.ToString() == zone.ToString());
            
            var theEnumMemberAttribute = theMember.GetCustomAttributes<EnumMemberAttribute>().First();
            ((List<string>)this.Properties["AvailabilityZones"]).Add(theEnumMemberAttribute.Value);

        }

    }
}
