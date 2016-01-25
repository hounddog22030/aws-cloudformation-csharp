﻿using System;
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

        public override string Type { get; }

        public Instance(Template template, string name, InstanceTypes instanceType, string imageId,
            OperatingSystem operatingSystem, bool enableHup)
            : this(template,name,instanceType,imageId,operatingSystem,enableHup,DefinitionType.Instance)
        {
            
        }

        public Instance(Template template, string name, InstanceTypes instanceType, string imageId, OperatingSystem operatingSystem, bool enableHup, DefinitionType definitionType)
            : base(template, name, instanceType, imageId, operatingSystem)
        {
            switch (definitionType)
            {
                    case DefinitionType.Instance:
                    this.Type = "AWS::EC2::Instance";
                    // only applies to instances
                    SourceDestCheck = true;
                    break;
                case DefinitionType.LaunchConfiguration:
                    this.Type = "AWS::AutoScaling::LaunchConfiguration";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(definitionType));
            }


            NetworkInterfaces = new List<NetworkInterface>();

            if (this.SupportsTags)
            {
                this.Tags.Add("Name",name);
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

        [JsonIgnore] public Subnet Subnet
        {
            get
            {
                if (this.Type.Contains("Instance"))
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
                if (this.Type.Contains("Instance"))
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
                if (this.Type.Contains("Instance"))
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
                if (this.Type.Contains("Instance"))
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
            ElasticIp = new ElasticIp(this, this.LogicalId + "EIP");
            return ElasticIp;
        }

        protected override bool SupportsTags
        {
            get { return this.Type.Contains("Instance"); }
        }


        public void AddDependsOn(Instance dependsOn, TimeSpan timeout)
        {
            if (dependsOn.OperatingSystem != OperatingSystem.Windows)
            {
                throw new NotSupportedException($"Cannot depend on instance of OperatingSystem:{dependsOn.OperatingSystem}");
            }

            dependsOn.AddFinalizer(timeout);

            this.DependsOn2.Add(dependsOn.WaitConditionName);

        }

        public void AddFinalizer(TimeSpan timeout)
        {
            var finalizeConfig =
                this.Metadata.Init.ConfigSets.GetConfigSet(Init.FinalizeConfigSetName).GetConfig(Init.FinalizeConfigName);

            const string finalizeKey = "a-signal-success";

            if (!finalizeConfig.Commands.ContainsKey(finalizeKey))
            {
                var command = finalizeConfig.Commands.AddCommand<Command>("a-signal-success",
                    Commands.CommandType.CompleteWaitHandle);
                command.WaitAfterCompletion = 0.ToString();

                WaitCondition wait = new WaitCondition(Template, this.WaitConditionName, timeout);
            }


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
