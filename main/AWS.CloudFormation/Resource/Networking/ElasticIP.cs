using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.Networking
{
    public class ElasticIP : ResourceBase
    {
        public ElasticIP(Instance.Instance instance, string name) : base(instance.Template, "AWS::EC2::EIP", name, false)
        {
            Instance = instance;
            this.Domain = "vpc";
        }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "InstanceId")]
        public Instance.Instance Instance { get; }

        [CloudFormationProperties]
        public string Domain { get; private set; }
    }
}
