using AWS.CloudFormation.Instance;
using AWS.CloudFormation.Instance.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
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
            base(template, name, instanceType, imageId, subnet )
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
            var installRdsCommand = installRdsConfig.Commands.AddCommand<PowerShellCommand>("a-install-rds");
            installRdsCommand.Command.AddCommandLine("-Command \"Install-WindowsFeature RDS-Gateway,RSAT-RDS-Gateway\"");
            //"c:\\cfn\\scripts\\Configure-RDGW.ps1"     : {
            //    "source" : "https://s3.amazonaws.com/quickstart-reference/microsoft/activedirectory/latest/scripts/Configure-RDGW.ps1"
            //                }
            var configureRdgwPsScript = installRdsConfig.Files.GetFile("c:\\cfn\\scripts\\Configure-RDGW.ps1");
            configureRdgwPsScript.Source =
                "https://s3.amazonaws.com/quickstart-reference/microsoft/activedirectory/latest/scripts/Configure-RDGW.ps1";

            installRdsCommand = installRdsConfig.Commands.AddCommand<PowerShellCommand>("b-configure-rdgw");
            installRdsCommand.Command.AddCommandLine(
                                            "-ExecutionPolicy RemoteSigned",
                                            " C:\\cfn\\scripts\\Configure-RDGW.ps1 -ServerFQDN " + this.Name + ".",
                                            this.DomainDnsName,
                                            " -DomainNetBiosName ",
                                            this.DomainNetBiosName,
                                            " -GroupName 'domain admins'" );
        }


        private void AddSecurityGroup()
        {
            var rdgwSecurityGroup = this.Template.GetSecurityGroup("RDGWSecurityGroup", this.Subnet.Vpc, "Remote Desktop Security Group");

            rdgwSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol, Ports.Ssl);
            rdgwSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Udp, Ports.RdpAdmin);
            rdgwSecurityGroup.AddIngressEgress<SecurityGroupIngress>(PredefinedCidr.TheWorld, Protocol.Icmp, Ports.All);

            this.SecurityGroups.Add(rdgwSecurityGroup);
        }


        protected internal override void OnAddedToDomain()
        {
            base.OnAddedToDomain();
            this.InstallRemoteDesktopGateway();
        }
    }
}
