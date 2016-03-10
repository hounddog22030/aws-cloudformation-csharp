using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;

namespace AWS.CloudFormation.Configuration.Packages
{
    public class VisualStudioPowershellTools : PackageBase<ConfigSet>
    {
        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            const string powerShellToolsVsixLocalPath = "c:/cfn/scripts/PowerShellTools.14.0.vsix";
            var powerShellToolsVsix = this.Config.Files.GetFile(powerShellToolsVsixLocalPath);
            powerShellToolsVsix.Source = "https://visualstudiogallery.msdn.microsoft.com/c9eb3ba8-0c59-4944-9a62-6eee37294597/file/199313/2/PowerShellTools.14.0.vsix";
            var installPowerSehllToolsVsix = this.Config.Commands.AddCommand<Command>("PowerShellTools140vsix");
            installPowerSehllToolsVsix.Command = $"\"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\IDE\\VSIXInstaller.exe\" /q {powerShellToolsVsixLocalPath}";
            installPowerSehllToolsVsix.WaitAfterCompletion = 0.ToString();
        }
    }
}
