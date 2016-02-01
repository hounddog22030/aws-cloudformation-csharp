using System;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Resource.Route53;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Resource.EC2.Instancing
{
    public class RemoteDesktopGateway : WindowsInstance
    {

        private const string InstallRds = "installRDS";

        public RemoteDesktopGateway(Template template,
            string name,
            InstanceTypes instanceType,
            string imageId,
            Subnet subnet) : 
            base(template, name, instanceType, imageId, subnet, true)
        {
            this.Subnet = subnet;
            this.AddSecurityGroup();
            this.AddElasticIp();
        }

        private void InstallRemoteDesktopGateway()
        {
            var installRdsConfig =
                this.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName)
                    .GetConfig(InstallRds);
            var installRdsCommand = installRdsConfig.Commands.AddCommand<Command>("a-install-rds");
            installRdsCommand.Command = new PowershellFnJoin("-Command \"Install-WindowsFeature RDS-Gateway,RSAT-RDS-Gateway\"");

            var configureRdgwPsScript = installRdsConfig.Files.GetFile("c:\\cfn\\scripts\\Configure-RDGW.ps1");

            configureRdgwPsScript.Source =
                "https://s3.amazonaws.com/gtbb/Configure-RDGW.ps1";

            installRdsCommand = installRdsConfig.Commands.AddCommand<Command>("b-configure-rdgw");
            installRdsCommand.Command = new PowershellFnJoin(
                                            "-ExecutionPolicy RemoteSigned",
                                            "C:\\cfn\\scripts\\Configure-RDGW.ps1 -ServerFQDN " + new ReferenceProperty(this) + ".",
                                            new ReferenceProperty(this.DomainDnsName),
                                            "-DomainNetBiosName",
                                            new ReferenceProperty(this.DomainNetBiosName),
                                            "-GroupName 'domain admins'");
        }


        private void AddSecurityGroup()
        {
            var rdgwSecurityGroup = new SecurityGroup(Template, $"{this.LogicalId}SecurityGroup", "Remote Desktop Security Group", this.Subnet.Vpc);

            rdgwSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol, Ports.Ssl);
            rdgwSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.Http);
            rdgwSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Udp, Ports.RdpAdmin);
            rdgwSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Icmp, Ports.All);

            this.AddSecurityGroup(rdgwSecurityGroup);
        }


        protected internal override void OnAddedToDomain(string domainName)
        {
            base.OnAddedToDomain(domainName);

            var domainParts = this.DomainDnsName.Default.ToString().Split('.');
            var tldDomain = $"{domainParts[domainParts.Length - 2]}.{domainParts[domainParts.Length - 1]}.";


            this.InstallRemoteDesktopGateway();
            RecordSet routing = RecordSet.AddByHostedZoneName(
                this.Template, 
                this.LogicalId + "Record",
                tldDomain,
                $"{this.LogicalId}.{this.DomainDnsName.Default}.",
                RecordSet.RecordSetTypeEnum.A);
            routing.AddResourceRecord(new ReferenceProperty(this.ElasticIp));

            routing.TTL = "60";

        }
    }
}
