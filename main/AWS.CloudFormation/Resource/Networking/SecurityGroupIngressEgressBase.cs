using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.CloudFormation.Resource.Networking
{
    public enum Ports : int
    {
        ActiveDirectoryManagement = 9389,
        ActiveDirectoryManagement2 = 464,
        Rdp = 3389,
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
        Http = 80
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


        public SecurityGroupIngressEgressBase(int fromPort, int toPort, string protocol, string cidr) : this(fromPort,toPort,protocol)
        {
            CidrIp = cidr;
        }

        protected SecurityGroupIngressEgressBase(int fromPort, int toPort, string protocol)
        {
            FromPort = fromPort;
            ToPort = toPort;
            IpProtocol = protocol;
        }


        public string CidrIp { get; private set; }
        public int FromPort { get; private set; }
        public string IpProtocol { get; private set; }
        public int ToPort { get; private set; }

        
    }
}
