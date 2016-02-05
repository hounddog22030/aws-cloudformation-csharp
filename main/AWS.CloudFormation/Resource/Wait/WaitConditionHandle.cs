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
        //private string name;

        //public WaitConditionHandle() : base(ResourceType.AwsCloudFormationWaitConditionHandle)
        //{
            
        //}

        public WaitConditionHandle(string name) : base(ResourceType.AwsCloudFormationWaitConditionHandle)
        {
            this.LogicalId = name;
        }

        protected override bool SupportsTags => false;

    }
}
