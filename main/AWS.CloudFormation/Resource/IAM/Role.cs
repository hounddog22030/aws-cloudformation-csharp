using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy;
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
        public Role() : base(ResourceType.AwsIamRole)
        {
            this.Policies = new List<Policy>();
        }

        [JsonIgnore]
        public List<Policy> Policies
        {
            get
            {
                return this.Properties.GetValue<List<Policy>>();
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
            set { this.Properties.SetValue(value); }
        }


        protected override bool SupportsTags => false;

        [JsonIgnore]
        public PolicyDocument AssumeRolePolicyDocument
        {
            get
            {
                return this.Properties.GetValue<PolicyDocument>();
            }
            set { this.Properties.SetValue(value); }
        }
    }

    public class PolicyDocument : CloudFormationDictionary
    {
        public PolicyDocument()
        {
            this.Add("Version", "2012-10-17");
            this.Statement = new List<Statement>();
            
        }

        [JsonIgnore]
        public List<Statement> Statement
        {
            get
            {
                return this.GetValue<List<Statement>>();
            }
            private set { this.SetValue(value); }
        }
    }

    public class Statement : CloudFormationDictionary
    {
        [JsonIgnore]
        public string Effect
        {
            get
            {
                return this.GetValue<string>();
            }
            set { this.SetValue(value); }
        }

        [JsonIgnore]
        public Principal Principal
        {
            get
            {
                return this.GetValue<Principal>();
            }
            set { this.SetValue(value); }
        }

        [JsonIgnore]
        public object Action
        {
            get
            {
                return this.GetValue<object>();
            }
            set { this.SetValue(value); }
        }

        [JsonIgnore]
        public string Resource
        {
            get
            {
                return this.GetValue<string>();
            }
            set { this.SetValue(value); }
        }
    }

    public class Principal : CloudFormationDictionary
    {
        public Principal()
        {
            this.Service = new List<string>();
        }

        [JsonIgnore]
        public List<string> Service
        {
            get
            {
                return this.GetValue<List<string>>();
            }
            set { this.SetValue(value); }
        }
    }

    public class Policy : CloudFormationDictionary
    {
        [JsonIgnore]
        public string PolicyName
        {
            get
            {
                return this.GetValue<string>();
            }
            set { this.SetValue(value); }
        }
        [JsonIgnore]
        public PolicyDocument PolicyDocument
        {
            get
            {
                return this.GetValue<PolicyDocument>();
            }
            set { this.SetValue(value); }
        }
    }
}
