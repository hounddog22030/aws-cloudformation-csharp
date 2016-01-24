﻿using AWS.CloudFormation.Resource.EC2.Instancing;

using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class ElasticIp : ResourceBase
    {
        public ElasticIp(Instancing.Instance instance, string name) : base(instance.Template, "AWS::EC2::EIP", name, false)
        {
            Instance = instance;
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
