﻿using System;
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
        public WaitCondition(Template template, string name, TimeSpan timeout) : base(template,name)
        {
            Timeout = (int)timeout.TotalSeconds;
            Handle = new WaitConditionHandle(template, this.LogicalId + "Handle");
        }

        protected override bool SupportsTags => false;

        public override string Type => "AWS::CloudFormation::WaitCondition";

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
            get { return this.Properties.GetValue<WaitConditionHandle>(); }
            set { this.Properties.SetValue(value); }
        }

    }
}
