using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.AutoScaling;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata
{
    public class ConfigSet : CloudFormationDictionary
    {
        public ConfigSet()
        {
            
        }
        public ConfigSet(LaunchConfiguration resource) : base(resource)
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

        public LaunchConfiguration Instance { get; internal set; }
    }

}
