using System;
using System.Collections.Generic;
using System.IO;
using AWS.CloudFormation.Common;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Instance.MetaData.Config.Command
{


    public class CommandConverter : JsonConverter
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

    public class Command : CloudFormationDictionary
    {
        public ConfigCommand Parent { get; internal set; }

        public virtual void AddCommandLine(params object[] commandLine)
        {
        }
    }
}
