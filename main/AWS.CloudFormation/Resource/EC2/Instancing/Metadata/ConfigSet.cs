using AWS.CloudFormation.Common;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata
{
    public class ConfigSet : CloudFormationDictionary
    {
        public ConfigSet(Instance resource) : base(resource)
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

        public Instance Instance { get; }
    }

}
