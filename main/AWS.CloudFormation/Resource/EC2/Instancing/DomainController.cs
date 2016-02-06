using System;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Resource.Wait;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Instancing
{
    //public class DomainController : Instance
    //{

    public class DomainInfo
    {
        public DomainInfo(string domainDnsName, string adminUserName, ReferenceProperty adminPassword)
        {
            DomainDnsName = domainDnsName;
            DomainNetBiosName = domainDnsName.Split('.')[domainDnsName.Split('.').Length-3];
            AdminUserName = adminUserName;
            AdminPassword = adminPassword;

        }

        public string DomainDnsName { get; }
        public string DomainNetBiosName { get; }
        public string AdminUserName { get; }
        public ReferenceProperty AdminPassword { get; }
    }
}