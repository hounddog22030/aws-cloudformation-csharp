using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Instance.Metadata.Config;

namespace AWS.CloudFormation.Instance.Metadata
{
    public class ConfigSet : CloudFormationDictionary
    {
        public ConfigSet(Resource.EC2.Instance resource) : base(resource)
        {
            Instance = resource;
        }

        public Resource.EC2.Instancing.Metadata.Config.Config GetConfig(string configName)
        {
            if (this.ContainsKey(configName))
            {
                return this[configName] as Resource.EC2.Instancing.Metadata.Config.Config;
            }
            else
            {
                return this.Add(configName, new Resource.EC2.Instancing.Metadata.Config.Config(this.Instance)) as Resource.EC2.Instancing.Metadata.Config.Config;
            }
        }

        public Resource.EC2.Instance Instance { get; }
    }

}
