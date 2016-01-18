using System;
using AWS.CloudFormation.Resource;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Serialization
{
    public class ResourceAsPropertyConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Ref");
            writer.WriteValue(((ResourceBase)value).LogicalId);
            writer.WriteEndObject();
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
