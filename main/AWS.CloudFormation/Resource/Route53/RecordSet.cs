using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
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


        private RecordSet(Template template, string name) : base(template, "AWS::Route53::RecordSet", name, false)
        {
            template.AddResource(this);
            TTL = "900";
        }

        [CloudFormationProperties]
        public string HostedZoneName { get; set; }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "Name")]
        public string RecordSetName { get; private set; }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "Type")]
        //[JsonConverter(typeof(StringEnumConverter))]
        public string RecordSetType { get; set; }

        [CloudFormationProperties]
        public string HostedZoneId { get; private set; }

        [CloudFormationProperties]
        // ReSharper disable once InconsistentNaming
        public string TTL { get; set; }

        List<object> _resourceRecords = new List<object>();

        [CloudFormationProperties]
        public object[] ResourceRecords => _resourceRecords.ToArray();

        public void AddResourceRecord(object resourceRecord)
        {
            _resourceRecords.Add(resourceRecord);
        }

        public static RecordSet AddByHostedZoneId(Template template, string resourceName, string hostedZoneId)
        {
            return new RecordSet(template, resourceName) { HostedZoneId = hostedZoneId };
        }

        public static RecordSet AddByHostedZoneId(Template template, string resourceName, string hostedZoneId, string dnsName)
        {
            return new RecordSet(template, resourceName) { HostedZoneId = hostedZoneId, RecordSetName = dnsName };
        }

        public static RecordSet AddByHostedZoneName(Template template, string name, string hostedZoneName, string dnsName)
        {
            return new RecordSet(template, name) { HostedZoneName = hostedZoneName, RecordSetName = dnsName };
        }
    }
}
