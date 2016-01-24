using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;

namespace AWS.CloudFormation.Resource.AutoScaling
{
    public class LaunchConfiguration : WindowsInstance
    {
        public LaunchConfiguration(Template template,
                                string name,
                                InstanceTypes instanceType,
                                string imageId,
                                Subnet subnet)
            : base(template, name, instanceType, imageId, subnet, false)
        {
        }

        public override string Type => "AWS::AutoScaling::LaunchConfiguration";
        protected override bool SupportsTags => false;
    }
}
