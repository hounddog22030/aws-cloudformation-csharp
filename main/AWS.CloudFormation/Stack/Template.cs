using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.EC2;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serialization;

using Newtonsoft.Json;

namespace AWS.CloudFormation.Stack
{
    public class Template
    {

        public const string AwsTemplateFormatVersion20100909 = "2010-09-09";
        public const string CidrIpTheWorld = "0.0.0.0/0";
        public const string ParameterKeyPairName = "KeyPairName";
        public const string ParameterDomainAdminPassword = "DomainAdminPassword";

        public Template(string stackName, string defaultKeyName, string vpcName, string vpcCidrBlock) : this(defaultKeyName,vpcName,vpcCidrBlock, stackName,null)
        {
            
        }

        public Template(string stackName, string keyPairName, string vpcName, string vpcCidrBlock, string description) : this(stackName,description)
        {
            this.Parameters.Add(ParameterKeyPairName, new ParameterBase(ParameterKeyPairName, "AWS::EC2::KeyPair::KeyName", keyPairName, "Key Pair to decrypt instance password."));
            Vpc vpc = new Vpc(vpcCidrBlock);
            this.Resources.Add(vpcName, vpc);

        }

        public Template(string name, string description)
        {
            Outputs = new CloudFormationDictionary();
            AwsTemplateFormatVersion = AwsTemplateFormatVersion20100909;

            this.StackName = name.Replace('.','-');


            if (!string.IsNullOrEmpty(description))
            {
                this.Description = description;
            }

            this.Resources = new ObservableDictionary<string, ResourceBase>();
            this.Resources.CollectionChanged += Resources_CollectionChanged;
            this.Parameters = new CloudFormationDictionary();
        }

        private void Resources_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (KeyValuePair<string,ResourceBase> newItem in e.NewItems)
                {
                    newItem.Value.LogicalId = newItem.Key;
                    newItem.Value.Template = this;
                }
            }
        }

        public string Description { get; }

        private string _stackName;
        private string v1;
        private string v2;

        [JsonIgnore]
        public string StackName {
            get { return _stackName; }
            private set
            {
                Regex r = new Regex("[a-zA-Z][-a-zA-Z0-9]*");
                if (r.Match(value).Length!=value.Length)
                {
                    throw new ArgumentException("Value does not match [a-zA-Z][-a-zA-Z0-9]*");
                }
                _stackName = value;
            }
        }

        [JsonProperty(PropertyName = "AWSTemplateFormatVersion")]
        public string AwsTemplateFormatVersion { get; }

        public ObservableDictionary<string, ResourceBase> Resources { get; }
        
        public CloudFormationDictionary Parameters { get; }

        [JsonIgnore]
        public IEnumerable<Vpc> Vpcs
        {
            get { return this.Resources.Where(r => r.Value is Vpc).Select(r=>r.Value).OfType<Vpc>(); }
        }

        public void AddParameter(ParameterBase parameter)
        {
            Parameters.Add(parameter.LogicalId,parameter);
        }

        public CloudFormationDictionary Outputs { get; }
    }

    public class ParameterBase : Dictionary<string,object>, ILogicalId
    {
        public ParameterBase(string name, string type, object defaultValue,string description)
        {
            LogicalId = name;
            this.Add("Type",type);
            this.Add("Default", defaultValue);
            this.Add("Description", description);
        }

        public string Type => this["Type"].ToString();

        public object Default
        {
            get { return this["Default"]; }
            set { this["Default"] = value; }
        }
        public string Description => this["Description"].ToString();

        public string LogicalId { get; }
        public bool NoEcho {
            get
            {
                return (bool)this["NoEcho"];
            }
            set
            {
                this["NoEcho"] = value;
            }
        }
    }
}
