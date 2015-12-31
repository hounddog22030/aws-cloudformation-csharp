using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Instance.Metadata.Config.Command;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Instance
{
    public class WindowsInstance : Instance
    {
        const string DomainController1PrivateIpAddress = "10.0.4.20";
        const string DomainController2PrivateIpAddress = "10.0.8.20";

        public const string DefaultConfigSetName = "config";
        public const string DefaultConfigSetRenameConfig = "rename";
        public const string DefaultConfigSetJoinConfig = "join";
        public const string DefaultConfigSetRenameConfigRenamePowerShellCommand = "1-execute-powershell-script-RenameComputer";
        public const string DefaultConfigSetRenameConfigSetDnsServers = "a-set-dns-servers";
        public const string DefaultConfigSetRenameConfigJoinDomain = "b-join-domain";

        public WindowsInstance( Template template, 
                                string name, 
                                InstanceTypes instanceType, 
                                string imageId, 
                                Subnet subnet) 
            : base(template, name, instanceType, imageId, OperatingSystem.Windows, true)
        {
            this.Vpc = subnet.Vpc;
            this.Subnet = subnet;
            this.Rename();
        }

        [JsonIgnore]
        public ParameterBase DomainDnsName { get; protected internal set; }
        [JsonIgnore]
        public ParameterBase DomainNetBiosName { get; protected internal set; }


        private void Rename()
        {
            if (OperatingSystem == OperatingSystem.Windows)
            {
                var renameConfig = this.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName).GetConfig(DefaultConfigSetRenameConfig);
                var renameCommandConfig = renameConfig.Commands.AddCommand<PowerShellCommand>(DefaultConfigSetRenameConfigRenamePowerShellCommand);
                renameCommandConfig.Command.AddCommandLine("\"Rename-Computer -NewName ",
                                                            this.Name,
                                                            " -Restart\"");
                renameCommandConfig.WaitAfterCompletion = "forever";
            }
        }

        protected internal virtual void OnAddedToDomain()
        {
        }
    }
}
