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
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Configuration.Packages
{
    public class WindowsShare : PackageBase<ConfigSet>
    {
        public WindowsShare(string path, string shareName, string cidrForSmbAccess, params object[] accounts)
        {
            this.Path = path;
            this.ShareName = shareName;
            this.Accounts = accounts;
            this.CidrForSmbAccess = cidrForSmbAccess;
        }

        public string CidrForSmbAccess { get; }

        public object[] Accounts { get; }

        public string ShareName { get; }

        public string Path { get; }

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            var command = this.Config.Commands.AddCommand<Command>("CreateWindowsShare");
            const string CreateWindowsShare = "c:/cfn/scripts/CreateWindowsShare.ps1";
            var script = this.Config.Files.GetFile(CreateWindowsShare);
            script.Source = "https://s3.amazonaws.com/gtbb/CreateWindowsShare.ps1";
            command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.Space,
                CreateWindowsShare,
                this.Path,
                this.ShareName,
                "@(",
                new FnJoin(FnJoinDelimiter.Comma,
                    this.Accounts),
                ")");

            SecurityGroup securityGroupSmbAccess = new SecurityGroup("Security Group For SMB Access", configuration.Subnet.Vpc);
            string theWorld = "0.0.0.0/0";
            securityGroupSmbAccess.AddIngress(theWorld, Protocol.Tcp, Ports.Min, Ports.Max);
            securityGroupSmbAccess.AddIngress(theWorld, Protocol.Udp, Ports.Min, Ports.Max);
            configuration.Template.Resources.Add(securityGroupSmbAccess.LogicalId, securityGroupSmbAccess);
            Instance launchConfigurationAsInstance = configuration as Instance;
            launchConfigurationAsInstance.SecurityGroupIds.Add(new ReferenceProperty(securityGroupSmbAccess));
        }
    }
}
