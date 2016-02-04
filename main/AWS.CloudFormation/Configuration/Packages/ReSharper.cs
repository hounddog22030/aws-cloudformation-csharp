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
            // https://download.jetbrains.com/resharper/JetBrains.ReSharperUltimate.10.0.2.exe /VsVersion=14 /SpecificProductNames=ReSharper /Silent=True /PerMachine=True 
            // C:\Users\Administrator\AppData\Local\Microsoft\Windows\INetCache\IE\NCO3L2C4\JetBrains.ReSharperUltimate.10.0.2.exe /VsVersion=14 /SpecificProductNames=ReSharper /Silent=True /PerMachine=True 

            this.Config.Files.Add("c:/cfn/scripts/ReSharperUltimate.10.0.2.exe")
                .Add("source", "https://download.jetbrains.com/resharper/JetBrains.ReSharperUltimate.10.0.2.exe");
            this.Config.Commands.AddCommand<Command>("InstallResharper").Command =
                "c:/cfn/scripts/ReSharperUltimate.10.0.2.exe /VsVersion=14 /SpecificProductNames=ReSharper /Silent=True /PerMachine=True";
        }
    }
}
