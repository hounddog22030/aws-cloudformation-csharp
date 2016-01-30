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
    public enum Region
    {
        [EnumMember(Value = "invalid")]
        None,
        [EnumMember(Value = "us-east-1")]
        UsEast1,
        [EnumMember(Value = "us-west-1")]
        UsWest1
    }
    /**
    sa-east-1, us-west-1, us-west-2, cn-north-1, ap-northeast-1, ap-southeast-1, eu-west-1, us-east-1, ap-southeast-2, eu-central-1
    **/
}
