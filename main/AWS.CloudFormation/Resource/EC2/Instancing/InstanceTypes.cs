using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AWS.CloudFormation.Resource.EC2.Instancing
{
    internal class InstanceTypesConverter : StringEnumConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var names = System.Enum.GetNames(value.GetType());
            object underlyingValue = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));
            MemberInfo[] memberInfos = value.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static);
            var theMember = memberInfos.Single(r => r.Name.ToString() == value.ToString());
            var theEnumMemberAttribute = theMember.GetCustomAttributes<EnumMemberAttribute>().First();
            writer.WriteValue(theEnumMemberAttribute.Value);

            //string alerta = "";
            //for (int i = 0; i < memberInfos.Length; i++)
            //{
            //    alerta += memberInfos[i].Name + " - ";
            //    alerta += memberInfos[i].GetType().Name + "\n";
            //}

            //InstanceTypes valueAsInstanceTypes = (InstanceTypes)value;
            //switch (valueAsInstanceTypes)
            //{
            //    case InstanceTypes.T2Nano:
            //        writer.WriteValue(Instance.T2Nano);
            //        break;
            //    case InstanceTypes.T2Micro:
            //        writer.WriteValue(Instance.T2Micro);
            //        break;
            //    case InstanceTypes.T2Small:
            //        writer.WriteValue(Instance.T2Small);
            //        break;
            //    case InstanceTypes.M4Xlarge:
            //        writer.WriteValue(Instance.T2Small);
            //        break;

            //    default:
            //        throw new InvalidEnumArgumentException();
            //}
        }
    }

    [JsonConverter(typeof(InstanceTypesConverter))]
    public enum InstanceTypes
    {
        None,
        [EnumMember(Value="t2.nano")]
        T2Nano,
        [EnumMember(Value = "t2.micro")]
        T2Micro,
        [EnumMember(Value = "t2.small")]
        T2Small,
        [EnumMember(Value = "m4.xlarge")]
        M4Xlarge
    }
}
