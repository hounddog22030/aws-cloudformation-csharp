﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Instance.Metadata;
using AWS.CloudFormation.Instance.Metadata.Config.Command;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Resource.Wait;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Instance
{

    public class Instance : ResourceBase
    {
        public const string ParameterNameDefaultKeyPairKeyName = "DefaultKeyPairKeyName";


        internal const string T2Nano = "t2.nano";
        internal const string T2Small = "t2.small";
        internal const string T2Micro = "t2.micro";
        internal const string M4XLarge = "m4.xlarge";

        public string WaitConditionName => $"{this.Name}WaitCondition";
        public string WaitConditionHandleName => this.WaitConditionName + "Handle";

        public Instance(    Template template, 
                            string name, 
                            InstanceTypes instanceType, 
                            string imageId, 
                            OperatingSystem operatingSystem, 
                            bool enableHup)
            : base(template,"AWS::EC2::Instance", name, true)
        {
            this.OperatingSystem = operatingSystem;
            SecurityGroups = new CollectionThatSerializesAsIds<SecurityGroup>();
            this.InstanceType = instanceType;
            this.ImageId = imageId;
            NetworkInterfaces = new List<NetworkInterface>();
            if (!this.Template.Parameters.ContainsKey(Instance.ParameterNameDefaultKeyPairKeyName))
            {
                throw new InvalidOperationException($"Template must contain a Parameter named {Instance.ParameterNameDefaultKeyPairKeyName} which contains the default encryption key name for the instance.");
            }
            var keyName = this.Template.Parameters[Instance.ParameterNameDefaultKeyPairKeyName];
            KeyName = keyName;
            UserData = new CloudFormationDictionary(this);
            SourceDestCheck = true;
            ShouldEnableHup = enableHup;
            this.EnableHup();
            SetUserData();
        }



        [JsonIgnore]
        public Vpc Vpc { get; protected set; }

        [JsonIgnore]
        public bool ShouldEnableHup { get; }

        internal void SetUserData()
        {
            switch (this.OperatingSystem)
            {
                case OperatingSystem.Windows:
                    this.UserData.Clear();
                    this.UserData.Add("Fn::Base64").SetFnJoin(
                        "<script>cfn-init.exe -v -c ",
                        string.Join(",", this.Metadata.Init.ConfigSets.Keys),
                        " -s ",
                        new ReferenceProperty() { Ref = "AWS::StackId" },
                        " -r " + this.Name + " --region ",
                        new ReferenceProperty() { Ref = "AWS::Region" }, "</script>");
                    break;
                case OperatingSystem.Linux:
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        internal void EnableHup()
        {
            if (this.ShouldEnableHup)
            {

                var setup = Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("setup");
                var setupFiles = setup.Files;

                var cfnHupConfContent = setupFiles.GetFile("c:\\cfn\\cfn-hup.conf").Content;
                cfnHupConfContent.Clear();
                cfnHupConfContent.SetFnJoin( "[main]\nstack=", new ReferenceProperty() {Ref = "AWS::StackName"},
                        "\nregion=", new ReferenceProperty() {Ref = "AWS::Region"}, "\ninterval=1\nverbose=true");

                var autoReloader = setupFiles.GetFile("c:\\cfn\\hooks.d\\cfn-auto-reloader.conf");
                autoReloader.Content.Clear();
                autoReloader.Content.SetFnJoin(
                    "[cfn-auto-reloader-hook]\n",
                    "triggers=post.update\n",
                    "path=Resources." + Name + ".Metadata.AWS::CloudFormation::Init\n",
                    "action=cfn-init.exe -v -c ",
                    string.Join(",",this.Metadata.Init.ConfigSets.Keys),
                    " -s ",
                    new ReferenceProperty() {Ref = "AWS::StackName"},
                    " -r ",
                    this.Name,
                    " --region ",
                    new ReferenceProperty() {Ref = "AWS::Region"},
                    "\n");

                setup.Services.Clear();

                var cfnHup = setup.Services.Add("windows").Add("cfn-hup");
                cfnHup.Add("enabled", true);
                cfnHup.Add("ensureRunning", true);
                cfnHup.Add("files", new string[] {"c:\\cfn\\cfn-hup.conf", "c:\\cfn\\hooks.d\\cfn-auto-reloader.conf"});
            }
        }

        [JsonIgnore]
        public OperatingSystem OperatingSystem { get; }

        [CloudFormationProperties]
        public string ImageId { get; set; }

        [CloudFormationProperties]
        public ParameterBase KeyName { get; }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "SubnetId")]
        public Subnet Subnet { get; set; }

        [CloudFormationProperties]
        public InstanceTypes InstanceType { get; private set; }

        [CloudFormationProperties]
        [JsonProperty(PropertyName = "SecurityGroupIds")]
        public CollectionThatSerializesAsIds<SecurityGroup> SecurityGroups { get; private set; }

        [CloudFormationProperties]
        public bool SourceDestCheck { get; set; }

        [CloudFormationProperties]
        public List<NetworkInterface> NetworkInterfaces { get; private set; }

        [CloudFormationProperties]
        public string PrivateIpAddress { get; set; }


        public ElasticIP AddElasticIp()
        {
            ElasticIP eip = new ElasticIP(this, this.Name + "EIP");
            this.Template.AddResource(eip);
            return eip;
        }


        [CloudFormationProperties]
        public CloudFormationDictionary UserData { get; }

        public void AddDependsOn(CloudFormation.Instance.Instance dependsOn, TimeSpan timeout)
        {
            if (dependsOn.OperatingSystem != OperatingSystem.Windows)
            {
                throw new NotSupportedException($"Cannot depend on instance of OperatingSystem:{dependsOn.OperatingSystem}");
            }

            dependsOn.AddFinalizer(timeout);

            var tempDependsOn = new List<string>();

            if (this.DependsOn != null)
            {
                tempDependsOn.AddRange(this.DependsOn);
            }

            tempDependsOn.Add(dependsOn.WaitConditionName);

            this.DependsOn = tempDependsOn.ToArray();
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
                Template.Resources.Add(wait.Name, wait);
            }


        }

        [CloudFormationProperties]
        public CloudFormationDictionary[] BlockDeviceMappings { get; private set; }

        public void AddBlockDeviceMapping(string deviceName, uint volumeSize, string volumeType)
        {
            var tempBlockDeviceMapping = new List<CloudFormationDictionary>();
            if (this.BlockDeviceMappings != null)
            {
                tempBlockDeviceMapping.AddRange(this.BlockDeviceMappings);
            }
            var c = new CloudFormationDictionary();
            c.Add("DeviceName", deviceName);
            var ebs = c.Add("Ebs");
            ebs.Add("VolumeSize", volumeSize.ToString());
            ebs.Add("VolumeType", volumeType);
            tempBlockDeviceMapping.Add(c);
            this.BlockDeviceMappings = tempBlockDeviceMapping.ToArray();
        }
    }
}
