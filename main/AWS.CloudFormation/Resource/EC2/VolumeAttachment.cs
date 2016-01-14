using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Resource.EC2
{
    public class VolumeAttachment : ResourceBase
    {
        public VolumeAttachment(Template template, string name, string device, string instanceId, string volumeId) : base(template, "AWS::EC2::VolumeAttachment", name, false)
        {
            Device = device;
            InstanceId = instanceId;
            VolumeId = volumeId;
        }

        [CloudFormationProperties]
        public string Device { get; }

        [CloudFormationProperties]
        public string InstanceId { get; }

        [CloudFormationProperties]
        public string VolumeId { get; }


    }
}
