using System;
using AWS.CloudFormation.Resource.EC2.Networking;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Common
{
    public class IdCollectionConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            IdCollection<SecurityGroup> valueAsCollection =
                value as IdCollection<SecurityGroup>;

            foreach (var collectionThatSerializesAsId in valueAsCollection)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Ref");
                writer.WriteValue(collectionThatSerializesAsId.LogicalId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
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