using System.Collections.Generic;
using System.ComponentModel;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Instance.MetaData.Config.Command;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Instance
{

    public class Instance : ResourceBase
    {
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
                            string keyName, 
                            OperatingSystem operatingSystem, 
                            bool enableHup)
            : base(template,"AWS::EC2::Instance", name, true)
        {
            this.OperatingSystem = operatingSystem;
            SecurityGroups = new CollectionThatSerializesAsIds<SecurityGroup>();
            this.InstanceType = instanceType;
            this.ImageId = imageId;
            NetworkInterfaces = new List<NetworkInterface>();
            KeyName = keyName;
            UserData = new CloudFormationDictionary(this);
            Metadata = new MetaData.MetaData(this);
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
        public string KeyName { get; }

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

        public MetaData.MetaData Metadata { get; }

        public ElasticIP AddElasticIp(string name)
        {
            ElasticIP eip = ElasticIP.Create(name, this);
            this.Template.AddResource(eip);
            return eip;
        }


        [CloudFormationProperties]
        public CloudFormationDictionary UserData { get; }
    }
}
