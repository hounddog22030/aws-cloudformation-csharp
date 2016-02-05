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


        private RecordSet(RecordSetTypeEnum recordSetType) 
            : base(ResourceType.AwsRoute53RecordSet)
        {
            TTL = "900";
            this.RecordSetType = recordSetType.ToString();
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
        public object[] ResourceRecords
        {
            get { return this.Properties.GetValue<object[]>(); }
            set { this.Properties.SetValue(value); }
        }

        public void AddResourceRecord(object resourceRecord)
        {
            List<object> temp = new List<object>();
            if(this.ResourceRecords!=null&& this.ResourceRecords.Any())
            {
                temp.AddRange(this.ResourceRecords);
            }
            temp.Add(resourceRecord);
            this.ResourceRecords = temp.ToArray();
        }

        public static RecordSet AddByHostedZoneId(Template template, string resourceName, string hostedZoneId, string dnsName, RecordSetTypeEnum recordSetType)
        {
            var returnValue = new RecordSet(recordSetType) { HostedZoneId = hostedZoneId, Name = dnsName };
            template.Resources.Add(resourceName,returnValue);
            return returnValue;
        }

        public static RecordSet AddByHostedZone(Template template, string name, HostedZone hostedZone, string dnsName, RecordSetTypeEnum recordSetType)
        {
            RecordSet recordSet = AddByHostedZoneName(template, name, hostedZone.Name, dnsName, recordSetType);
            recordSet.Name = dnsName;
            recordSet.DependsOn.Add(hostedZone.LogicalId);
            return recordSet;
        }

        public static RecordSet AddByHostedZoneName(Template template, string name, string hostedZoneName, string dnsName, RecordSetTypeEnum recordSetType)
        {
            var returnValue = new RecordSet(recordSetType) { HostedZoneName = hostedZoneName, Name = dnsName };
            template.Resources.Add(name, returnValue);
            return returnValue;
        }

        protected override bool SupportsTags {
            get { return false; }
        }
    }
}
