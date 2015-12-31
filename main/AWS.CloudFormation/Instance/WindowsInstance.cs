using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Instance.MetaData.Config.Command;
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
                                string keyName, 
                                Subnet subnet,
                                string domainToJoin) 
            : base(template, name, instanceType, imageId, keyName, OperatingSystem.Windows, true)
        {
            this.Vpc = subnet.Vpc;
            this.Subnet = subnet;
            this.Rename();
            Domain = domainToJoin;
            if (!string.IsNullOrEmpty(domainToJoin))
            {
                this.SetDnsServers();
                this.AddToDomain();
            }

        }

        private void AddToDomain()
        {

            //"powershell.exe -Command \"",
            //                                "Add-Computer -DomainName ",
            //                                {
            //    "Ref" : "DomainDNSName"
            //                                },
            //                                " -Credential ",
            //                                "(New-Object System.Management.Automation.PSCredential('",
            //                                {
            //    "Ref" : "DomainNetBIOSName"
            //                                },
            //                                "\\",
            //                                {
            //    "Ref" : "DomainAdminUser"
            //                                },
            //                                "',",
            //                                "(ConvertTo-SecureString ",
            //                                {
            //    "Ref" : "DomainAdminPassword"
            //                                },
            //                                " -AsPlainText -Force))) ",
            //                                "-Restart\""

            var joinCommandConfig =
                this.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName)
                    .GetConfig(DefaultConfigSetJoinConfig);
            var joinCommand =
                joinCommandConfig.Commands.AddCommand<PowerShellCommand>(DefaultConfigSetRenameConfigJoinDomain);
            joinCommand.Command.AddCommandLine( "-Command \"",
                                                " Add-Computer -DomainName ",
                                                this.Domain,
                                                " -Credential ",
                                                "(New-Object System.Management.Automation.PSCredential('",
                                                "gtbb\\johnny",
                                                "',",
                                                "(ConvertTo-SecureString ",
                                                "kasdfiajs!!9",
                                                " -AsPlainText -Force))) ",
                                                "-Restart\"");
            joinCommand.WaitAfterCompletion = "forever";

            var domainController = Template.Resources.Single(r=>r.Value is DomainController).Value as DomainController;

            //this.AddDependsOn(domainController,new TimeSpan(0,40,0));
        }

        [JsonIgnore]
        public string Domain { get; }


        private void SetDnsServers()
        {
            if (OperatingSystem == OperatingSystem.Windows)
            {
                var renameConfig = this.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName).GetConfig(DefaultConfigSetJoinConfig);
                var renameCommandConfig = renameConfig.Commands.AddCommand<PowerShellCommand>(DefaultConfigSetRenameConfigSetDnsServers);
                renameCommandConfig.Command.AddCommandLine("-Command \"Get-NetAdapter | Set-DnsClientServerAddress -ServerAddresses ",
                                                            DomainController1PrivateIpAddress,
                                                            ",",
                                                            DomainController2PrivateIpAddress,
                                                            "\"");
                renameCommandConfig.WaitAfterCompletion = 30.ToString();
            }
        }
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
    }
}
