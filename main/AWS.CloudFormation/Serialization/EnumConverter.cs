using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AWS.CloudFormation.Serialization
{
    internal class EnumConverter : StringEnumConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            MemberInfo[] memberInfos = value.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static);
            var theMember = memberInfos.Single(r => r.Name.ToString() == value.ToString());
            var theEnumMemberAttribute = theMember.GetCustomAttributes<EnumMemberAttribute>().First();
            writer.WriteValue(theEnumMemberAttribute.Value);
        }
    }
}