﻿using System;
using System.Linq;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata
{


    [JsonConverter(typeof(InitConverter))]
    public class Init : CloudFormationDictionary
    {

        public Init(LaunchConfiguration resource) : base(resource)
        {
            Instance = resource;
        }

        [JsonIgnore]
        public ConfigSets ConfigSets
        {
            get
            {
                if (this.ContainsKey("configSets"))
                {
                    return this["configSets"] as ConfigSets;
                }
                else
                {
                    return this.Add("configSets", new ConfigSets(this.Instance)) as ConfigSets;
                }
            }
        }

        public LaunchConfiguration Instance { get;  }

        public class InitConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {

                Init valueAsInit = (Init)value;
                if (valueAsInit.ConfigSets.Any())
                {

                    writer.WriteStartObject();
                    writer.WritePropertyName("configSets");
                    writer.WriteStartObject();
                    foreach (var configSet in valueAsInit.ConfigSets)
                    {

                        writer.WritePropertyName(configSet.Key);
                        writer.WriteStartArray();
                        var configSetAsConfigSet = configSet.Value as ConfigSet;

                        foreach (var x in configSetAsConfigSet)
                        {
                            writer.WriteValue(x.Key);
                        }
                        writer.WriteEndArray();
                    }
                    writer.WriteEndObject();


                    foreach (var configSetKeyValuePair in valueAsInit.ConfigSets)
                    {
                        //writer.WritePropertyName(configSetKeyValuePair.Key);
                        //writer.WriteStartObject();
                        foreach (var configKeyValuePair in (ConfigSet)configSetKeyValuePair.Value)
                        {
                            writer.WritePropertyName(configKeyValuePair.Key);
                            serializer.Serialize(writer, configKeyValuePair.Value);
                        }
                        //writer.WriteEndObject();


                    }
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
    }


}
