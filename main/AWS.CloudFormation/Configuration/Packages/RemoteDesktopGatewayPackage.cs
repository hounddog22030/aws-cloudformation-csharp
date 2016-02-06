using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Route53;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Configuration.Packages
{
    public class RemoteDesktopGatewayPackage : PackageBase<ConfigSet>
    {
        public RemoteDesktopGatewayPackage(DomainInfo domainInfo)
        {
            DomainInfo = domainInfo;

        }

        private DomainInfo DomainInfo { get; }

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);

            var domainParts = this.DomainInfo.DomainDnsName.Split('.');
            var tldDomain = $"{domainParts[domainParts.Length - 2]}.{domainParts[domainParts.Length - 1]}.";


            this.InstallRemoteDesktopGateway();
            RecordSet routing = RecordSet.AddByHostedZoneName(
                this.Instance.Template,
                $"RecordSet4{this.Instance.LogicalId}",
                tldDomain,
                $"{this.Instance.LogicalId}.{this.DomainInfo.DomainDnsName}.",
                RecordSet.RecordSetTypeEnum.A);

            var eip = new ElasticIp(this.Instance);
            this.Instance.Template.Resources.Add(eip.LogicalId,eip);

            routing.AddResourceRecord(new ReferenceProperty(eip));

            routing.TTL = "60";

            AddSecurityGroup();
        }
        private void InstallRemoteDesktopGateway()
        {
            var installRdsCommand = this.Config.Commands.AddCommand<Command>("InstallRemoteDesktopGatewayServices");

            installRdsCommand.Command = new PowershellFnJoin("-Command \"Install-WindowsFeature RDS-Gateway,RSAT-RDS-Gateway\"");

            ExecuteRemotePowershellScript.AddExecuteRemotePowershellScript(this.Config, new Uri("https://s3.amazonaws.com/gtbb/Configure-RDGW.ps1"), null, TimeSpan.MinValue );

            installRdsCommand = this.Config.Commands.AddCommand<Command>("ConfigureRemoteDesktopGatewayServices");
            installRdsCommand.Command = new PowershellFnJoin(FnJoinDelimiter.None,
                                            " -ExecutionPolicy RemoteSigned ",
                                            " C:\\cfn\\scripts\\Configure-RDGW.ps1 -ServerFQDN ",
                                            this.Instance.LogicalId,
                                            ".",
                                            this.DomainInfo.DomainDnsName,
                                            " -DomainNetBiosName ",
                                            this.DomainInfo.DomainNetBiosName,
                                            " -GroupName 'domain admins'");
        }
        private void AddSecurityGroup()
        {
            var launchConfigurationAsInstance = this.Instance as Instance;
            var rdgwSecurityGroup = new SecurityGroup("Remote Desktop Security Group", launchConfigurationAsInstance.Subnet.Vpc);
            this.Instance.Template.Resources.Add($"SecurityGroup4{this.Instance.LogicalId}", rdgwSecurityGroup);


            rdgwSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol, Ports.Ssl);
            rdgwSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.Http);
            rdgwSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Udp, Ports.RdpAdmin);
            rdgwSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Icmp, Ports.All);

            this.Instance.AddSecurityGroup(rdgwSecurityGroup);
        }
    }
}
