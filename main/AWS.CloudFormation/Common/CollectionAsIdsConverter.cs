using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class IdCollectionConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartArray();

            foreach (var item in ((IEnumerable)value))
            {
                writer.WriteStartObject();
                if (item is ILogicalId)
                {
                    writer.WritePropertyName("Ref");
                    writer.WriteValue(((ILogicalId)item).LogicalId);
                }
                else
                {
                    writer.WriteValue(item);
                }
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

    [JsonConverter(typeof(IdCollectionConverter))]
    public class IdCollection<T> : List<T>
    {

    }
}
