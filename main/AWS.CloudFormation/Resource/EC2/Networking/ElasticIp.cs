using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class ElasticIp : ResourceBase
    {
        public ElasticIp(Instancing.Instance instance, string name) : this(instance.Template,name)
        {
            Instance = instance;
        }
        public ElasticIp(Template template, string name) : base(template, "AWS::EC2::EIP", name, false)
        {
            this.Domain = "vpc";
        }

        [JsonIgnore]
        public Instance Instance
        {
            get { return this.Properties.GetValue<Instance>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public string Domain
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }
    }
}
