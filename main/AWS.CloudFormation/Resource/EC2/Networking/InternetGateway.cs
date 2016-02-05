using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Resource.EC2.Networking
{

    public class InternetGateway : ResourceBase
    {
        public InternetGateway() : base(ResourceType.AwsEc2InternetGateway)
        {
            
        }

        protected override bool SupportsTags => true;

    }
}
