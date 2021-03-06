﻿using System;
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

        public Instance(Subnet subnet, InstanceTypes instanceType, string imageId,
            OperatingSystem operatingSystem, Ebs.VolumeTypes volumeType, uint volumeSize)
            : this(subnet, instanceType, imageId, operatingSystem)
        {
            this.AddDisk(volumeType, volumeSize, this.GetRootDeviceId(),true);
        }
        private string GetRootDeviceId()
        {
            switch (OperatingSystem)
            {
                case OperatingSystem.Windows:
                    return "/dev/sda1";
                case OperatingSystem.Linux:
                    return "/dev/xvda";
                default:

                    throw new NotImplementedException();
            }
        }

        public Instance(Subnet subnet, InstanceTypes instanceType, string imageId, OperatingSystem operatingSystem,
            bool rename) : base(subnet, instanceType, imageId, operatingSystem, ResourceType.AwsEc2Instance,rename)
        {
            SourceDestCheck = true;
            NetworkInterfaces = new List<NetworkInterface>();
            this.Tags.Add(new Tag("Name", this.LogicalId));
            this.VolumesToAttach = new List<Volume>();
            this.SecurityGroupIds = new List<ReferenceProperty>();
        }


        public Instance(Subnet subnet, InstanceTypes instanceType, string imageId, OperatingSystem operatingSystem)
            : this(subnet, instanceType, imageId, operatingSystem, true)
        {
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

        private List<NetworkInterface> _networkInterfaces;

        [JsonIgnore]
        public List<NetworkInterface> NetworkInterfaces
        {
            get
            {
                if (this.Type == ResourceType.AwsEc2Instance)
                {
                    return this.Properties.GetValue<List<NetworkInterface>>();
                }
                else
                {
                    return _networkInterfaces;
                }
                
            }
            set
            {
                if (this.Type == ResourceType.AwsEc2Instance)
                {
                    this.Properties.SetValue(value);
                }
                else
                {
                    _networkInterfaces = value;
                }
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
            ElasticIp = new ElasticIp(this);
            this.Template.Resources.Add(ElasticIp.LogicalId,ElasticIp);
            return ElasticIp;
        }


        [JsonIgnore]
        public string CidrBlock {
            get { return this.PrivateIpAddress + "/32"; }
            set
            {
                throw new ReadOnlyException();
            }
        }

        public void AddDisk(Volume volume)
        {
            if (this.Template == null)
            {
                this.VolumesToAttach.Add(volume);
            }
            else
            {
                this.AttachVolume(volume);
            }
        }

        [JsonIgnore]
        private List<Volume> VolumesToAttach { get; set; }

        protected override void OnTemplateSet(Template template)
        {
            base.OnTemplateSet(template);
            foreach (var volume in this.VolumesToAttach)
            {
                this.AttachVolume(volume);
            }
            this.VolumesToAttach.Clear();
        }

        private void AttachVolume(Volume volume)
        {
            if (volume.AvailabilityZone == AvailabilityZone.None)
            {
                volume.AvailabilityZone = this.Subnet.AvailabilityZone;
            }
            string deviceId = string.Empty;

            switch (volume.AttachmentType)
            {
                    case VolumeAttachmentType.Extension:
                    deviceId = this.GetAvailableDevice();
                    break;
                    case VolumeAttachmentType.Root:
                    deviceId = this.RootDeviceId;
                    break;
                default:
                    throw new NotSupportedException(volume.AttachmentType.ToString());
            }
            VolumeAttachment attachment = new VolumeAttachment(deviceId, this, volume);
            this.Template.Resources.Add(attachment.LogicalId, attachment);
        }

        [JsonIgnore]
        public List<SsmAssociation> SsmAssociations
        {
            get
            {
                if (this.Properties.GetValue<List<SsmAssociation>>() == null)
                {
                    this.SsmAssociations = new List<SsmAssociation>();
                }
                return this.Properties.GetValue<List<SsmAssociation>>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public List<ReferenceProperty> SecurityGroupIds
        {
            get
            {
                return this.Properties.GetValue<List<ReferenceProperty>>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

    }

    public class SsmAssociation
    {
        public SsmAssociation(object documentName)
        {
            DocumentName = documentName;
        }

        public object DocumentName { get; }
    }
}
