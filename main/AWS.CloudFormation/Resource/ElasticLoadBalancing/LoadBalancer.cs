using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.ElasticLoadBalancing
{

    [JsonConverter(typeof(JsonConverterListThatSerializesAsRef))]
    public class ListThatSerializesAsRef : List<ILogicalId>
    {
        
    }

    public class JsonConverterListThatSerializesAsRef : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ListThatSerializesAsRef valueAsList = value as ListThatSerializesAsRef;
            foreach (var item in valueAsList)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Ref");
                writer.WriteValue(item.LogicalId);
                writer.WriteEndObject();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }

    public class LoadBalancer : ResourceBase
    {
        public LoadBalancer(Template template, string name) : base(template, "AWS::ElasticLoadBalancing::LoadBalancer", name, false)
        {
            SecurityGroups = new IdCollection<SecurityGroup>();

        }

        public void AddInstance(EC2.Instancing.Instance instance)
        {
            List<ReferenceProperty> tempInstances = new List<ReferenceProperty>();
            if (this.Instances != null && this.Instances.Length > 0)
            {
                tempInstances.AddRange(this.Instances);
            }
            tempInstances.Add(new ReferenceProperty() { Ref = instance.LogicalId });
            this.Instances = tempInstances.ToArray();
        }
        public void AddSubnet(Subnet Subnet)
        {
            List<ReferenceProperty> tempSubnets = new List<ReferenceProperty>();
            if (this.Subnets != null && this.Subnets.Length > 0)
            {
                tempSubnets.AddRange(this.Subnets);
            }
            tempSubnets.Add(new ReferenceProperty() { Ref = Subnet.LogicalId });
            this.Subnets = tempSubnets.ToArray();
        }
        public void AddListener(string loadBalancePort,string instancePort, string protocol)
        {
            List<Listener> tempListeners = new List<Listener>();
            if (this.Listeners != null && this.Listeners.Length > 0)
            {
                tempListeners.AddRange(this.Listeners);
            }
            tempListeners.Add(new Listener(loadBalancePort, instancePort, protocol));
            this.Listeners = tempListeners.ToArray();
        }

        [CloudFormationProperties]
        public Listener[] Listeners { get; private set; }

        [CloudFormationProperties]
        public ReferenceProperty[] Instances { get; private set; }

        [CloudFormationProperties]
        public ReferenceProperty[] Subnets { get; private set; }

        [CloudFormationProperties]
        public IdCollection<SecurityGroup> SecurityGroups { get; private set; }

        public class Listener
        {
            public Listener(string loadBalancerPort, string instancePort, string protocol)
            {
                LoadBalancerPort = loadBalancerPort;
                InstancePort = instancePort;
                Protocol = protocol;
            }

            public string LoadBalancerPort { get; }
            public string InstancePort { get; }
            public string Protocol { get; }


        }
    }
}
