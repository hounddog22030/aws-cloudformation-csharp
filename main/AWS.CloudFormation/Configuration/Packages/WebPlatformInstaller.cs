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
    public class WebPlatformInstaller : PackageBase<ConfigSet>
    {
        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            this.Config.Files.Add("c:/cfn/scripts/wpilauncher.exe")
                .Add("source", "https://s3.amazonaws.com/gtbb/software/wpilauncher.exe");
        }
    }
}
