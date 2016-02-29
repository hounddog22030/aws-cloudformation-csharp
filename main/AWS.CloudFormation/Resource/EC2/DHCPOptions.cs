using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.DirectoryService;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2
{

    public class DhcpOptions : ResourceBase
    {
        public DhcpOptions(object domainName, Vpc vpc, FnJoin dnsServers, FnJoin netBiosNameServers) 
            : base(ResourceType.DhcpOptions)
        {
            if (domainName != null)
            {
                this.DomainName = domainName;
            }
            this.Vpc = vpc;
            this.DomainNameServers = dnsServers;
            this.NetbiosNameServers = netBiosNameServers;
        }

        public DhcpOptions(Vpc vpc, MicrosoftAd simpleAd) : base(ResourceType.DhcpOptions)
        {
            this.Vpc = vpc;
            this.DomainNameServers = new FnGetAtt(simpleAd, FnGetAttAttribute.AwsDirectoryServiceSimpleAdDnsIpAddresses);
            this.NetbiosNameServers = new FnGetAtt(simpleAd, FnGetAttAttribute.AwsDirectoryServiceSimpleAdDnsIpAddresses);
            this.LogicalId = $"Dhcp{Vpc}{simpleAd.Name}";
        }
        public DhcpOptions(Vpc vpc, string[] dnsServers, string[] netBiosServers) : base(ResourceType.DhcpOptions)
        {
            this.Vpc = vpc;
            this.DependsOn.Add(vpc.LogicalId);
            this.DomainNameServers = dnsServers;
            this.NetbiosNameServers = netBiosServers;
        }

        public DhcpOptions(string domainName, FnJoin dnsServers, FnJoin netBiosNameServers) : base(ResourceType.DhcpOptions)
        {
            var logicalId = string.Empty;
            if (domainName != null)
            {
                this.DomainName = domainName;
                logicalId = ResourceBase.NormalizeLogicalId(domainName);
            }

            this.DomainNameServers = dnsServers;
            this.NetbiosNameServers = netBiosNameServers;
            logicalId += ResourceBase.NormalizeLogicalId(this.DomainNameServers.ToString());
            logicalId += ResourceBase.NormalizeLogicalId(this.NetbiosNameServers.ToString());
            this.LogicalId = logicalId;
            this.NetbiosNodeType = "2";


        }

        [JsonIgnore]
        public Vpc Vpc { get;  }

        protected override void OnTemplateSet(Template template)
        {
            base.OnTemplateSet(template);
            if (this.Vpc != null)
            {
                VpcDhcpOptionsAssociation association = new VpcDhcpOptionsAssociation(this, this.Vpc);
                template.Resources.Add($"VpcDhcpOptionsAssociation4{this.LogicalId}", association);
            }
        }


        protected override bool SupportsTags => true;

        [JsonIgnore]
        public object DomainName
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public object DomainNameServers
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }
        [JsonIgnore]
        public object NetbiosNameServers
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public string NetbiosNodeType
        {
            get
            {
                return this.Properties.GetValue<string>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }
    }
}
