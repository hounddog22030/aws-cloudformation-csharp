using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Resource.AutoScaling;

namespace AWS.CloudFormation.Configuration.Packages
{
    public class ReSharper : PackageBase
    {
        public ReSharper(LaunchConfiguration instance) : base(instance,new Uri("http://download.jetbrains.com/resharper/ReSharperSetup.8.2.3000.5176.msi"))
        {
        }
    }
}
