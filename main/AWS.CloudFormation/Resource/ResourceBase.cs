using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.Wait;
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
        [EnumMember(Value = "AWS::EC2::VPCDHCPOptionsAssociation")] VpcDhcpOptionsAssociation,
        [EnumMember(Value = "AWS::RDS::DBInstance")] AwsRdsDbInstance,
        [EnumMember(Value = "AWS::RDS::DBSubnetGroup")] AwsRdsDbSubnetGroup,
        [EnumMember(Value = "AWS::RDS::DBSecurityGroup")] AwsRdsDbSecurityGroup
    }

    public abstract class ResourceBase : ILogicalId
    {

        protected ResourceBase(ResourceType type)
        {
            Type = type;
            DependsOn = new List<string>();
            Metadata = new Metadata(this);
        }

        protected virtual void OnTemplateSet(Template template)
        {
            
        }

        public void AddDependsOn(WaitCondition waitConditionHandle)
        {
            this.DependsOn.Add(waitConditionHandle.LogicalId);
        }

        protected abstract bool SupportsTags { get; }

        private Template _template;

        [JsonIgnore]
        internal Template Template {
            get { return _template; }
            set
            {
                _template = value;
                OnTemplateSet(value);
            }
        }

        public ResourceType Type { get; protected set; }

        public bool ShouldSerializeMetadata()
        {
            return this.Metadata != null && this.Metadata.Any();
        }

        public Metadata Metadata { get; }


        private string _logicalId;

        [JsonIgnore]
        public string LogicalId
        {
            get
            {
                return _logicalId;
            }
            internal set
            {
                _logicalId = value;
                if (SupportsTags)
                {
                    this.Tags.Add(new Tag("Name", value));
                }

            }
        }

        public bool ShouldSerializeDependsOn()
        {
            return this.DependsOn.Any();
        }

        public List<string> DependsOn { get; private set; }

        //public string[] DependsOn => this.DependsOn2.ToArray();

        private bool _serializing = false;
        public bool ShouldSerializeProperties()
        {
            _serializing = true;
            return true;
        }

        private CloudFormationDictionary _properties;
        public CloudFormationDictionary Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = new CloudFormationDictionary(this);
                }
                if (_serializing)
                {
                    CloudFormationDictionary clone = new CloudFormationDictionary(this);
                    foreach (var key in _properties.Keys)
                    {
                        ICollection valueAsCollection = _properties[key] as ICollection;
                        bool shouldAdd = valueAsCollection == null || valueAsCollection.Count > 0;
                        if (shouldAdd)
                        {
                            clone.Add(key, _properties[key]);
                        }
                    }
                    return clone;

                }
                else
                {
                    return _properties;
                }
            }
        }

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
