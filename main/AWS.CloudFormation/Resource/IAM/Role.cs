using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.IAM
{
//"Properties": {
//      "AssumeRolePolicyDocument": { JSON },
//      "ManagedPolicyArns": [String, ... ],
//      "Path": String,
//      "Policies": [Policies, ... ]
//   }
    public class Role : ResourceBase
    {
        public Role(CloudFormationDictionary rolePolicyDocument, object[] policies, string path) : base(ResourceType.AwsIamRole)
        {
            this.AssumeRolePolicyDocument = rolePolicyDocument;
            this.Path = path;
            this.Policies = policies;
        }

        [JsonIgnore]
        public object[] Policies
        {
            get
            {
                return this.Properties.GetValue<object[]>();
            }
            private set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public string Path
        {
            get
            {
                return this.Properties.GetValue<string>();
            }
            private set { this.Properties.SetValue(value); }
        }

        public Role(CloudFormationDictionary assumeRolePolicyDocument) : base(ResourceType.AwsIamRole)
        {
            this.AssumeRolePolicyDocument = assumeRolePolicyDocument;
        }

        protected override bool SupportsTags => false;

        [JsonIgnore]
        public object AssumeRolePolicyDocument
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            private set { this.Properties.SetValue(value); }
        }
    }
}
