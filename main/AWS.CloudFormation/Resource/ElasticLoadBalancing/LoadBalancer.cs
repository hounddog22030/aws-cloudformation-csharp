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
        public LoadBalancer() : base(ResourceType.AwsElasticLoadBalancingLoadBalancer)
        {
            SecurityGroups = new IdCollection<SecurityGroup>();

        }

        [JsonIgnore]
        public List<Listener> Listeners
        {
            get
            {
                if (this.Properties.GetValue<List<Listener>>() == null)
                {
                    this.Listeners = new List<Listener>();
                }
                return this.Properties.GetValue<List<Listener>>();
            }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public HealthCheck HealthCheck
        {
            get
            {
                if (this.Properties.GetValue<HealthCheck>() == null)
                {
                    this.HealthCheck = new HealthCheck();
                }
                return this.Properties.GetValue<HealthCheck>();
            }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public List<ReferenceProperty> Instances
        {
            get
            {
                if (this.Properties.GetValue<List<ReferenceProperty>>() == null)
                {
                    this.Instances = new List<ReferenceProperty>();
                }
                return this.Properties.GetValue<List<ReferenceProperty>>();
            }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public List<object> Subnets
        {
            get
            {
                if (this.Properties.GetValue<List<object>>() == null)
                {
                    this.Subnets = new List<object>();
                }
                return this.Properties.GetValue<List<object>>();
            }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public IdCollection<SecurityGroup> SecurityGroups
        {
            get { return this.Properties.GetValue<IdCollection<SecurityGroup>>(); }
            set { this.Properties.SetValue(value); }
        }

        public class Listener
        {
            public Listener(int loadBalancerPort, int instancePort, string protocol)
            {
                LoadBalancerPort = loadBalancerPort;
                InstancePort = instancePort;
                Protocol = protocol;
            }

            public int LoadBalancerPort { get; }
            public int InstancePort { get; }
            public string Protocol { get; }
            public string SSLCertificateId { get; set; }
        }

        protected override bool SupportsTags {
            get { return false;}
        }

    }
    public class HealthCheck
    {
        public string HealthyThreshold { get; set; }
        public string Interval { get; set; }
        public string Target { get; set; }
        public string Timeout { get; set; }
        public string UnhealthyThreshold { get; set; }
    }
}
