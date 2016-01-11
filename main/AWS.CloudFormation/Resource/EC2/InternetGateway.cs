using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Resource.Networking
{

    public class InternetGateway : ResourceBase
    {
        public InternetGateway(Template template, string name)
            : base(template, "AWS::EC2::InternetGateway", name, true)
        {
            
        }
    }
}
