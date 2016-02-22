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
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Resource.Wait;
using AWS.CloudFormation.Stack;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.AutoScaling
{
    public class LaunchConfiguration : ResourceBase
    {
        public const string ChefNodeJsonConfigSetName = "ChefNodeJsonConfigSetName";
        public const string ChefNodeJsonConfigName = "ChefNodeJsonConfigName";
        public const string DefaultConfigSetName = "LaunchConfigurationConfigSet";
        public const string DefaultConfigName = "LaunchConfigurationConfig";
        public const string DefaultConfigSetRenameConfig = "Rename";
        public const string DefaultConfigSetRenameConfigRenamePowerShellCommand = "RenameComputer";
        public const int NetBiosMaxLength = 15;


        public LaunchConfiguration( Subnet subnet, 
                                    InstanceTypes instanceType, 
                                    string imageId, 
                                    OperatingSystem operatingSystem, 
                                    ResourceType resourceType,
                                    bool rename)
            : base(resourceType)
        {
            this.Rename = rename;
            _availableDevices = new List<string>();
            if (subnet != null)
            {
                this.Subnet = subnet;
            }
            this.InstanceType = instanceType;
            this.OperatingSystem = operatingSystem;
            switch (OperatingSystem)
            {
                case OperatingSystem.Windows:
                    RootDeviceId = "/dev/sda1";
                    break;
                case OperatingSystem.Linux:
                    RootDeviceId = "/dev/xvda";
                    break;
                default:
                    throw new NotSupportedException(nameof(operatingSystem));
            }
            Packages = new ObservableCollection<PackageBase<ConfigSet>>();
            Packages.CollectionChanged += Packages_CollectionChanged;
            this.ImageId = imageId;
            this.PopulateAvailableDevices();

            KeyName = new ReferenceProperty(Template.ParameterKeyPairName);
            
        }

        [JsonIgnore]
        public bool Rename { get; set; }

        protected override void OnTemplateSet(Template template)
        {
            base.OnTemplateSet(template);
            if (!this.Template.Parameters.ContainsKey(Template.ParameterKeyPairName))
            {
                throw new InvalidOperationException($"Template must contain a Parameter named {Template.ParameterKeyPairName} which contains the default encryption key name for the instance.");
            }
            UserData = new CloudFormationDictionary(this);
            UserData.Add("Fn::Base64", "");
            this.EnableHup();
            SetUserData();
            this.DisableFirewall();
            this.SetTimeZone();
            if (this.Rename && OperatingSystem == OperatingSystem.Windows &&
                this.Type != ResourceType.AwsAutoScalingLaunchConfiguration)
            {
                this.AddRename();
            }
        }

        private const int NetBiosMachineNameLengthLimit = 15;

        private void AddRename()
        {
            var computerName = this.LogicalId.Substring(0,
                this.LogicalId.Length > NetBiosMachineNameLengthLimit
                    ? NetBiosMachineNameLengthLimit
                    : this.LogicalId.Length);
            var renameConfig = this.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName).GetConfig(DefaultConfigSetRenameConfig);
            var renameCommandConfig = renameConfig.Commands.AddCommand<Command>(DefaultConfigSetRenameConfigRenamePowerShellCommand);
            renameCommandConfig.Command = new PowershellFnJoin($"\"Rename-Computer -NewName {computerName} -Restart -Force\"");
            renameCommandConfig.WaitAfterCompletion = "forever";
            renameCommandConfig.Test = $"IF \"%COMPUTERNAME%\"==\"{computerName.ToUpperInvariant()}\" EXIT /B 1 ELSE EXIT /B 0";
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

        public BlockDeviceMapping AddDisk(Ebs.VolumeTypes ec2DiskType,
            uint sizeInGigabytes,
            bool deleteOnTermination)
        {
            return this.AddDisk(ec2DiskType, sizeInGigabytes, this.GetAvailableDevice(), deleteOnTermination);
        }


        public BlockDeviceMapping AddDisk(Ebs.VolumeTypes ec2DiskType, 
            uint sizeInGigabytes, 
            string deviceId, 
            bool deleteOnTermination)
        {
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(this, deviceId);
            blockDeviceMapping.Ebs.VolumeSize = sizeInGigabytes;
            blockDeviceMapping.Ebs.VolumeType = ec2DiskType;
            blockDeviceMapping.Ebs.DeleteOnTermination = deleteOnTermination;
            this.BlockDeviceMappings.Add(blockDeviceMapping);
            return blockDeviceMapping;
        }
        public BlockDeviceMapping AddDisk(Ebs.VolumeTypes ec2DiskType, uint sizeInGigabytes)
        {
            return AddDisk(ec2DiskType,sizeInGigabytes,this.GetAvailableDevice(),true);
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

            var chefConfig = this.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName).GetConfig(DefaultConfigName);
            var nodeJson = chefConfig.Files.GetFile("c:/chef/node.json");
            return nodeJson.Content;
        }

        [JsonIgnore]
        public readonly string RootDeviceId;

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


        [JsonIgnore]
        public List<BlockDeviceMapping> BlockDeviceMappings
        {
            get
            {
                if (this.Properties.GetValue<List<BlockDeviceMapping>>()==null)
                {
                    this.BlockDeviceMappings = new List<BlockDeviceMapping>();
                }
                return this.Properties.GetValue<List<BlockDeviceMapping>>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

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
        public object IamInstanceProfile
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        public void AddBlockDeviceMapping(string deviceName, uint volumeSize, Ebs.VolumeTypes volumeType)
        {
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(this, deviceName);
            blockDeviceMapping.Ebs.VolumeSize = volumeSize;
            blockDeviceMapping.Ebs.VolumeType = volumeType;
            this.BlockDeviceMappings.Add(blockDeviceMapping);
        }

        [JsonIgnore]
        public ReferenceProperty KeyName
        {
            get
            {
                return this.Properties.GetValue<ReferenceProperty>();
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
                if (this.Properties.GetValue<CloudFormationDictionary>() == null)
                {
                    this.UserData=new CloudFormationDictionary();
                }
                return this.Properties.GetValue<CloudFormationDictionary>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public OperatingSystem OperatingSystem { get; set; }

        [JsonIgnore]
        public AutoScalingGroup AutoScalingGroup { get; set; }

        [JsonIgnore]
        public virtual Subnet Subnet {
            get
            {
                if (this.Type == ResourceType.AwsAutoScalingLaunchConfiguration)
                {
                    List<ReferenceProperty> subnetReferences = this.AutoScalingGroup.VPCZoneIdentifier as List<ReferenceProperty>;
                    return (Subnet)this.Template.Resources[subnetReferences.First().Reference.LogicalId];
                }
                else
                {
                    return this.Properties.GetValue<Subnet>();
                }
            }
            set
            {
                if (this.Type == ResourceType.AwsAutoScalingLaunchConfiguration)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    this.Properties.SetValue(value);
                }
            }
        }

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

                var setup = Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName).GetConfig(DefaultConfigName);
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
                var setup = this.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName).GetConfig(DefaultConfigName);
                var disableFirewallCommand = setup.Commands.AddCommand<Command>("DisableWindowsFirewall");
                disableFirewallCommand.Command = new PowershellFnJoin("-Command \"Get-NetFirewallProfile | Set-NetFirewallProfile -Enabled False\"");
                disableFirewallCommand.WaitAfterCompletion = 0.ToString();
            }
        }
        private void SetTimeZone()
        {
            if (OperatingSystem == OperatingSystem.Windows)
            {
                var setup = this.Metadata.Init.ConfigSets.GetConfigSet(DefaultConfigSetName).GetConfig(DefaultConfigName);
                var disableFirewallCommand = setup.Commands.AddCommand<Command>("SetTimeZone");
                disableFirewallCommand.Command = "tzutil /s \"Eastern Standard Time\"";
                disableFirewallCommand.WaitAfterCompletion = 0.ToString();
            }
        }

        protected override bool SupportsTags => this.Type == ResourceType.AwsEc2Instance;
    }
}
