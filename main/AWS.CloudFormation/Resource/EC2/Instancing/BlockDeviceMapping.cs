using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;

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
        

        public string VolumeSize
        {
            get
            {
                if (this.ContainsKey("VolumeSize"))
                {
                    return (string)this["VolumeSize"];
                }
                return null;
            }
            set { this["VolumeSize"] = value; }
        }
        public string VolumeType
        {
            get
            {
                if (this.ContainsKey("VolumeType"))
                {
                    return (string)this["VolumeType"];
                }
                return null;
            }
            set { this["VolumeType"] = value; }
        }
    }
}
