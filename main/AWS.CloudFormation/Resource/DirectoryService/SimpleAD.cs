using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Serialization;
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
        public SimpleAd(object name, string password, DirectorySize size, Vpc vpc, params Subnet[] subnets) : base(ResourceType.AwsDirectoryServiceSimpleAd)
        {
            Name = name;
            Password = password;
            Size = size;
            VpcSettings = new VpcSettings(vpc,subnets);
        }

        protected override bool SupportsTags => false;
        //"CreateAlias" : Boolean,
        //    "Description" : String,
        //    "EnableSso" : Boolean,
        //    "Name" : String,
        //    "Password" : String,
        //    "ShortName" : String,
        //    "Size" : String,
        //    "VpcSettings" : VpcSettings
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
        public string Password
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
