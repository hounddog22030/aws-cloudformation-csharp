using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource;

namespace AWS.CloudFormation.Property
{
    public class ReferenceProperty : CloudFormationDictionary
    {
        public ReferenceProperty(ILogicalId reference) : this(reference.LogicalId)
        {
        }

        public ReferenceProperty(string reference)
        {
            this.Add("Ref", reference);
        }

    }
}
