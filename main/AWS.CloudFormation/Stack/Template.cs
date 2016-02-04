using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.EC2;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serialization;

using Newtonsoft.Json;
using VpcGatewayAttachment = AWS.CloudFormation.Resource.EC2.Networking.Vpc.VpcGatewayAttachment;

namespace AWS.CloudFormation.Stack
{
    public class Template
    {

        public const string AwsTemplateFormatVersion20100909 = "2010-09-09";
        public const string CidrIpTheWorld = "0.0.0.0/0";
        public const string ParameterKeyPairName = "KeyPairName";
        public const string ParameterDomainAdminPassword = "DomainAdminPassword";

        public Template(string defaultKeyName, string vpcName, string vpcCidrBlock ) : this(defaultKeyName,vpcName,vpcCidrBlock,null)
        {
        }

        public Template(string keyPairName, string vpcName, string vpcCidrBlock, string description) : base()
        {

            Outputs = new CloudFormationDictionary();
            AwsTemplateFormatVersion = AwsTemplateFormatVersion20100909;
            this.Resources = new Dictionary<string, ResourceBase>();
            this.Parameters = new CloudFormationDictionary();
            this.Parameters.Add(ParameterKeyPairName, new ParameterBase(ParameterKeyPairName, "AWS::EC2::KeyPair::KeyName", keyPairName,"Key Pair to decrypt instance password."));
            Vpc vpc = new Vpc(this, vpcName, vpcCidrBlock);

            if (!string.IsNullOrEmpty(description))
            {
                this.Description = description;
            }
        }

        public string Description { get; }

        [JsonIgnore]
        public string StackName { get; set; }

        [JsonProperty(PropertyName = "AWSTemplateFormatVersion")]
        public string AwsTemplateFormatVersion { get; }

        public Dictionary<string, ResourceBase> Resources { get; private set; }
        public CloudFormationDictionary Parameters { get; private set; }

        [JsonIgnore]
        public IEnumerable<Vpc> Vpcs
        {
            get { return this.Resources.Where(r => r.Value is Vpc).Select(r=>r.Value).OfType<Vpc>(); }
        }

        //public void AddResource(ResourceBase resource)
        //{
        //    this.Resources.Add(resource.LogicalId, resource);
        //}



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
