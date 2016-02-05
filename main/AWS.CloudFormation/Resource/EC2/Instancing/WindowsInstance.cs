using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Configuration.Packages;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Wait;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Instancing
{
    public class WindowsInstance : Instance
    {


        
        public WindowsInstance( string name,
                                InstanceTypes instanceType,
                                string imageId,
                                Subnet subnet,
                                Ebs.VolumeTypes volumeType,
                                uint volumeSize)
            : this(name, instanceType, imageId, subnet)
        {
            this.AddBlockDeviceMapping("/dev/sda1", volumeSize, volumeType);
        }


        public WindowsInstance(string name, InstanceTypes instanceType, string imageId, Subnet subnet)
            : this(name, instanceType, imageId,DefinitionType.Instance)
        {
            if (subnet != null)
            {
                this.Subnet = subnet;
            }

        }
        public WindowsInstance(string name, InstanceTypes instanceType, string imageId, 
            DefinitionType definitionType): base(instanceType, imageId, OperatingSystem.Windows, true, definitionType)
        {
            if (name.Length > NetBiosMaxLength)
            {
                throw new InvalidOperationException($"Name length is limited to {NetBiosMaxLength} characters.");
            }
        }

        protected internal virtual void OnAddedToDomain(string domainName)
        {

            var nodeJson = this.GetChefNodeJsonContent();
            nodeJson.Add("domain", domainName);
        }

        public T AddPackage<T>() where T :PackageBase<ConfigSet>, new()
        {
            T package = new T();
            return package;
        }




    }
}
