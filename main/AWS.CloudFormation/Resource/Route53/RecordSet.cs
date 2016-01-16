using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AWS.CloudFormation.Resource.Route53
{

    public class RecordSet : ResourceBase
    {
        public enum RecordSetTypeEnum
        {
            A,
            // ReSharper disable once InconsistentNaming
            AAAA,
            // ReSharper disable once InconsistentNaming
            CNAME,
            // ReSharper disable once InconsistentNaming
            MX,
            // ReSharper disable once InconsistentNaming
            NS,
            // ReSharper disable once InconsistentNaming
            PTR,
            // ReSharper disable once InconsistentNaming
            SOA,
            // ReSharper disable once InconsistentNaming
            SPF,
            // ReSharper disable once InconsistentNaming
            SRV,
            // ReSharper disable once InconsistentNaming
            TXT
        }


        public RecordSet(Template template, string name) : base(template, "AWS::Route53::RecordSet", name, false)
        {
        }

        [CloudFormationProperties]
        public string HostedZoneName { get; set; }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "Name")]
        public string RecordSetName => Name;

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "Type")]
        //[JsonConverter(typeof(StringEnumConverter))]
        public string RecordSetType { get; set; }

    }
}
