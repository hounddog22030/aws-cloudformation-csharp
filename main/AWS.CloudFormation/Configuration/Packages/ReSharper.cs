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
    public class ReSharper : PackageBase<ConfigSet>
    {

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            this.Config.Files.Add("c:/cfn/scripts/ReSharperUltimate.10.0.2.exe")
                .Add("source", "https://download.jetbrains.com/resharper/JetBrains.ReSharperUltimate.10.0.2.exe");
            var command = this.Config.Commands.AddCommand<Command>("InstallResharper");
            command.Command = "c:/cfn/scripts/ReSharperUltimate.10.0.2.exe /VsVersion=14 /SpecificProductNames=ReSharper /Silent=True /PerMachine=True";
            command.Test = "IF EXISTS \"C:\\Program Files (x86)\\JetBrains\\Installations\\ReSharperPlatformVs14\\CsLex.exe\" EXIT /B 1";
            command.WaitAfterCompletion = 0.ToString();
        }
    }
}
