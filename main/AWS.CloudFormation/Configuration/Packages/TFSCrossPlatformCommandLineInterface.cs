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
    public class TfsCrossPlatformCommandLineInterface : PackageBase<ConfigSet>
    {
        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            if (!configuration.Packages.Any(p => p.GetType() == typeof (NodeJs)))
            {
                var thisPackageIndex = 0;
                for (int i = 0; i < configuration.Packages.Count; i++)
                {
                    if (configuration.Packages[i] == this)
                    {
                        thisPackageIndex = i;
                        break;
                    }
                }
                configuration.Packages.Insert(thisPackageIndex,new NodeJs());
            }
            var commandConfig = this.Config.Commands.AddCommand<Command>("InstallTfxCli");
            commandConfig.Command = "npm i -g tfx-cli";
            commandConfig.WaitAfterCompletion = 0.ToString();
            this.Config.IgnoreErrors = true.ToString();
        }
    }
}
