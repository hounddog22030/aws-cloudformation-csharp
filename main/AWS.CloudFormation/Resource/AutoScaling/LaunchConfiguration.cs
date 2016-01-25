using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.AutoScaling
{
    public class LaunchConfiguration : ResourceBase
    {
        internal const string ParameterNameDefaultKeyPairKeyName = "DefaultKeyPairKeyName";

        public LaunchConfiguration(Template template,
                                string name,
                                InstanceTypes instanceType,
                                string imageId,
                                OperatingSystem operatingSystem)
            : base(template, name)
        {
            this.InstanceType = instanceType;

            this.ImageId = imageId;
            if (!this.Template.Parameters.ContainsKey(ParameterNameDefaultKeyPairKeyName))
            {
                throw new InvalidOperationException($"Template must contain a Parameter named {ParameterNameDefaultKeyPairKeyName} which contains the default encryption key name for the instance.");
            }
            var keyName = this.Template.Parameters[ParameterNameDefaultKeyPairKeyName];
            KeyName = keyName.Default.ToString();
            UserData = new CloudFormationDictionary(this);
            UserData.Add("Fn::Base64", "");
            this.OperatingSystem = operatingSystem;
            ShouldEnableHup = operatingSystem==OperatingSystem.Windows;
            this.EnableHup();
            SetUserData();
        }

        [JsonIgnore]
        public bool AssociatePublicIpAddress
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


        public virtual void AddSecurityGroup(SecurityGroup securityGroup)
        {
            string propertyName = "SecurityGroups";
            if (this.Type.Contains("Instance"))
            {
                propertyName = "SecurityGroupIds";
            }

            List<ReferenceProperty> temp = new List<ReferenceProperty>();

            var ids = this.Properties.GetValue<ReferenceProperty[]>(propertyName);
            if (ids != null && ids.Any())
            {
                temp.AddRange(ids);
            }
            temp.Add(new ReferenceProperty() { Ref = securityGroup.LogicalId });
            this.Properties.SetValue(propertyName, temp.ToArray());
        }

        [JsonIgnore]
        public string WaitConditionName => $"{this.LogicalId}WaitCondition";

        [JsonIgnore]
        public string WaitConditionHandleName => this.WaitConditionName + "Handle";


        [JsonIgnore]
        public bool ShouldEnableHup { get; set; }

        public override string Type => "AWS::AutoScaling::LaunchConfiguration";
        protected override bool SupportsTags => false;

        [JsonIgnore]
        public InstanceTypes InstanceType
        {
            get
            {
                return this.Properties.GetValue<InstanceTypes>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public string ImageId
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

        [JsonIgnore]
        public OperatingSystem OperatingSystem { get; set; }
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

    }
}
