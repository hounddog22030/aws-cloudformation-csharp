using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Stack
{
    public class Output : CloudFormationDictionary, ILogicalId
    {
        public Output(string logicalId, ReferenceProperty value)
        {
            LogicalId = logicalId;
            Value = value;
        }

        public ReferenceProperty Value {
            get
            {
                return this["Value"] as ReferenceProperty;
            }
            private set { this["Value"] = value; }
        }
        [JsonIgnore]
        public string LogicalId { get; }
    }
}
