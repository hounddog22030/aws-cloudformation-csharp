using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Resource.EC2
{
    public class VolumeAttachment : ResourceBase
    {
        public VolumeAttachment(    Template template, 
                                    string name, 
                                    string device, 
                                    ReferenceProperty instanceId, 
                                    object volume) : base(template, "AWS::EC2::VolumeAttachment", name, false)
        {
            Device = device;
            InstanceId = instanceId;
            VolumeId = volume;
        }

        [CloudFormationProperties]
        public string Device { get; }

        [CloudFormationProperties]
        public ReferenceProperty InstanceId { get; }

        [CloudFormationProperties]
        public object VolumeId { get; }


    }
}
