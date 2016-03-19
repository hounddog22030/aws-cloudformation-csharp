using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.DirectoryService;
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
        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);

            this.InstallRemoteDesktopGateway();
            RecordSet routing = RecordSet.AddByHostedZoneName(
                this.Instance.Template,
                $"RecordSet4{this.Instance.LogicalId}",
                new FnJoin(FnJoinDelimiter.None, new ReferenceProperty(MicrosoftAd.DomainTopLevelParameterName),"."),
                new FnJoin( FnJoinDelimiter.Period, 
                            this.Instance.LogicalId + DateTime.Now.Second,
                            new ReferenceProperty(MicrosoftAd.DomainFqdnParameterName)),
                RecordSet.RecordSetTypeEnum.A);

            var eip = new ElasticIp(this.Instance);
            this.Instance.Template.Resources.Add(eip.LogicalId,eip);

            routing.AddResourceRecord(new ReferenceProperty(eip));

            routing.TTL = "60";

            AddSecurityGroup();
        }
        private void InstallRemoteDesktopGateway()
        {
            MicrosoftAd.AddInstanceToDomain(this.Instance.RenameConfig);
            var installRdsConfig =
                this.Instance.Metadata.Init.ConfigSets.GetConfigSet("RemoteDesktop")
                    .GetConfig("Install");
            var installRdsCommand = installRdsConfig.Commands.AddCommand<Command>("a-install-rds");
            installRdsCommand.Command = new FnJoinPowershellCommand("-Command \"Install-WindowsFeature RDS-Gateway,RSAT-RDS-Gateway\"");

            var configureRdgwPsScript = installRdsConfig.Files.GetFile("c:\\cfn\\scripts\\Configure-RDGW.ps1");

            configureRdgwPsScript.Source =
                "https://s3.amazonaws.com/gtbb/Configure-RDGW.ps1";

            installRdsCommand = installRdsConfig.Commands.AddCommand<Command>("b-configure-rdgw");
            installRdsCommand.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None,
                                            " -ExecutionPolicy RemoteSigned ",
                                            " C:\\cfn\\scripts\\Configure-RDGW.ps1 -ServerFQDN ",
                                            this.Instance.LogicalId,
                                            ".",
                                            new ReferenceProperty(MicrosoftAd.DomainFqdnParameterName),
                                            " -DomainNetBiosName ",
                                            new ReferenceProperty(MicrosoftAd.DomainNetBiosNameParameterName),
                                            " -GroupName 'Domain Users'");
            installRdsCommand.Test = "IF EXIST c:/rdp.cer EXIT 1";
        }
        private void AddSecurityGroup()
        {
            var launchConfigurationAsInstance = this.Instance as Instance;
            var rdgwSecurityGroup = new SecurityGroup("Remote Desktop Security Group", launchConfigurationAsInstance.Subnet.Vpc);
            this.Instance.Template.Resources.Add($"SecurityGroup4{this.Instance.LogicalId}", rdgwSecurityGroup);

            rdgwSecurityGroup.AddIngress(PredefinedCidr.LocalGateway, Protocol.Tcp, Ports.RemoteDesktopProtocol, Ports.Ssl, Ports.Http);
            rdgwSecurityGroup.AddIngress(PredefinedCidr.LocalGateway, Protocol.Udp, Ports.RdpAdmin);
            rdgwSecurityGroup.AddIngress(PredefinedCidr.LocalGateway, Protocol.Icmp, Ports.All);

            launchConfigurationAsInstance.SecurityGroupIds.Add(new ReferenceProperty(rdgwSecurityGroup));
        }
    }
}
