using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;

namespace AWS.CloudFormation.Configuration.Packages
{
    public class AmazonAwsCli : PackageBase<ConfigSet>
    {
        public AmazonAwsCli() : base(new Uri("https://s3.amazonaws.com/gtbb/software/AWSCLI64.msi"))
        {
            
        }
    }
}
