using System;
using System.Collections.Generic;
using AWS.CloudFormation.Resource;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Common
{
    [JsonConverter(typeof(CloudFormationDictionaryConverter))]
    public class CloudFormationDictionary : Dictionary<string, object>
    {
        public CloudFormationDictionary()
        {
            
        }
        public CloudFormationDictionary(ResourceBase resource)
        {
            this.Resource = resource;
        }

        public ResourceBase Resource { get; internal set; }


        public string Type
        {
            get
            {
                if (this.ContainsKey("type"))
                {
                    return this["type"] as string;
                }
                return null;
            }
            set
            {
                if (this.ContainsKey("type"))
                {
                    this["type"] = value;
                }
                else
                {
                    this.Add("type", value);
                }
            }
        }

        public CloudFormationDictionary Add(string key)
        {
            return Add(key, new CloudFormationDictionary(this.Resource));
        }


        public CloudFormationDictionary Add(string key, CloudFormationDictionary value)
        {
            base.Add(key, value);
            return value;
        }

        public void SetFnJoin(params object[] fnJoinElements)
        {
            AddFnJoin("", fnJoinElements);
        }
        private void AddFnJoin(string delimiter, params object[] fnJoinElements)
        {
            var final = new object[] { delimiter, fnJoinElements };
            base.Add("Fn::Join", final);
        }
    }

    public class CloudFormationDictionaryConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new System.NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            throw new System.NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new System.NotImplementedException();
        }
    }
}
