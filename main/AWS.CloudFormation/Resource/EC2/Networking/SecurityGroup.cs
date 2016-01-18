using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.Networking;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    
    public class SecurityGroup : ResourceBase
    {
        public SecurityGroup(Template template, string name, string description, Vpc vpc)
            : base(template, "AWS::EC2::SecurityGroup", name, true)
        {
            Vpc = vpc;
            GroupDescription = description;
            this.SecurityGroupIngress = new SecurityGroupIngress[0]; 
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
        public SecurityGroupIngress[] SecurityGroupIngress
        {
            get { return this.Properties.GetValue<SecurityGroupIngress[]>(); }
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
            List<SecurityGroupIngress> temp = new List<SecurityGroupIngress>();
            temp.AddRange(this.SecurityGroupIngress.Cast<SecurityGroupIngress>());
            temp.Add(new SecurityGroupIngress(fromPort,toPort,protocol,cidr));
            SecurityGroupIngress[] myArray = temp.ToArray();
            this.SecurityGroupIngress = myArray;
            return newIngressEgress;
        }

        private List<SecurityGroupIngress> AddIngress(string cidrBlock, Protocol protocol, Ports portBegin, Ports portEnd)
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
                    temp.AddRange(this.SecurityGroupIngress);
                    temp.Add(newIngressEgress);
                    this.SecurityGroupIngress = temp.ToArray();

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
                    cidrAsString = Template.CIDR_IP_THE_WORLD;
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

    }
}
