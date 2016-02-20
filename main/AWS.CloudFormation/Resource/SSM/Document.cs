using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.SSM
{
    public class Document : ResourceBase
    {
        public Document() : base(ResourceType.AwsSsmDocument)
        {
        }

        protected override bool SupportsTags => false;

        [JsonIgnore]
        public CloudFormationDictionary Content
        {
            get
            {
                if (this.Properties.GetValue<CloudFormationDictionary>() == null)
                {
                    this.Content = new CloudFormationDictionary();
                }
                return this.Properties.GetValue<CloudFormationDictionary>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

    }
}
