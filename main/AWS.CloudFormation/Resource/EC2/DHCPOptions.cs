﻿using System;
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
            throw new NotImplementedException();
            //if (domainName != null)
            //{
            //    this.DomainName = domainName;
            //}
            //this.Vpc = vpc;
            //this.AddDomainNameServer(dnsServers);
            //this.AddNetBiosNameServers(netBiosNameServers);
        }

        public DhcpOptions(Vpc vpc, SimpleAd simpleAd) : base(ResourceType.DhcpOptions)
        {
            this.Vpc = vpc;
            this.DomainNameServers = new FnGetAtt(simpleAd, FnGetAttAttribute.AwsDirectoryServiceSimpleAdDnsIpAddresses);
            this.NetbiosNameServers = new FnGetAtt(simpleAd, FnGetAttAttribute.AwsDirectoryServiceSimpleAdDnsIpAddresses);
        }
        public DhcpOptions(Vpc vpc, string[] dnsServers, string[] netBiosServers) : base(ResourceType.DhcpOptions)
        {
            this.Vpc = vpc;
            this.DependsOn.Add(vpc.LogicalId);
            this.DomainNameServers = dnsServers;
            this.NetbiosNameServers = netBiosServers;
        }

        [JsonIgnore]
        public Vpc Vpc { get;  }

        protected override void OnTemplateSet(Template template)
        {
            base.OnTemplateSet(template);
            VpcDhcpOptionsAssociation association = new VpcDhcpOptionsAssociation(this, this.Vpc);
            template.Resources.Add($"VpcDhcpOptionsAssociation4{this.LogicalId}", association);
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
