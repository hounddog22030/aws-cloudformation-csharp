using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AWS.CloudFormation.Resource.EC2.Instancing
{
    internal class InstanceTypesConverter : StringEnumConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            InstanceTypes valueAsInstanceTypes = (InstanceTypes)value;
            switch (valueAsInstanceTypes)
            {
                case InstanceTypes.T2Nano:
                    writer.WriteValue(Resource.EC2.Instance.T2Nano);
                    break;
                case InstanceTypes.T2Micro:
                    writer.WriteValue(Resource.EC2.Instance.T2Micro);
                    break;
                case InstanceTypes.T2Small:
                    writer.WriteValue(Resource.EC2.Instance.T2Small);
                    break;
                case InstanceTypes.M4Xlarge:
                    writer.WriteValue(Resource.EC2.Instance.T2Small);
                    break;

                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }

    [JsonConverter(typeof(InstanceTypesConverter))]
    public enum InstanceTypes
    {
        None,
        T2Nano,
        T2Micro,
        T2Small,
        M4Xlarge
    }
}
