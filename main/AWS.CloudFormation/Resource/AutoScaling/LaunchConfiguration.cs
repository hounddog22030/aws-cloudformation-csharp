using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Configuration.Packages;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Wait;
using AWS.CloudFormation.Stack;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.AutoScaling
{
    public class LaunchConfiguration : ResourceBase
    {
        internal const string ParameterNameDefaultKeyPairKeyName = "DefaultKeyPairKeyName";
        public const string ChefNodeJsonConfigSetName = "ChefNodeJsonConfigSetName";
        public const string ChefNodeJsonConfigName = "ChefNodeJsonConfigName";
        public const string DefaultConfigSetName = "config";
        public const string DefaultConfigSetRenameConfig = "rename";
        public const string DefaultConfigSetJoinConfig = "join";
        public const string DefaultConfigSetRenameConfigRenamePowerShellCommand = "1-execute-powershell-script-RenameComputer";
        public const string DefaultConfigSetRenameConfigJoinDomain = "b-join-domain";
        public const int NetBiosMaxLength = 15;


        public LaunchConfiguration(Template template,
                                string name,
                                InstanceTypes instanceType,
                                string imageId,
                                OperatingSystem operatingSystem)
            : base(template, name, ResourceType.AwsAutoScalingLaunchConfiguration)
        {
            _availableDevices = new List<string>();
            this.InstanceType = instanceType;
            this.OperatingSystem = operatingSystem;
            Packages = new ObservableCollection<PackageBase<ConfigSet>>();
            Packages.CollectionChanged += Packages_CollectionChanged;
            this.ImageId = imageId;
            this.PopulateAvailableDevices();

            if (!this.Template.Parameters.ContainsKey(ParameterNameDefaultKeyPairKeyName))
            {
                throw new InvalidOperationException($"Template must contain a Parameter named {ParameterNameDefaultKeyPairKeyName} which contains the default encryption key name for the instance.");
            }
            var keyName = this.Template.Parameters[ParameterNameDefaultKeyPairKeyName];
            KeyName = keyName.Default.ToString();
            UserData = new CloudFormationDictionary(this);
            UserData.Add("Fn::Base64", "");
            this.EnableHup();
            SetUserData();
            this.DisableFirewall();
            this.AddRename();
        }

        private void AddRename()
        {
            if (OperatingSystem == OperatingSystem.Windows)
            {
                var renameConfig = this.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName).GetConfig(DefaultConfigSetRenameConfig);
                var renameCommandConfig = renameConfig.Commands.AddCommand<Command>(DefaultConfigSetRenameConfigRenamePowerShellCommand);
                renameCommandConfig.Command = new PowershellFnJoin($"\"Rename-Computer -NewName {this.LogicalId} -Restart\"");
                renameCommandConfig.WaitAfterCompletion = "forever";
                renameCommandConfig.Test = $"IF /I \"%COMPUTERNAME%\"==\"{this.LogicalId}\" EXIT /B 1 ELSE EXIT /B 0";
            }
        }


        [JsonIgnore]
        public string DomainDnsName { get; internal set; }
        [JsonIgnore]
        public string DomainNetBiosName { get; internal set; }


        public void AddDependsOn(WaitCondition waitConditionHandle)
        {
            this.DependsOn.Add(waitConditionHandle.LogicalId);
        }

        private void Packages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (var newItem in e.NewItems)
            {
                var senderAsPackage = newItem as PackageBase<ConfigSet>;
                senderAsPackage.AddToLaunchConfiguration(this);

            }
        }

        [JsonIgnore]
        public ObservableCollection<PackageBase<ConfigSet>> Packages { get; }

        public void AddDisk(Ebs.VolumeTypes ec2DiskType, int sizeInGigabytes)
        {
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(this, this.GetAvailableDevice());
            blockDeviceMapping.Ebs.VolumeSize = sizeInGigabytes;
            blockDeviceMapping.Ebs.VolumeType = ec2DiskType;
            this.AddBlockDeviceMapping(blockDeviceMapping);
        }

        protected void PopulateAvailableDevices()
        {
            switch (OperatingSystem)
            {
                case OperatingSystem.Windows:
                    for (char c = 'f'; c < 'z'; c++)
                    {
                        _availableDevices.Add($"xvd{c}");
                    }
                    break;
                case OperatingSystem.Linux:
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        public ConfigFileContent GetChefNodeJsonContent()
        {

            var chefConfig = this.Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("setup");
            var nodeJson = chefConfig.Files.GetFile("c:/chef/node.json");
            return nodeJson.Content;
        }

        private readonly List<string> _availableDevices;
        internal string GetAvailableDevice()
        {
            var returnValue = _availableDevices.First();
            _availableDevices.Remove(returnValue);
            return returnValue;
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
            if (this.Type==ResourceType.AwsEc2Instance)
            {
                propertyName = "SecurityGroupIds";
            }

            List<ReferenceProperty> temp = new List<ReferenceProperty>();

            var ids = this.Properties.GetValue<ReferenceProperty[]>(propertyName);
            if (ids != null && ids.Any())
            {
                temp.AddRange(ids);
            }
            temp.Add(new ReferenceProperty(securityGroup));
            this.Properties.SetValue(propertyName, temp.ToArray());
        }

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
                        "<script>",
                        "cfn-init.exe -v -c ",
                        string.Join(",", this.Metadata.Init.ConfigSets.Keys),
                        " -s ",
                        new ReferenceProperty("AWS::StackId"),
                        " -r " + this.LogicalId + " --region ",
                        new ReferenceProperty("AWS::Region"),
                        "</script>");
                    break;
                case OperatingSystem.Linux:
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
        internal void EnableHup()
        {
            if (this.OperatingSystem==OperatingSystem.Windows)
            {

                var setup = Metadata.Init.ConfigSets.GetConfigSet("config").GetConfig("setup");
                var setupFiles = setup.Files;

                var cfnHupConfContent = setupFiles.GetFile("c:\\cfn\\cfn-hup.conf").Content;
                cfnHupConfContent.Clear();
                cfnHupConfContent.SetFnJoin("[main]\nstack=", new ReferenceProperty("AWS::StackName"),
                        "\nregion=", new ReferenceProperty("AWS::Region"), "\ninterval=1\nverbose=true");

                var autoReloader = setupFiles.GetFile("c:\\cfn\\hooks.d\\cfn-auto-reloader.conf");
                autoReloader.Content.Clear();
                autoReloader.Content.SetFnJoin(
                    "[cfn-auto-reloader-hook]\n",
                    "triggers=post.update\n",
                    "path=Resources." + LogicalId + ".Metadata.AWS::CloudFormation::Init\n",
                    "action=",
                    "cfn-init.exe -v -c ",
                    string.Join(",", this.Metadata.Init.ConfigSets.Keys),
                    " -s ",
                    new ReferenceProperty("AWS::StackName"),
                    " -r ",
                    this.LogicalId,
                    " --region ",
                    new ReferenceProperty("AWS::Region"),
                    "\n");

                setup.Services.Clear();

                var cfnHup = setup.Services.Add("windows").Add("cfn-hup");
                cfnHup.Add("enabled", true);
                cfnHup.Add("ensureRunning", true);
                cfnHup.Add("files", new string[] { "c:\\cfn\\cfn-hup.conf", "c:\\cfn\\hooks.d\\cfn-auto-reloader.conf" });
            }
        }

        private void DisableFirewall()
        {
            if (OperatingSystem == OperatingSystem.Windows)
            {
                var setup = this.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName).GetConfig("setup");
                var disableFirewallCommand = setup.Commands.AddCommand<Command>("a-disable-win-fw");
                disableFirewallCommand.Command = new PowershellFnJoin("-Command \"Get-NetFirewallProfile | Set-NetFirewallProfile -Enabled False\"");
                disableFirewallCommand.WaitAfterCompletion = 0.ToString();
            }
        }

    }
}
