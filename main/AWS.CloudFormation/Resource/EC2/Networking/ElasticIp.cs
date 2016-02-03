using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing;

using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class ElasticIp : ResourceBase
    {
        public ElasticIp(LaunchConfiguration instance) : base(instance.Template, $"Eip4{instance.LogicalId}", ResourceType.AwsEc2Eip)
        {
            Instance = instance;
            this.Domain = "vpc";
        }

        [JsonIgnore]
        public LaunchConfiguration Instance
        {
            get { return this.Properties.GetValue<LaunchConfiguration>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public string Domain
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        protected override bool SupportsTags {
            get { return false; } 
        }
    }
}
