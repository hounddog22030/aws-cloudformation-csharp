﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2
{

    public class FnJoin : CloudFormationDictionary
    {
        public FnJoin(string delimiter, params object[] elements)
        {
            var temp = new List<object>();
            temp.Add(delimiter);
            temp.Add(elements);
            this.Add("Fn::Join", temp.ToArray());
        }

    }
    public class DhcpOptions : ResourceBase
    {
        public DhcpOptions(Template template, string name, string domainName, Vpc vpc, params object[] netBiosNameServers) : base(template, name, ResourceType.DhcpOptions)
        {
            this.DomainName = domainName;
            //this.AddDomainNameServer("AmazonProvidedDNS");
            if (netBiosNameServers != null)
            {
                foreach (var netBiosNameServer in netBiosNameServers)
                {
                    this.AddNetBiosNameServers(netBiosNameServer);
                    this.AddDomainNameServer(netBiosNameServer);
                }
            }
            VpcDhcpOptionsAssociation association = new VpcDhcpOptionsAssociation(template,$"vpcDhcpOptionsAssociationFor{name}",this,vpc);
        }


        protected override bool SupportsTags => true;
        //"DomainName" : String,
        //      "DomainNameServers" : [String, ... ],
        //      "NetbiosNameServers" : [String, ... ],
        //      "NetbiosNodeType" : Number,
        //      "NtpServers" : [String, ... ],
        //      "Tags" : [Resource Tag, ... ]
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
        public string[] DomainNameServers
        {
            get
            {
                return this.Properties.GetValue<string[]>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }
        [JsonIgnore]
        public string[] NetbiosNameServers
        {
            get
            {
                return this.Properties.GetValue<string[]>();
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

        //"NetbiosNameServers" : [
        //    {
        //        "Ref" : "AD1PrivateIp"
        //    },
        //    {
        //        "Ref" : "AD2PrivateIp"
        //    }
        //],
        //"NetbiosNodeType"    : "2",

        public void AddDomainNameServer(object domainNameServer)
        {
            var temp = new List<object>();
            IEnumerable<object> current = null;
            if (this.Properties.ContainsKey("DomainNameServers"))
            {
                current = this.Properties["DomainNameServers"] as IEnumerable<object>;
            }
            if (current != null)
            {
                temp.AddRange(current);
            }
            temp.Add(domainNameServer);
            this.Properties["DomainNameServers"] = temp;
        }
        private void AddNetBiosNameServers(object netBiosNameServer)
        {
            var temp = new List<object>();
            IEnumerable<object> current = null;
            if (this.Properties.ContainsKey("NetbiosNameServers"))
            {
                current = this.Properties["NetbiosNameServers"] as IEnumerable<object>;
            }
            if (current != null)
            {
                temp.AddRange(current);
            }
            temp.Add(netBiosNameServer);
            this.Properties["NetbiosNameServers"] = temp;
        }
    }
}
