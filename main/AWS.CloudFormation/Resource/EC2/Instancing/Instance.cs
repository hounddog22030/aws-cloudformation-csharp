using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Resource.Wait;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Instancing
{

    public enum DefinitionType
    {

        Instance,
        LaunchConfiguration
    }
    public class Instance : LaunchConfiguration, ICidrBlock
    {

        public Instance(Template template, string name, InstanceTypes instanceType, string imageId,
            OperatingSystem operatingSystem, bool enableHup)
            : this(template,name,instanceType,imageId,operatingSystem,enableHup,DefinitionType.Instance)
        {
            
        }

        public Instance(Template template, string name, InstanceTypes instanceType, string imageId, OperatingSystem operatingSystem, bool enableHup, DefinitionType definitionType)
            : base(template, name, instanceType, imageId, operatingSystem, ResourceType.AwsEc2Instance)
        {
            switch (definitionType)
            {
                    case DefinitionType.Instance:
                    this.Type = ResourceType.AwsEc2Instance;
                    // only applies to instances
                    SourceDestCheck = true;
                    break;
                case DefinitionType.LaunchConfiguration:
                    this.Type = ResourceType.AwsAutoScalingLaunchConfiguration;
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(definitionType));
            }


            NetworkInterfaces = new List<NetworkInterface>();

            if (this.SupportsTags)
            {
                this.Tags.Add( new Tag("Name",name) );
            }
        }

        



        //public IdCollection<SecurityGroup> SecurityGroupIds
        //{
        //    get
        //    {
        //        if (this.Type.Contains("Instance"))
        //        {
        //            return this.Properties.GetValue<IdCollection<SecurityGroup>>();
        //        }
        //        else
        //        {
        //            return _securityGroupIds;
        //        }
        //    }
        //    set
        //    {
        //        if (this.Type.Contains("Instance"))
        //        {
        //            this.Properties.SetValue(value);
        //        }
        //        else
        //        {
        //            _securityGroupIds = value;
        //        }
        //    }
        //}


        private Subnet _subnet;

        [JsonIgnore] public override Subnet Subnet
        {
            get
            {
                if (this.Type==ResourceType.AwsEc2Instance)
                {
                    return this.Properties.GetValue<Subnet>();
                }
                else
                {
                    return _subnet;
                }
            }
            set
            {
                if (this.Type == ResourceType.AwsEc2Instance)
                {
                    this.Properties.SetValue(value);
                }
                else
                {
                    _subnet = value;
                }
            }
        }

        [JsonIgnore]
        public bool SourceDestCheck
        {
            get
            {
                return this.Properties.GetValue<bool>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        private List<NetworkInterface> _networkInterfaces;

        [JsonIgnore]
        public List<NetworkInterface> NetworkInterfaces
        {
            get
            {
                if (this.Type == ResourceType.AwsEc2Instance)
                {
                    return this.Properties.GetValue<List<NetworkInterface>>();
                }
                else
                {
                    return _networkInterfaces;
                }
                
            }
            set
            {
                if (this.Type == ResourceType.AwsEc2Instance)
                {
                    this.Properties.SetValue(value);
                }
                else
                {
                    _networkInterfaces = value;
                }
            }
        }

        [JsonIgnore]
        public string PrivateIpAddress
        {
            get
            {
                return this.Properties.GetValue<string>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }


        [JsonIgnore]
        protected ElasticIp ElasticIp { get; set; }

        public ElasticIp AddElasticIp()
        {
            ElasticIp = new ElasticIp(this);
            return ElasticIp;
        }

        protected override bool SupportsTags
        {
            get { return this.Type == ResourceType.AwsEc2Instance; }
        }

        [JsonIgnore]
        public string CidrBlock {
            get { return this.PrivateIpAddress + "/32"; }
            set
            {
                throw new ReadOnlyException();
            }
        }
    }
}
