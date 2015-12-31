using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Instance;
using AWS.CloudFormation.Resource;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Serializer
{
    public class ResourceJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {

            ResourceBase valueAsResourceBase = value as ResourceBase;

            List<PropertyInfo> propertiesToSerializeAsAwsProperties = new List<PropertyInfo>();
            PropertyInfo MetadataPropertyInfo = null;


            foreach (var propertyInfo in value.GetType().GetProperties())
            {
                if (!propertyInfo.GetCustomAttributes(typeof (JsonIgnoreAttribute), true).Any())
                {
                    if (propertyInfo.GetCustomAttributes(typeof(CloudFormationPropertiesAttribute), true).Any())
                    {
                        propertiesToSerializeAsAwsProperties.Add(propertyInfo);
                    }
                    else if (propertyInfo.Name == "Metadata")
                    {
                        MetadataPropertyInfo = propertyInfo;
                    }
                }
            }

            // the resource itself
            writer.WriteStartObject();

            writer.WritePropertyName("Type");
            writer.WriteValue(valueAsResourceBase.Type);

            if (!string.IsNullOrEmpty(valueAsResourceBase.DependsOn))
            {
                writer.WritePropertyName("DependsOn");
                writer.WriteValue(valueAsResourceBase.DependsOn);

            }


            writer.WritePropertyName("Properties");

            // properties
            writer.WriteStartObject();


            foreach (var propertyInfo in propertiesToSerializeAsAwsProperties)
            {
                var propertyValue = propertyInfo.GetValue(value);

                if (propertyValue != null && (!(propertyValue is ICollection) || (propertyValue is ICollection && ((ICollection)propertyValue).Count > 0 )))
                {
                    var propertyType = propertyValue.GetType();
                    var serializeAsName = propertyInfo.GetCustomAttributes(typeof(JsonPropertyAttribute), true);
                    var nameToSerializeAs = propertyInfo.Name;

                    if (serializeAsName != null && serializeAsName.Length > 0)
                    {
                        nameToSerializeAs = ((JsonPropertyAttribute)serializeAsName[0]).PropertyName;
                    }
                    writer.WritePropertyName(nameToSerializeAs);


                    if (propertyType.IsPrimitive || propertyType == typeof(Decimal) ||
                        propertyType == typeof(String))
                    {
                        writer.WriteValue(propertyValue);
                    }
                    else
                    {
                        if (typeof(ResourceBase).IsAssignableFrom(propertyValue.GetType()))
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName("Ref");
                            var resource = propertyValue as ResourceBase;
                            writer.WriteValue(resource.Name);
                            writer.WriteEndObject();
                        }
                        else
                        {
                            serializer.Serialize(writer, propertyValue);
                        }
                    }
                }
            }
            // properties
            writer.WriteEndObject();

            if (MetadataPropertyInfo != null)
            {
                var propertyValue = MetadataPropertyInfo.GetValue(value);
                Dictionary<string, object> Metadata = propertyValue as Dictionary<string, object>;

                if (Metadata != null && Metadata.Any())
                {
                    writer.WritePropertyName("Metadata");
                    serializer.Serialize(writer, Metadata);
                }
            }

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
