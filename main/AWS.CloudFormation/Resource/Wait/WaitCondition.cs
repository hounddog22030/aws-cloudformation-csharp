using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.Wait
{
    public class WaitCondition : ResourceBase
    {
        public WaitCondition(Template template, string name, TimeSpan timeout) : base(template,"AWS::CloudFormation::WaitCondition",name,false)
        {
            Timeout = (int)timeout.TotalSeconds;
            Handle = new WaitConditionHandle(template, this.LogicalId + "Handle");
            template.AddResource(Handle);
        }

        [CloudFormationProperties]
        public int Timeout { get; }


        [JsonConverter(typeof(ResourceAsPropertyConverter))]
        [CloudFormationProperties]
        public WaitConditionHandle Handle { get; }
    }
}
