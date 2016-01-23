using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using EnumConverter = AWS.CloudFormation.Serialization.EnumConverter;

namespace AWS.CloudFormation.Resource.EC2.Instancing
{
    [JsonConverter(typeof(EnumConverter))]
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
