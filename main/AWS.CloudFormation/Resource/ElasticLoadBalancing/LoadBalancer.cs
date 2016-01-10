using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Resource.ElasticLoadBalancing
{
    public class LoadBalancer : ResourceBase
    {
        public LoadBalancer(Template template, string name) : base(template, "AWS::ElasticLoadBalancing::LoadBalancer", name, false)
        {
        }
    }
}
