using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Resource.EC2.Networking
{

    public class InternetGateway : ResourceBase
    {
        public InternetGateway(Template template, string name) : base(template, name)
        {
            
        }

        protected override bool SupportsTags => true;

        public override string Type => "AWS::EC2::InternetGateway";
    }
}
