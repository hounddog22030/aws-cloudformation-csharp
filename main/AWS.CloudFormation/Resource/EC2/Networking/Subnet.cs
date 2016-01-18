using System;
using System.Linq;
using System.Runtime.Serialization;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AWS.CloudFormation.Resource.EC2.Networking
{

    public class Subnet : ResourceBase, ICidrBlock
    {
        public const string AVAILIBILITY_ZONE_US_EAST_1A = "us-east-1a";
        public const string SUBNET_TYPE = "AWS::EC2::Subnet";


        public Subnet(Template template, string logicalId, Vpc vpc, string cidr, Template.AvailabilityZone availabilityZone) : base(template, SUBNET_TYPE,logicalId,true)
        {
            Vpc = vpc;
            CidrBlock = cidr;
            AvailabilityZone = availabilityZone;
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

        [JsonIgnore]
        public string CidrBlock
        {
            get { return (string)this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public Template.AvailabilityZone AvailabilityZone
        {
            get { return this.Properties.GetValue<Template.AvailabilityZone>(); }
            set
            {
                var enumType = typeof(Template.AvailabilityZone);
                var name = Enum.GetName(enumType, value);
                var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).Single();
                this.Properties.SetValue(enumMemberAttribute.Value);
            }
        }
    }
}
