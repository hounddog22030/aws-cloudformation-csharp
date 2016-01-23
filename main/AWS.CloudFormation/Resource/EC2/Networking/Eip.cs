using System;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Serializer;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class Eip : ResourceBase<EipProperties>, IEipProperties
    {
        public Eip(Instancing.Instance instance, string name) : base(instance.Template, "AWS::EC2::EIP", name, false)
        {
            this.Properties.Instance = instance;
            this.Properties.Domain = "vpc";
        }

        [JsonIgnore]
        public string Domain => this.Properties.Domain;

        [JsonIgnore] public string InstanceId => this.Properties.InstanceId;
    }

    public interface IEipProperties
    {
        string InstanceId { get; }
        string Domain { get; }
    }

    public class EipProperties : NewProperties, IEipProperties
    {
        [JsonIgnore]
        public Instance Instance { get; set; }
        public string InstanceId { get; }
        public string Domain { get; set; }
    }
}
