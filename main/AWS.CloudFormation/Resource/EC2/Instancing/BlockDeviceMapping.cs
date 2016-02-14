using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Serialization;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AWS.CloudFormation.Resource.EC2.Instancing
{
    public class BlockDeviceMapping : CloudFormationDictionary
    {
        public BlockDeviceMapping(ResourceBase resource, string deviceName) : base(resource)
        {
            Ebs = new Ebs(resource);
            DeviceName = deviceName;
        }
        public BlockDeviceMapping(ResourceBase resource, string deviceName, bool deleteOnTermination) : this(resource,deviceName)
        {
            this.Ebs.DeleteOnTermination = deleteOnTermination;
        }

        [JsonIgnore]
        public Ebs Ebs
        {
            get { return this.GetValue<Ebs>(); }
            set { this.SetValue(value); }
        }

        [JsonIgnore]
        public object NoDevice
        {
            get { return this.GetValue<object>(); }
            set { this.SetValue(value); }
        }

        public string DeviceName
        {
            get { return this.GetValue<string>(); }
            set { this.SetValue(value); }
        }
    }
    public class Ebs : CloudFormationDictionary
    {
        public Ebs(ResourceBase resource) : base(resource)
        {
            
        }

        [JsonIgnore]

        public string SnapshotId
        {
            get { return this.GetValue<string>(); }
            set { this.SetValue(value); }
        }


        [JsonIgnore]
        public uint VolumeSize
        {
            get { return this.GetValue<uint>(); }
            set { this.SetValue(value); }
        }

        [JsonConverter(typeof(EnumConverter))]
        public enum VolumeTypes
        {
            [EnumMember(Value="gp2")]
            GeneralPurpose,
            [EnumMember(Value = "standard")]
            Magnetic,
            [EnumMember(Value = "io1")]
            ProvisionedIops
        }

        [JsonIgnore]
        public VolumeTypes VolumeType
        {
            get { return this.GetValue<VolumeTypes>(); }
            set { this.SetValue(value); }
        }

        [JsonIgnore]
        public bool DeleteOnTermination
        {
            get { return this.GetValue<bool>(); }
            set { this.SetValue(value); }
        }
    }
}
