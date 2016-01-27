using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Serialization;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource
{

    public interface ILogicalId
    {
        string LogicalId { get; }
    }

    [JsonConverter(typeof (EnumConverter))]
    public enum ResourceType
    {
        [EnumMember(Value = "AWS::AutoScaling::AutoScalingGroup")] AwsAutoScalingAutoScalingGroup,
        [EnumMember(Value = "AWS::AutoScaling::LaunchConfiguration")] AwsAutoScalingLaunchConfiguration,
        [EnumMember(Value = "AWS::EC2::Instance")] AwsEc2Instance,
        [EnumMember(Value = "AWS::CloudFormation::Init")] AwsCloudFormationInit,
        [EnumMember(Value = "AWS::CloudFormation::Authentication")] AwsCloudFormationAuthentication,
        [EnumMember(Value = "AWS::EC2::EIP")] AwsEc2Eip,
        [EnumMember(Value = "AWS::EC2::InternetGateway")] AwsEc2InternetGateway,
        [EnumMember(Value = "AWS::EC2::Route")] AwsEc2Route,
        [EnumMember(Value = "AWS::EC2::RouteTable")] AwsEc2RouteTable,
        [EnumMember(Value = "AWS::EC2::SecurityGroup")] AwsEc2SecurityGroup,
        [EnumMember(Value = "AWS::EC2::Subnet")] AwsEc2Subnet,
        [EnumMember(Value = "AWS::EC2::SubnetRouteTableAssociation")] AwsEc2SubnetRouteTableAssociation,
        [EnumMember(Value = "AWS::EC2::VPCGatewayAttachment")] AwsEc2VpcGatewayAttachment,
        [EnumMember(Value = "AWS::EC2::VPC")] AwsEc2Vpc,
        [EnumMember(Value = "AWS::EC2::Volume")] AwsEc2Volume,
        [EnumMember(Value = "AWS::EC2::VolumeAttachment")] AwsEc2VolumeAttachment,
        [EnumMember(Value = "AWS::ElasticLoadBalancing::LoadBalancer")] AwsElasticLoadBalancingLoadBalancer,
        [EnumMember(Value = "AWS::Route53::HostedZone")] AwsRoute53HostedZone,
        [EnumMember(Value = "AWS::Route53::RecordSet")] AwsRoute53RecordSet,
        [EnumMember(Value = "AWS::CloudFormation::WaitCondition")] AwsCloudFormationWaitCondition,
        [EnumMember(Value = "AWS::CloudFormation::WaitConditionHandle")] AwsCloudFormationWaitConditionHandle,
        [EnumMember(Value = "AWS::EC2::KeyPair::KeyName")] AwsEc2KeyPairKeyName,
        [EnumMember(Value = "AWS::EC2::DHCPOptions")] DhcpOptions,
        [EnumMember(Value = "AWS::EC2::VPCDHCPOptionsAssociation")] VpcDhcpOptionsAssociation
    }

    public abstract class ResourceBase : ILogicalId
    {

        protected ResourceBase(Template template, string name, ResourceType type)
        {
            Template = template;
            Type = type;
            LogicalId = name;
            DependsOn2 = new List<string>();
            this.Template.Resources.Add(name, this);
            Properties = new CloudFormationDictionary();
            Metadata = new Metadata(this);
        }
        protected abstract bool SupportsTags { get; }

        [JsonIgnore]
        internal Template Template { get; private set; }

        public ResourceType Type { get; protected set; }

        public Metadata Metadata { get; }


        [JsonIgnore]
        public string LogicalId { get ; private set; }


        [JsonIgnore]
        public List<string> DependsOn2 { get; }

        public string[] DependsOn { get { return this.DependsOn2.ToArray(); } }

        public CloudFormationDictionary Properties { get; }

        [JsonIgnore]
        public TagDictionary Tags
        {

            get
            {
                if (SupportsTags)
                {
                    var returnValue = this.Properties.GetValue<TagDictionary>();
                    if (returnValue == null)
                    {
                        this.Tags = new TagDictionary();
                        return this.Tags;
                    }
                    return returnValue;
                }
                else
                {
                    return null;
                }

            }
            set
            {
                if (SupportsTags)
                {
                    this.Properties.SetValue(value);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        [JsonArray]
        public class TagDictionary : List<Tag>
        {
            
        }


    }

    public class Tag
    {
        public Tag(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get;  }
        public string Value { get; }
    }
}
