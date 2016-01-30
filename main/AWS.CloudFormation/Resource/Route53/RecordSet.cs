using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;

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


        private RecordSet(Template template, string name, RecordSetTypeEnum recordSetType) : base(template, name, ResourceType.AwsRoute53RecordSet)
        {
            TTL = "900";
            this.RecordSetType = recordSetType.ToString();
            this.ResourceRecords = new IdCollection<object>();
        }

        [JsonIgnore]
        public string HostedZoneName
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public string Name
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }


        [JsonProperty(PropertyName = "Type")]
        [JsonIgnore]
        public string RecordSetType
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }


        [JsonIgnore]
        public string HostedZoneId
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }


        [JsonIgnore]
        // ReSharper disable once InconsistentNaming
        public string TTL
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        List<object> _resourceRecords = new List<object>();

        [JsonIgnore]
        public IdCollection<object> ResourceRecords
        {
            get { return this.Properties.GetValue<IdCollection<object>>(); }
            set { this.Properties.SetValue(value); }
        }

        //public void AddResourceRecord(object resourceRecord)
        //{
        //    List<object> temp = new List<object>();
        //    temp.AddRange(this.ResourceRecords);
        //    temp.Add(resourceRecord);
        //    this.ResourceRecords = temp.ToArray();
        //}

        public static RecordSet AddByHostedZoneId(Template template, string resourceName, string hostedZoneId, string dnsName, RecordSetTypeEnum recordSetType)
        {
            return new RecordSet(template, resourceName, recordSetType) { HostedZoneId = hostedZoneId, Name = dnsName };
        }

        public static RecordSet AddByHostedZoneName(Template template, string name, string hostedZoneName, string dnsName, RecordSetTypeEnum recordSetType)
        {
            return new RecordSet(template, name, recordSetType) { HostedZoneName = hostedZoneName, Name = dnsName };
        }

        protected override bool SupportsTags {
            get { return false; }
        }
    }
}
