using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Resource.EC2
{
    public class Volume : ResourceBase
    {
        public Volume(Template template, string name) : base(template, "AWS::EC2::Volume", name, true)
        {
        }

        [CloudFormationProperties]
        public string SnapshotId { get; set; }

        [CloudFormationProperties]
        public string AvailabilityZone { get; set; }
    }
}
