﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.AutoScaling
{
    public class AutoScalingGroup : ResourceBase
    {
        public AutoScalingGroup() : base(ResourceType.AwsAutoScalingAutoScalingGroup)
        {
            this.Properties["AvailabilityZones"] = new List<string>();
            this.Properties["VPCZoneIdentifier"] = new List<ReferenceProperty>();

            var d = new CloudFormationDictionary();
            d.Add("Granularity", "1Minute");
            string[] sizes = new[] { "GroupMinSize", "GroupMaxSize" };
            d.Add("Metrics", sizes);
            this.MetricsCollection = new object[] {d};
            DesiredCapacity = 1.ToString();
        }

        [JsonIgnore]
        public object LaunchConfigurationName
        {
            get
            {
                return this.Properties.GetValue<object>();
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

        [JsonIgnore]
        public string DesiredCapacity
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

        [JsonIgnore]
        public object[] MetricsCollection
        {
            get
            {
                return this.Properties.GetValue<object[]>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public object VPCZoneIdentifier
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
        public LaunchConfiguration LaunchConfiguration {
            get
            {
                var o = this.Properties.GetValue<ReferenceProperty>("LaunchConfigurationName");
                string name = (string)o["Ref"];
                return (LaunchConfiguration) this.Template.Resources[name];
            }
            set
            {
                value.AutoScalingGroup = this;
                this.Properties.SetValue("LaunchConfigurationName",new ReferenceProperty(value));
            }
        }


        //"MetricsCollection": [
        //         {
        //            "Granularity": "1Minute",
        //            "Metrics": [
        //               "GroupMinSize",
        //               "GroupMaxSize"
        //            ]
        //    }
        //      ]

        public void AddSubnetToVpcZoneIdentifier(Subnet subnet)
        {
            ((List<ReferenceProperty>)this.Properties["VPCZoneIdentifier"]).Add(new ReferenceProperty(subnet));
        }
    }
}
