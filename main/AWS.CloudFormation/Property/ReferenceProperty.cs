using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Property
{
    public class ReferenceProperty : CloudFormationDictionary
    {
        public ReferenceProperty(ILogicalId reference) : this(reference.LogicalId)
        {
            if (string.IsNullOrEmpty(reference.LogicalId))
            {
                throw new ArgumentOutOfRangeException(nameof(reference));
            }
            this.Reference = reference;
        }

        public ReferenceProperty(string reference)
        {
            this.Add("Ref", reference);
        }

        [JsonIgnore]
        public ILogicalId Reference { get; }


    }
}
