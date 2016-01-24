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
    public class Instance : LaunchConfiguration, ICidrBlock
    {
        public const string ParameterNameDefaultKeyPairKeyName = "DefaultKeyPairKeyName";



        [JsonIgnore]
        public string WaitConditionName => $"{this.LogicalId}WaitCondition";

        [JsonIgnore]
        public string WaitConditionHandleName => this.WaitConditionName + "Handle";

        public Instance(Template template, string name, InstanceTypes instanceType, string imageId, OperatingSystem operatingSystem, bool enableHup)
            : base(template, name, instanceType, imageId)
        {
            this.OperatingSystem = operatingSystem;
            SecurityGroupIds = new IdCollection<SecurityGroup>();
            NetworkInterfaces = new List<NetworkInterface>();
            if (!this.Template.Parameters.ContainsKey(ParameterNameDefaultKeyPairKeyName))
            {
                throw new InvalidOperationException($"Template must contain a Parameter named {ParameterNameDefaultKeyPairKeyName} which contains the default encryption key name for the instance.");
            }
            var keyName = this.Template.Parameters[ParameterNameDefaultKeyPairKeyName];
            KeyName = keyName.Default.ToString();
            UserData = new CloudFormationDictionary(this);
            UserData.Add("Fn::Base64", "");
            SourceDestCheck = true;
            ShouldEnableHup = enableHup;
            this.EnableHup();
            SetUserData();
        }


        [JsonIgnore]
        public bool ShouldEnableHup { get; set; }


        [JsonIgnore]
        public OperatingSystem OperatingSystem { get; set; }

        [JsonIgnore]
        public string KeyName
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

        [JsonIgnore] public Subnet Subnet
        {
            get
            {
                return this.Properties.GetValue<Subnet>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }


        [JsonIgnore]
        public IdCollection<SecurityGroup> SecurityGroupIds
        {
            get
            {
                return this.Properties.GetValue<IdCollection<SecurityGroup>>();
            }
            set
            {
                this.Properties.SetValue(value);
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

        [JsonIgnore]
        public List<NetworkInterface> NetworkInterfaces
        {
            get
            {
                return this.Properties.GetValue<List<NetworkInterface>>();
            }
            set
            {
                this.Properties.SetValue(value);
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

         


        [JsonIgnore]
        public CloudFormationDictionary UserData
        {
            get
            {
                return this.Properties.GetValue<CloudFormationDictionary>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
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
        public CloudFormationDictionary[] BlockDeviceMappings
        {
            get
            {
                return this.Properties.GetValue<CloudFormationDictionary[]>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

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
                        " -r " + this.LogicalId + " --region ",
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
                cfnHupConfContent.SetFnJoin("[main]\nstack=", new ReferenceProperty() { Ref = "AWS::StackName" },
                        "\nregion=", new ReferenceProperty() { Ref = "AWS::Region" }, "\ninterval=1\nverbose=true");

                var autoReloader = setupFiles.GetFile("c:\\cfn\\hooks.d\\cfn-auto-reloader.conf");
                autoReloader.Content.Clear();
                autoReloader.Content.SetFnJoin(
                    "[cfn-auto-reloader-hook]\n",
                    "triggers=post.update\n",
                    "path=Resources." + LogicalId + ".Metadata.AWS::CloudFormation::Init\n",
                    "action=cfn-init.exe -v -c ",
                    string.Join(",", this.Metadata.Init.ConfigSets.Keys),
                    " -s ",
                    new ReferenceProperty() { Ref = "AWS::StackName" },
                    " -r ",
                    this.LogicalId,
                    " --region ",
                    new ReferenceProperty() { Ref = "AWS::Region" },
                    "\n");

                setup.Services.Clear();

                var cfnHup = setup.Services.Add("windows").Add("cfn-hup");
                cfnHup.Add("enabled", true);
                cfnHup.Add("ensureRunning", true);
                cfnHup.Add("files", new string[] { "c:\\cfn\\cfn-hup.conf", "c:\\cfn\\hooks.d\\cfn-auto-reloader.conf" });
            }
        }

        public void AddBlockDeviceMapping(string deviceName, uint volumeSize, Ebs.VolumeTypes volumeType)
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

        public void AddBlockDeviceMapping(BlockDeviceMapping blockDeviceMapping)
        {
            var tempBlockDeviceMapping = new List<CloudFormationDictionary>();

            if (this.BlockDeviceMappings != null)
            {
                tempBlockDeviceMapping.AddRange(this.BlockDeviceMappings);
            }
            tempBlockDeviceMapping.Add(blockDeviceMapping);
            this.BlockDeviceMappings = tempBlockDeviceMapping.ToArray();
        }


        [JsonIgnore]
        public string CidrBlock {
            get { return this.PrivateIpAddress + "/32"; }
            set
            {
                throw new ReadOnlyException();
            }
        }

        protected override bool SupportsTags {
            get { return true; } 
        }
        public override string Type {
            get { return "AWS::EC2::Instance"; }
        }
    }
}
