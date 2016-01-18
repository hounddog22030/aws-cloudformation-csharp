using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class Vpc : ResourceBase, ICidrBlock
    {
        public Vpc(Template template, string name, string cidrBlock) : base(template, "AWS::EC2::VPC", name, true)
        {
            CidrBlock = cidrBlock;
        }

        [JsonIgnore]
        public string CidrBlock {
            get { return (string)this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        public class VpcGatewayAttachment : ResourceBase
        {

            public VpcGatewayAttachment(Template template, string name)
                : base(template, "AWS::EC2::VPCGatewayAttachment", name, false)
            {

            }


            [JsonIgnore]
            public InternetGateway InternetGateway
            {
                get
                {
                    var vpcId = this.Properties.GetValue<CloudFormationDictionary>();
                    return vpcId["Ref"] as InternetGateway;
                }
                set
                {
                    var refDictionary = new CloudFormationDictionary();
                    refDictionary.Add("Ref", ((ILogicalId)value).LogicalId);
                    this.Properties.SetValue(refDictionary);
                }
            }

            [JsonIgnore]
            public Vpc Vpc
            {
                get
                {
                    var vpcId = this.Properties.GetValue<CloudFormationDictionary>();
                    return vpcId["Ref"] as Vpc;
                }
                set
                {
                    var refDictionary = new CloudFormationDictionary();
                    refDictionary.Add("Ref", ((ILogicalId)value).LogicalId);
                    this.Properties.SetValue(refDictionary);
                }
            }
        }
    }
}
