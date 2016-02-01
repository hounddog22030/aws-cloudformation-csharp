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
    public class DomainController : WindowsInstance
    {

        public const string DefaultConfigSetRenameConfigSetDnsServers = "a-set-dns-servers";

        public class DomainInfo
        {
            public DomainInfo(string domainDnsName, string adminUserName, string adminPassword)
            {
                DomainDnsName = domainDnsName;
                DomainNetBiosName = domainDnsName.Split('.')[0];
                AdminUserName = adminUserName;
                AdminPassword = adminPassword;

            }

            public string DomainDnsName { get; }
            public string DomainNetBiosName { get; }
            public string AdminUserName { get; }
            public string AdminPassword { get; }
        }

        public const string ParameterNameDomainAdminPassword = "DomainAdminPassword";
        public const string ParameterNameDomainDnsName = "DomainDNSName";
        public const string ParameterNameDomainNetBiosName = "DomainNetBIOSName";
        public const string ParameterNameDomainAdminUser = "DomainAdminUser";





        public DomainController(Template template, string name, InstanceTypes instanceType, string imageId,
            Subnet subnet, DomainInfo domainInfo)
            : base(template, name, instanceType, imageId, subnet, true)
        {
            Template.AddParameter(new ParameterBase(DomainController.ParameterNameDomainDnsName, "String", domainInfo.DomainDnsName));

            this.DomainAdminPassword = new ParameterBase(DomainController.ParameterNameDomainAdminPassword, "String",
                domainInfo.AdminPassword);
            Template.AddParameter(DomainAdminPassword);

            template.AddParameter(new ParameterBase(DomainController.ParameterNameDomainNetBiosName, "String",
                domainInfo.DomainNetBiosName));

            this.DomainAdminUser = new ParameterBase(DomainController.ParameterNameDomainAdminUser, "String",
                domainInfo.AdminUserName);
            template.AddParameter(this.DomainAdminUser);

        }

        [JsonIgnore]
        public ParameterBase DomainAdminUser { get; }

        [JsonIgnore]
        public ParameterBase DomainAdminPassword { get; set; }

        private WaitCondition _domainAvailable = null;

        private WaitCondition DomainAvailable
        {
            get
            {
                if (_domainAvailable == null)
                {
                    _domainAvailable = new WaitCondition(this.Template, $"{this.LogicalId}DomainAvailableWaitCondition", new TimeSpan(4,0,0));
                }
                return _domainAvailable;
            }
        }



        private SecurityGroup CreateDomainMemberSecurityGroup()
        {
            SecurityGroup domainMemberSg = new SecurityGroup(this.Template, "DomainMemberSG", "For All Domain Members", this.Subnet.Vpc);
            domainMemberSg.GroupDescription = "Domain Member Security Group";
            return domainMemberSg;
        }


        [JsonIgnore]
        public SecurityGroup DomainMemberSecurityGroup { get; }


    }
}
