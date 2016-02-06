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
        public VolumeAttachment(string device, Instance instance) : base(ResourceType.AwsEc2VolumeAttachment)
        {
            Device = device;
            Instance = instance;
        }

        public VolumeAttachment(string device, Instance instance, Volume volume) : this(device, instance)
        {
            VolumeId = new ReferenceProperty(volume);
            this.LogicalId = $"VolumeAttachment{Instance.LogicalId}{volume.LogicalId}";
        }
        public VolumeAttachment(string device, Instance instance, string volumeId) : this(device,instance)
        {
            VolumeId = volumeId;
            this.LogicalId = $"VolumeAttachment{Instance.LogicalId}{Device}{volumeId}".Replace("/",string.Empty);
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
        public object VolumeId
        {
            get { return this.Properties.GetValue<object>(); }
            set { this.Properties.SetValue(value); }
        }

        protected override bool SupportsTags => false;


    }
}
