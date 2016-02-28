using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.EC2.Networking;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Stack
{
    public class Output : CloudFormationDictionary, ILogicalId
    {
        public Output(string logicalId, ReferenceProperty value) : this(logicalId,(object)value)
        {
        }
        public Output(string logicalId, FnGetAtt value) : this(logicalId, (object)value)
        {
        }

        public Output(string logicalId, FnJoin value) : this(logicalId, (object)value)
        {
        }

        private Output(string logicalId,object value)
        {
            LogicalId = ResourceBase.NormalizeLogicalId(logicalId);
            Value = value;
        }

        public object Value {
            get
            {
                return this["Value"];
            }
            private set { this["Value"] = value; }
        }

        private string _logicalId = string.Empty;


        [JsonIgnore]
        public string LogicalId {
            get { return _logicalId; }
            private set { _logicalId = ResourceBase.NormalizeLogicalId(value); }
        }
    }
}
