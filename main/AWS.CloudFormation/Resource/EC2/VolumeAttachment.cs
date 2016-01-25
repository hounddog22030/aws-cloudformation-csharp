using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2
{
    public class VolumeAttachment : ResourceBase
    {
        public VolumeAttachment(    Template template, 
                                    string name, 
                                    string device, 
                                    Instance instance, 
                                    string volumeId) : base(template, name, ResourceType.AwsEc2VolumeAttachment)
        {
            Device = device;
            Instance = instance;
            VolumeId = volumeId;
        }

        [JsonIgnore]
        public string Device
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }


        [JsonIgnore]

        public Instance Instance
        {
            get { return this.Properties.GetValue<Instance>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public string VolumeId
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        protected override bool SupportsTags => false;


    }
}
