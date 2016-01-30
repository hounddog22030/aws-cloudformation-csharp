using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.Networking;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class Vpc : ResourceBase, ICidrBlock
    {
        public Vpc(Template template, string name, string cidrBlock) : base(template, name, ResourceType.AwsEc2Vpc)
        {
            CidrBlock = cidrBlock;
            InternetGateway = new InternetGateway(template, $"{this.LogicalId}InternetGateway");
            VpcGatewayAttachment attachment = new VpcGatewayAttachment(template, $"{InternetGateway.LogicalId}Attachment", InternetGateway, this);
        }

        [JsonIgnore]
        public AvailabilityZone AvailabilityZone { get; set; }

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
        public InternetGateway InternetGateway { get; }

        [JsonIgnore]
        public string CidrBlock {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        public class VpcGatewayAttachment : ResourceBase
        {

            public VpcGatewayAttachment(Template template, string name) : base(template, name, ResourceType.AwsEc2VpcGatewayAttachment)
            {

            }

            public VpcGatewayAttachment(Template template, string name, InternetGateway internetGateway, Vpc vpc) : this(template,name)
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
