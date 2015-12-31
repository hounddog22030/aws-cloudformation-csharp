using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.CloudFormation.Resource.Networking
{
    public interface ICidrBlock
    {
        string CidrBlock { get; set; }
    }
}
