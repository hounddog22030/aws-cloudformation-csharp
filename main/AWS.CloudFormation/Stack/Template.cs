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
        public const string CIDR_IP_THE_WORLD = "0.0.0.0/0";


        public Template(string defaultKeyName, string vpcName, string vpcCidrBlock )
        {
            Outputs = new CloudFormationDictionary();
            AwsTemplateFormatVersion = AwsTemplateFormatVersion20100909;
            this.Resources = new Dictionary<string, ResourceBase>();
            this.Parameters = new Dictionary<string, ParameterBase>();
            this.Parameters.Add(Instance.ParameterNameDefaultKeyPairKeyName, new ParameterBase(Instance.ParameterNameDefaultKeyPairKeyName, "AWS::EC2::KeyPair::KeyName", defaultKeyName));
            Vpc vpc = new Vpc(this,vpcName,vpcCidrBlock);
        }

        [JsonIgnore]
        public string StackName { get; set; }

        [JsonProperty(PropertyName = "AWSTemplateFormatVersion")]
        public string AwsTemplateFormatVersion { get; }

        public Dictionary<string, ResourceBase> Resources { get; private set; }
        public Dictionary<string, ParameterBase> Parameters { get; private set; }

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
        public ParameterBase(string name, string type, object defaultValue)
        {
            LogicalId = name;
            this.Add("Type",type);
            this.Add("Default", defaultValue);

        }

        public string Type => this["Type"].ToString();
        public object Default => this["Default"];

        public string LogicalId { get; }
    }
}
