﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Serialization;
using AWS.CloudFormation.Serializer;
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
            this.Add("Ebs", Ebs);
            this.Add("DeviceName", deviceName);
        }

        public Ebs Ebs { get;}

    }
    public class Ebs : CloudFormationDictionary
    {
        public Ebs(ResourceBase resource) : base(resource)
        {
            
        }

        public string SnapshotId
        {
            get
            {
                if (this.ContainsKey("SnapshotId"))
                {
                    return (string)this["SnapshotId"];
                }
                return null;
            }
            set { this["SnapshotId"] = value; }
        }
        

        public int VolumeSize
        {
            get
            {
                if (this.ContainsKey("VolumeSize"))
                {
                    return (int)this["VolumeSize"];
                }
                return 0;
            }
            set { this["VolumeSize"] = value; }
        }

        [JsonConverter(typeof(EnumConverter))]
        public enum VolumeTypes
        {
            none,
            gp2,
            standard,
            io1
        }

        //[JsonProperty(ItemConverterType = typeof(Enum))]
        public VolumeTypes VolumeType
        {
            get
            {
                if (this.ContainsKey("VolumeType"))
                {
                    return (VolumeTypes)this["VolumeType"];
                }
                return VolumeTypes.none;
            }
            set { this["VolumeType"] = value; }
        }
    }
}
