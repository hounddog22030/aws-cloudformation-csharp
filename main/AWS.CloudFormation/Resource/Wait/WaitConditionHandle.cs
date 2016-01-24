using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Resource.Wait
{
    public class WaitConditionHandle : ResourceBase
    {
        public WaitConditionHandle(Template template, string name) : base(template, name)
        {
            
        }
        protected override bool SupportsTags => false;

        public override string Type => "AWS::CloudFormation::WaitConditionHandle";
    }
}
