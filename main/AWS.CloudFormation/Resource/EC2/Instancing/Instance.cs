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

        public Instance(Subnet subnet, InstanceTypes instanceType, string imageId,
            OperatingSystem operatingSystem, Ebs.VolumeTypes volumeType, int volumeSize)
            : this(subnet, instanceType, imageId, operatingSystem)
        {
            this.AddDisk(volumeType, volumeSize);
        }


        public Instance(Subnet subnet, InstanceTypes instanceType, string imageId, OperatingSystem operatingSystem)
            : base(subnet,instanceType, imageId, operatingSystem, ResourceType.AwsEc2Instance)
        {
            SourceDestCheck = true;
            NetworkInterfaces = new List<NetworkInterface>();
            this.Tags.Add( new Tag("Name", this.LogicalId));
            this.VolumesToAttach = new List<Volume>(); 
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
            VolumeAttachment attachment = new VolumeAttachment(this.GetAvailableDevice(), this, volume);
            this.Template.Resources.Add(attachment.LogicalId, attachment);
        }
    }
}
