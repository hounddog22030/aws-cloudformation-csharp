using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.ElasticLoadBalancing
{

    [JsonConverter(typeof(JsonConverterListThatSerializesAsRef))]
    public class ListThatSerializesAsRef : List<IName>
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
                writer.WriteValue(item.Name);
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
        }

        public void AddInstance(Instance.Instance instance)
        {
            List<ReferenceProperty> tempInstances = new List<ReferenceProperty>();
            if (this.Instances !=null && this.Instances.Length > 0)
            {
                tempInstances.AddRange(this.Instances);
            }
            tempInstances.Add(new ReferenceProperty() {Ref  = instance.Name});
            this.Instances = tempInstances.ToArray();
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
            //this.Listeners.Add(new Listener(loadBalancePort,instancePort,protocol));
        }

        [CloudFormationProperties]
        public Listener[] Listeners { get; private set; }

        [CloudFormationProperties]
        public ReferenceProperty[] Instances { get; private set; }

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
