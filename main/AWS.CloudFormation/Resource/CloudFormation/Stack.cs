using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.CloudFormation
{
    public class Stack : ResourceBase
    {
        public Stack(Uri templateUrl) : base(ResourceType.AwsCloudFormationStack)
        {
            this.TemplateURL = templateUrl;
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
    }
}
