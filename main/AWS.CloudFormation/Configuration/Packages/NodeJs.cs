using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;

namespace AWS.CloudFormation.Configuration.Packages
{
    public class NodeJs : PackageBase<ConfigSet>
    {
        public NodeJs() : base(new Uri("https://s3.amazonaws.com/gtbb/software/node-v4.3.0-x64.msi"))
        {
            
        }
    }
}
