using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Serialization;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public enum Ports : int
    {
        ActiveDirectoryManagement = 9389,
        ActiveDirectoryManagement2 = 464,
        RemoteDesktopProtocol = 3389,
        Ssh = 22,
        Ssl = 443,
        RdpAdmin = 3391,
        All = -1,
        Min = 1,
        Max = 65535,
        WsManagementPowerShell = 5985,
        Ntp = 123,
        WinsManager = 135,
        NetBios = 138,
        Smb = 445,
        DnsBegin = 49152,
        DnsEnd = Max,
        Ldap = 389,
        Ldaps = 636,
        Ldap2Begin = 3268,
        Ldap2End = 3269,
        DnsQuery = 53,
        KerberosKeyDistribution = 88,
        DnsLlmnr = 5355,
        NetBt = 137,
        NetBiosNameServices = 139,
        ActiveDirectoryFileReplication = 5722,
        Http = 80,
        MsSqlServer = 1433,
        BuildController = 9191,
        TeamFoundationServerHttp = 8080,
        TeamFoundationServerBuild = 9191,
        Ping = 1,
        MySql = 3306,
        EphemeralRpcBegin = 1024,
        EphemeralRpcEnd = 65535
    }

    [Flags]
    public enum Protocol
    {
        Tcp = 1,
        Udp = 2,
        Icmp = 4,
        All = Tcp + Udp + Icmp
    }

    public enum PredefinedCidr
    {
        TheWorld = 1
    }

    public abstract class SecurityGroupIngressEgressBase
    {
        protected SecurityGroupIngressEgressBase()
        {
            
        }


        public SecurityGroupIngressEgressBase(int fromPort, int toPort, string protocol, object cidr) : this(fromPort,toPort,protocol)
        {
            CidrIp = cidr;
        }

        protected SecurityGroupIngressEgressBase(int fromPort, int toPort, string protocol)
        {
            FromPort = fromPort;
            ToPort = toPort;
            IpProtocol = protocol;
        }

        protected SecurityGroupIngressEgressBase(ILogicalId logicalId, Protocol protocol, Ports fromPort, Ports toPort) : this((int)fromPort, (int)toPort,protocol.ToString())
        {
            var fnJoinDictionary = new CloudFormationDictionary();
            var fnGetAttDictionary = new CloudFormationDictionary();
            var privateIp = new string[] { logicalId.LogicalId, "PrivateIp" };
            fnGetAttDictionary.Add("Fn::GetAtt", privateIp);
            fnJoinDictionary.SetFnJoin(fnGetAttDictionary,"/32");
            CidrIp = fnJoinDictionary;
        }




        public object CidrIp { get; private set; }
        public int FromPort { get; private set; }
        public string IpProtocol { get; private set; }
        public int ToPort { get; private set; }

        
    }

    [JsonConverter(typeof(EnumConverter))]
    public enum FnGetAttAttribute
    {
        [EnumMember(Value = "PrivateDnsName")]
        AwsEc2InstancePrivateDnsName,
        [EnumMember(Value = "PrivateIp")]
        AwsEc2InstancePrivateIp,
        [EnumMember(Value = "Endpoint.Address")]
        AwsRdsDbInstanceEndpointAddress,
        [EnumMember(Value = "DNSName")]
        AwsElasticLoadBalancingLoadBalancer,
        [EnumMember(Value = "DnsIpAddresses")]
        AwsDirectoryServiceSimpleAdDnsIpAddresses,
        [EnumMember(Value = "Alias")]
        AwsDirectoryServiceSimpleAdAlias

        //DnsIpAddresses

        //AWS::ElasticLoadBalancing::LoadBalancer

        //
    }

    public class FnGetAtt : CloudFormationDictionary
    {
        public FnGetAtt(object resource, string attribute) : this((object)resource, (object)attribute)
        {
        }
        public FnGetAtt(ILogicalId resource, FnGetAttAttribute attribute) : this((object)resource.LogicalId, (object)attribute)
        {
        }
        public FnGetAtt(ILogicalId resource, string attribute) : this((object)resource.LogicalId, (object)attribute)
        {
        }

        public FnGetAtt(FnGetAtt resource, FnGetAttAttribute attribute) : this((object)resource,(object)attribute)
        {
        }

        private FnGetAtt(object resource, object attribute)
        {
            var info = new object[] { resource, attribute };
            this.Add("Fn::GetAtt", info);
        }
    }
}
