using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.Networking;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    
    public class SecurityGroup : ResourceBase
    {
        public SecurityGroup(string description, Vpc vpc) : base(ResourceType.AwsEc2SecurityGroup)
        {
            Vpc = vpc;
            GroupDescription = description;
            this.SecurityGroupIngress = new List<SecurityGroupIngress>();
            this.SecurityGroupEgress = new List<SecurityGroupEgress>();
        }

        [JsonIgnore]
        public string GroupDescription
        {
            get { return (string)this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public Vpc Vpc
        {
            get
            {
                return this.Properties.GetValue<Vpc>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public List<SecurityGroupIngress> SecurityGroupIngress
        {
            get { return this.Properties.GetValue<List<SecurityGroupIngress>>(); }
            private set { this.Properties.SetValue(value); }
        }



        [JsonIgnore]
        public List<SecurityGroupEgress> SecurityGroupEgress
        {
            get { return this.Properties.GetValue<List<SecurityGroupEgress>>(); }
            set { this.Properties.SetValue(value); }
        }

        private static List<string> GetProtocolsListFromFlaggedValue(Protocol protocol)
        {
            List<string> protocols = new List<string>();

            if (protocol == Protocol.All)
            {
                protocols.Add("-1");
            }
            else
            {
                if (protocol.HasFlag(Protocol.Tcp))
                {
                    protocols.Add(Protocol.Tcp.ToString());
                }
                if (protocol.HasFlag(Protocol.Udp))
                {
                    protocols.Add(Protocol.Udp.ToString());
                }
                if (protocol.HasFlag(Protocol.Icmp))
                {
                    protocols.Add(Protocol.Icmp.ToString());
                }
            }
            return protocols;
        }

        private SecurityGroupIngress AddIngress(string cidr, string protocol, int fromPort, int toPort)
        {
            SecurityGroupIngress newIngressEgress;
            newIngressEgress = new SecurityGroupIngress(fromPort, toPort, protocol.ToString().ToLower(), cidr) as SecurityGroupIngress;
            //List<SecurityGroupIngress> temp = new List<SecurityGroupIngress>();
            //temp.AddRange(this.SecurityGroupIngress.Cast<SecurityGroupIngress>());
            //temp.Add(new SecurityGroupIngress(fromPort,toPort,protocol,cidr));
            //SecurityGroupIngress[] myArray = temp.ToArray();
            //this.SecurityGroupIngress = myArray;
            this.SecurityGroupIngress.Add(newIngressEgress);
            return newIngressEgress;
        }

        public List<SecurityGroupIngress> AddIngress(string cidrBlock, Protocol protocol, Ports portBegin, Ports portEnd)
        {
            List<SecurityGroupIngress> returnValue = new List<SecurityGroupIngress>();

            var protocols = GetProtocolsListFromFlaggedValue(protocol);

            foreach (var thisProtocol in protocols)
            {
                returnValue.Add(this.AddIngress(cidrBlock, thisProtocol, (int)portBegin, (int)portEnd));

            }
            return returnValue;
        }

        public List<SecurityGroupIngress> AddIngress(SecurityGroup securityGroup, Protocol protocol, Ports port)
        {
            return this.AddIngress(securityGroup, protocol, port, port);
        }

        public List<SecurityGroupIngress> AddIngress(SecurityGroup securityGroup, Protocol protocol, Ports fromPort, Ports toPort)
        {
            List<SecurityGroupIngress> returnValue = new List<SecurityGroupIngress>();

            var protocols = GetProtocolsListFromFlaggedValue(protocol);

            foreach (var thisProtocol in protocols)
            {
                SecurityGroupIngress newIngressEgress;

                if (((int)toPort) == 0)
                {
                    toPort = fromPort;
                }

                    newIngressEgress = new SecurityGroupIngress((int)fromPort, (int)toPort, thisProtocol.ToLower(), securityGroup) as SecurityGroupIngress;
                    List<SecurityGroupIngress> temp = new List<SecurityGroupIngress>();
                this.SecurityGroupIngress.Add(newIngressEgress);
                    //temp.AddRange(this.SecurityGroupIngress);
                    //temp.Add(newIngressEgress);
                    //this.SecurityGroupIngress = temp.ToArray();

                returnValue.Add(newIngressEgress);

            }


            return returnValue;
        }

        public List<SecurityGroupIngress> AddIngress(PredefinedCidr cidr, Protocol protocol, params Ports[] ports)
        {
            var returnValue = new List<SecurityGroupIngress>();
            var protocols = GetProtocolsListFromFlaggedValue(protocol);
            string cidrAsString;

            switch (cidr)
            {
                    case PredefinedCidr.TheWorld:
                    cidrAsString = Template.CidrIpTheWorld;
                    break;
                default:
                    throw new InvalidEnumArgumentException();

            }
            foreach (var port in ports)
            {
                foreach (var thisProtocol in protocols)
                {
                    returnValue.AddRange(this.AddIngress( cidrAsString, (Protocol) Enum.Parse(typeof(Protocol),thisProtocol), port, port));
                    
                }

            }
            return returnValue;
        }

        public List<SecurityGroupIngress> AddIngress(ICidrBlock cidrObject, Protocol protocol, Ports port)
        {
            return AddIngress(cidrObject, protocol, port, port);
        }

        public List<SecurityGroupIngress> AddIngress(ICidrBlock cidrObject, Protocol protocol, Ports portBegin, Ports portEnd)
        {
            return AddIngress(cidrObject.CidrBlock, protocol, portBegin, portEnd);
        }

        public SecurityGroupIngress AddIngress(ILogicalId logicalId, Protocol protocol, Ports port)
        {
            SecurityGroupIngress newIngressEgress;
            newIngressEgress = new SecurityGroupIngress(logicalId, protocol, port);
            //List<SecurityGroupIngress> temp = new List<SecurityGroupIngress>();
            //temp.AddRange(this.SecurityGroupIngress.Cast<SecurityGroupIngress>());
            //temp.Add(newIngressEgress);
            //SecurityGroupIngress[] myArray = temp.ToArray();
            //this.SecurityGroupIngress = myArray;
            this.SecurityGroupIngress.Add(newIngressEgress);
            return newIngressEgress;
        }

        protected override bool SupportsTags => true;

        public override string LogicalId
        {
            get
            {
                if (string.IsNullOrEmpty(base.LogicalId))
                {
                    this.LogicalId = $"SecurityGroup{this.GroupDescription.Replace(" ", String.Empty)}";
                }
                return base.LogicalId;
            }
            internal set
            {
                base.LogicalId = value;
            }
        }

        public void AddIngress(IPNetwork network, Protocol protocol, Ports port)
        {
            AddIngress(network, protocol, port, port);
        }

        public void AddIngress(IPNetwork network, Protocol protocol, Ports beginPort, Ports endPort)
        {
            AddIngress(network.ToString(), protocol, beginPort,endPort);
        }

        public void AddIngress(string cidr, Protocol protocol, Ports port)
        {
            AddIngress(cidr, protocol, port, port);
        }
    }
}
