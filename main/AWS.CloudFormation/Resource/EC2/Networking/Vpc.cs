using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.Networking;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class Vpc : ResourceBase, ICidrBlock
    {
        public Vpc(string cidrBlock) : base(ResourceType.AwsEc2Vpc)
        {
            CidrBlock = cidrBlock;
        }

        protected override void OnTemplateSet(Template template)
        {
            base.OnTemplateSet(template);
            InternetGateway = new InternetGateway();
            template.Resources.Add($"{this.LogicalId}InternetGateway", InternetGateway);
            VpcGatewayAttachment attachment = new VpcGatewayAttachment(InternetGateway, this);
            template.Resources.Add($"{InternetGateway.LogicalId}Attachment", attachment);
        }

        [JsonIgnore]
        public bool EnableDnsHostnames
        {
            get { return this.Properties.GetValue<bool>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public bool EnableDnsSupport
        {
            get { return this.Properties.GetValue<bool>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public InternetGateway InternetGateway { get; set; }

        [JsonIgnore]
        public string CidrBlock {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        public class VpcGatewayAttachment : ResourceBase
        {

            public VpcGatewayAttachment() : base(ResourceType.AwsEc2VpcGatewayAttachment)
            {

            }

            public VpcGatewayAttachment(InternetGateway internetGateway, Vpc vpc) : this()
            {
                InternetGateway = internetGateway;
                Vpc = vpc;
            }


            [JsonIgnore]
            public InternetGateway InternetGateway
            {
                get
                {
                    return this.Properties.GetValue<InternetGateway>();
                }
                private set
                {
                    this.Properties.SetValue(value);
                }
            }

            [JsonIgnore]
            public Vpc Vpc
            {
                get
                {
                    return this.Properties.GetValue<Vpc>();
                }
                private set
                {
                    this.Properties.SetValue(value);
                }
            }
            protected override bool SupportsTags => false;

        }
        protected override bool SupportsTags => true;

    }
}
