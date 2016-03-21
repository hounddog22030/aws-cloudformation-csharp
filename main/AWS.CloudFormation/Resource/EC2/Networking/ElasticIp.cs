using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing;

using Newtonsoft.Json;
using System;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class ElasticIp : ResourceBase
    {
        public ElasticIp(LaunchConfiguration instance) : this()
        {
            Instance = instance;
        }
        public ElasticIp() : base(ResourceType.AwsEc2Eip)
        {
            this.Domain = "vpc";
        }

        public override string LogicalId {
            get
            {
                if (base.LogicalId == null)
                {
                    var logicalId = "Eip";
                    if (this.Instance != null)
                    {
                        logicalId += $"4{this.Instance.LogicalId}";
                    }
                    else
                    {
                        logicalId += DateTime.Now.Ticks;
                    }
                    this.LogicalId = logicalId;
                }
                return base.LogicalId;
            }
            internal set { base.LogicalId = value; }
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
