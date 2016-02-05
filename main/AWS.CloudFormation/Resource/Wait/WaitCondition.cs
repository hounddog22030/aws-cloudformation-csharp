using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Serialization;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.Wait
{
    public class WaitCondition : ResourceBase
    {

        public static readonly TimeSpan TimeoutMax = new TimeSpan(12,0,0);
        public WaitCondition(TimeSpan timeout) : base(ResourceType.AwsCloudFormationWaitCondition)
        {
            Timeout = (int)timeout.TotalSeconds;
        }

        public WaitCondition() : this(TimeoutMax)
        {
        }

        protected override void OnTemplateSet(Template template)
        {
            base.OnTemplateSet(template);
            var name = this.LogicalId + "Handle";
            Handle = new WaitConditionHandle(name);
            template.Resources.Add(name, Handle);
        }

        protected override bool SupportsTags => false;


        [JsonIgnore]
        public int Timeout
        {
            get { return this.Properties.GetValue<int>(); }
            set { this.Properties.SetValue(value); }
        }



        [JsonIgnore]
        [JsonProperty(PropertyName = "Handle")]
        public WaitConditionHandle Handle
        {
            get { return this.Properties.GetValue<WaitConditionHandle>("Handle"); }
            set { this.Properties.SetValue(value); }
        }

    }
}
