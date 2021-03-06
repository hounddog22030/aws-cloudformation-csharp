﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.CloudFormation
{
    public class Stack : ResourceBase
    {
        public Stack(Uri templateUrl) : base(ResourceType.AwsCloudFormationStack)
        {
            this.TemplateURL = templateUrl;
            this.Parameters = new Dictionary<string, object>();
        }

        protected override bool SupportsTags => false;

        [JsonIgnore]
        public Uri TemplateURL
        {
            get
            {
                return this.Properties.GetValue<Uri>();
            }
            private set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public Dictionary<string,object> Parameters
        {
            get
            {
                return this.Properties.GetValue<Dictionary<string, object>>();
            }
            private set
            {
                this.Properties.SetValue(value);
            }
        }

    }
}
