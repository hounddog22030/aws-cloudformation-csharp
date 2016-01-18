using System;
using System.Collections.Generic;
using System.ComponentModel;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    
    public class SecurityGroup : ResourceBase
    {
        public SecurityGroup(Template template, string name, string description, Vpc vpc)
            : base(template, "AWS::EC2::SecurityGroup", name, true)
        {
            template.AddResource(this);
            VpcId = vpc;
            GroupDescription = description;
            this.SecurityGroupIngress = new List<SecurityGroupIngress>();
            this.SecurityGroupEgress = new List<SecurityGroupEgress>();
        }

        [JsonIgnore]
        public string GroupDescription
        {
            get { return (string)this.Properties.GetValue(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public object VpcId
        {
            get { return this.Properties.GetValue(); }
            set { this.Properties.SetValue(value); }
        }


        [JsonIgnore]
        public List<SecurityGroupIngress> SecurityGroupIngress
        {
            get { return this.Properties.GetValue() as List<SecurityGroupIngress>; }
            set { /*this.Properties.SetValue(value);*/ }
        }


        [JsonIgnore]
        public List<SecurityGroupEgress> SecurityGroupEgress
        {
            get { return this.Properties.GetValue() as List<SecurityGroupEgress>; }
            set { /*this.Properties.SetValue(value);*/ }
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

        private T AddIngressEgress<T>(string cidr, string protocol, int fromPort, int toPort) where T : SecurityGroupIngressEgressBase
        {

            T newIngressEgress;
            if (typeof(T) == typeof(SecurityGroupIngress))
            {
                newIngressEgress = new SecurityGroupIngress(fromPort, toPort, protocol.ToString().ToLower(), cidr) as T;
                this.SecurityGroupIngress.Add(newIngressEgress as SecurityGroupIngress);
            }
            else
            {
                newIngressEgress = new SecurityGroupEgress(fromPort, toPort, protocol.ToString().ToLower(), cidr) as T;
                this.SecurityGroupEgress.Add(newIngressEgress as SecurityGroupEgress);

            }
            return newIngressEgress;
        }

        private List<T> AddIngressEgress<T>(string cidrBlock, Protocol protocol, Ports portBegin, Ports portEnd) where T : SecurityGroupIngressEgressBase, new()
        {
            List<T> returnValue = new List<T>();

            var protocols = GetProtocolsListFromFlaggedValue(protocol);

            foreach (var thisProtocol in protocols)
            {
                returnValue.Add(this.AddIngressEgress<T>(cidrBlock, thisProtocol, (int)portBegin, (int)portEnd));

            }
            return returnValue;
        }

        public List<T> AddIngressEgress<T>(SecurityGroup securityGroup, Protocol protocol, Ports port) where T : SecurityGroupIngressEgressBase, new()
        {
            return this.AddIngressEgress<T>(securityGroup, protocol, port, port);
        }

        public List<T> AddIngressEgress<T>(SecurityGroup securityGroup, Protocol protocol, Ports fromPort, Ports toPort) where T : SecurityGroupIngressEgressBase
        {
            List<T> returnValue = new List<T>();

            var protocols = GetProtocolsListFromFlaggedValue(protocol);

            foreach (var thisProtocol in protocols)
            {
                T newIngressEgress;

                if (((int)toPort) == 0)
                {
                    toPort = fromPort;
                }

                if (typeof(T) == typeof(SecurityGroupIngress))
                {
                    newIngressEgress = new SecurityGroupIngress((int)fromPort, (int)toPort, thisProtocol.ToLower(), securityGroup) as T;
                    this.SecurityGroupIngress.Add(newIngressEgress as SecurityGroupIngress);
                }
                else
                {
                    throw new InvalidOperationException();
                }

                returnValue.Add(newIngressEgress);

            }


            return returnValue;
        }

        public List<T> AddIngressEgress<T>(PredefinedCidr cidr, Protocol protocol, params Ports[] ports) where T: SecurityGroupIngressEgressBase, new()
        {
            var returnValue = new List<T>();
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
                    returnValue.AddRange(this.AddIngressEgress<T>( cidrAsString, (Protocol) Enum.Parse(typeof(Protocol),thisProtocol), port, port));
                    
                }

            }
            return returnValue;
        }

        public List<T> AddIngressEgress<T>(ICidrBlock cidrObject, Protocol protocol, Ports port) where T : SecurityGroupIngressEgressBase, new()
        {
            return AddIngressEgress<T>(cidrObject, protocol, port, port);
        }

        public List<T> AddIngressEgress<T>(ICidrBlock cidrObject, Protocol protocol, Ports portBegin, Ports portEnd) where T : SecurityGroupIngressEgressBase, new()
        {
            return AddIngressEgress<T>(cidrObject.CidrBlock, protocol, portBegin, portEnd);
        }

    }
}
