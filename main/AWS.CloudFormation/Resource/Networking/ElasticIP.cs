using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Serializer;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.Networking
{
    public class ElasticIP : ResourceBase
    {
        public ElasticIP(string name) : base( "AWS::EC2::EIP", name, false)
        {
            this.Domain = "vpc";
        }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "InstanceId")]
        public Instance.Instance Instance { get; set; }

        public static ElasticIP Create(string name, Instance.Instance instance)
        {
            ElasticIP returnValue = new ElasticIP( name ) {Instance = instance};
            return returnValue;
        }
        [CloudFormationProperties]
        public string Domain { get; private set; }
    }
}
