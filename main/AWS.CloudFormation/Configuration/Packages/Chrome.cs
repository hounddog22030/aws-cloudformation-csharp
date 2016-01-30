using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing;


namespace AWS.CloudFormation.Configuration.Packages
{
    public class Chrome : PackageBase
    {
        public Chrome(LaunchConfiguration instance) : base(instance, new Uri("https://s3.amazonaws.com/gtbb/software/googlechromestandaloneenterprise64.msi"))
        {

        }
    }
}
