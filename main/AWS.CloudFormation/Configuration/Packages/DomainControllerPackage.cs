using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Configuration.Packages
{
    public class DomainControllerPackage : PackageBase<ConfigSet>
    {
        public DomainControllerPackage(DomainController.DomainInfo domainInfo)
        {
            this.DomainInfo = domainInfo;
        }

        private DomainController.DomainInfo DomainInfo { get; }

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            var setup = this.Instance.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("setup");

            var setupFiles = setup.Files;

            ConfigFile file = setupFiles.GetFile("c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1");

            file.Source =
                "https://s3.amazonaws.com/quickstart-reference/microsoft/activedirectory/latest/scripts/ConvertTo-EnterpriseAdmin.ps1";

            var currentConfig = this.Instance.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("installADDS");
            var currentCommand = currentConfig.Commands.AddCommand<Command>("1-install-prereqsz");

            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command = new PowershellFnJoin("-Command \"Install-WindowsFeature AD-Domain-Services, rsat-adds -IncludeAllSubFeature\"");
            

            currentCommand = currentConfig.Commands.AddCommand<Command>("2-install-adds");
            currentCommand.WaitAfterCompletion = "forever";
            currentCommand.Test = $"if \"%USERDNSDOMAIN%\"==\"{this.DomainInfo.DomainDnsName.ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";
            currentCommand.Command = new PowershellFnJoin("-Command \"Install-ADDSForest -DomainName",
                this.DomainInfo.DomainDnsName,
                "-SafeModeAdministratorPassword (convertto-securestring jhkjhsdf338! -asplaintext -force) -DomainMode Win2012 -DomainNetbiosName",
                this.DomainInfo.DomainNetBiosName,
                "-ForestMode Win2012 -Confirm:$false -Force\"");

            currentCommand.Test =
                $"if \"%USERDNSDOMAIN%\"==\"{this.DomainInfo.DomainDnsName.ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";


            currentCommand = currentConfig.Commands.AddCommand<Command>("3-restart-service");
            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command = new PowershellFnJoin("-Command \"Restart-Service NetLogon -EA 0\"");
            currentCommand.Test = $"if \"%USERDNSDOMAIN%\"==\"{this.DomainInfo.DomainDnsName.ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";

            currentCommand = currentConfig.Commands.AddCommand<Command>("4 - create - adminuser");
            currentCommand.WaitAfterCompletion = "0";
            currentCommand.Command = new PowershellFnJoin(FnJoinDelimiter.None, "\"New-ADUser -Name ",
                this.DomainInfo.AdminUserName,
                "-UserPrincipalName ",
                this.DomainInfo.AdminUserName,
                "@",
                this.DomainInfo.DomainDnsName,
                " -AccountPassword (ConvertTo-SecureString ",
                this.DomainInfo.AdminPassword,
                " -AsPlainText -Force) -Enabled $true -PasswordNeverExpires $true\"");
            currentCommand.Test =
                $"if \"%USERDNSDOMAIN%\"==\"{this.DomainInfo.DomainDnsName.ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";

            currentCommand = currentConfig.Commands.AddCommand<Command>("5 - update - adminuser");
            currentCommand.WaitAfterCompletion = "0";
            currentCommand.Command = new PowershellFnJoin("-Command \"c:\\cfn\\scripts\\ConvertTo-EnterpriseAdmin.ps1 -Members",
                this.DomainInfo.AdminUserName,
                "\"");
            currentCommand.Test = $"if \"%USERDNSDOMAIN%\"==\"{this.DomainInfo.DomainDnsName.ToUpper()}\" EXIT /B 1 ELSE EXIT /B 0";

            currentConfig.Commands.AddCommand<Command>(this.WaitCondition);

        }
    }
}
