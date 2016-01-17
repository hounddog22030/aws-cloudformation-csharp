using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.EC2;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Common
{
    public class CollectionAsIdsConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            CollectionThatSerializesAsIds<SecurityGroup> valueAsCollection =
                value as CollectionThatSerializesAsIds<SecurityGroup>;

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

    [JsonConverter(typeof(CollectionAsIdsConverter))]
    public class CollectionThatSerializesAsIds<T> : List<T> where T : ResourceBase
    {

    }
}
