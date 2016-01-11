using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Resource.EC2.Networking
{

    public class InternetGateway : ResourceBase
    {
        public InternetGateway(Template template, string name)
            : base(template, "AWS::EC2::InternetGateway", name, true)
        {
            
        }
    }
}
