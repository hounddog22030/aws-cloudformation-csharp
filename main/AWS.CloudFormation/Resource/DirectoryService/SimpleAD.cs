using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Configuration.Packages;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Serialization;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.DirectoryService
{

    [JsonConverter(typeof(EnumConverter))]
    public enum DirectorySize
    {
        None,
        [EnumMember(Value = "Small")]
        Small,
        [EnumMember(Value = "Large")]
        Large
    }
    public class SimpleAd : ResourceBase
    {
        public const string DomainVersionParameterName = "DomainVersion";
        public const string DomainAppNameParameterName = "DomainAppName";
        public const string DomainTopLevelNameParameterName = "DomainTopLevelName";
        public const string DomainAdminUsernameParameterName = "DomainAdminUsername";
        public const string DomainAdminPasswordParameterName = "DomainAdminPassword";
        public const string DomainNetBiosNameParameterName = "DomainNetBiosName";
        public const string DomainFqdnParameterName = "DomainFqdn";

        public SimpleAd(object name, object password, DirectorySize size, Vpc vpc, params Subnet[] subnets) : base(ResourceType.AwsDirectoryServiceSimpleAd)
        {
            Name = name;
            Password = password;
            Size = size;
            VpcSettings = new VpcSettings(vpc,subnets);
        }

        protected override bool SupportsTags => false;

        [JsonIgnore]
        public VpcSettings VpcSettings
        {
            get
            {
                return this.Properties.GetValue<VpcSettings>();
            }
            private set { this.Properties.SetValue(value); }
        }
        [JsonIgnore]
        public DirectorySize Size
        {
            get
            {
                return this.Properties.GetValue<DirectorySize>();
            }
            private set { this.Properties.SetValue(value); }
        }
        [JsonIgnore]
        public object Name
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            private set { this.Properties.SetValue(value); }
        }
        [JsonIgnore]
        public object Password
        {
            get
            {
                return this.Properties.GetValue<string>();
            }
            private set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public object ShortName
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            set { this.Properties.SetValue(value); }
        }

        public static void AddInstanceToDomain(Config config)
        {

            const string CheckForDomainPsPath = "c:/cfn/scripts/check-for-domain.ps1";


            var checkForDomainPs = config.Files.GetFile(CheckForDomainPsPath);
            checkForDomainPs.Source = "https://s3.amazonaws.com/gtbb/check-for-domain.ps1";

            var joinCommand = config.Commands.AddCommand<Command>("JoinDomain");
            joinCommand.Command = new PowershellFnJoin(FnJoinDelimiter.None,
                "-Command \"",
                "Add-Computer -DomainName ",
                new FnJoin(FnJoinDelimiter.Period,
                            new ReferenceProperty(SimpleAd.DomainVersionParameterName),
                            new ReferenceProperty(SimpleAd.DomainAppNameParameterName),
                            new ReferenceProperty(SimpleAd.DomainTopLevelNameParameterName)),
                " -Credential (New-Object System.Management.Automation.PSCredential('",
                new ReferenceProperty(SimpleAd.DomainAdminUsernameParameterName),
                "@",
                new FnJoin(FnJoinDelimiter.Period,
                new ReferenceProperty(SimpleAd.DomainVersionParameterName),
                new ReferenceProperty(SimpleAd.DomainAppNameParameterName),
                new ReferenceProperty(SimpleAd.DomainTopLevelNameParameterName)),
                "',(ConvertTo-SecureString \"",
                new ReferenceProperty(SimpleAd.DomainAdminPasswordParameterName),
                "\" -AsPlainText -Force))) ",
                "-Restart\"");
            joinCommand.WaitAfterCompletion = "forever";
            joinCommand.Test = $"powershell.exe -ExecutionPolicy RemoteSigned {CheckForDomainPsPath}";
        }

    }

    public class VpcSettings
    {
        public VpcSettings(Vpc vpc, params Subnet[] subnets)
        {
            SubnetIds = new List<object>();

            VpcId = new ReferenceProperty(vpc);

            foreach (var subnet in subnets)
            {
                this.SubnetIds.Add( new ReferenceProperty(subnet));
            }
        }

        public object VpcId { get; }
        public List<object> SubnetIds { get; }
    }
}
