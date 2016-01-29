using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Serialization;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Property
{
    [JsonConverter(typeof(EnumConverter))]
    public enum AvailabilityZone
    {
        [EnumMember(Value = "invalid")]
        None,
        [EnumMember(Value = "us-east-1a")]
        UsEast1A,
        [EnumMember(Value = "us-east-1e")]
        UsEast1E
    }
}
