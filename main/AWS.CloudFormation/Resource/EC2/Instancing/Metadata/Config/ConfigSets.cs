using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.AutoScaling;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config
{
    public class ConfigSets : CloudFormationDictionary
    {
        public ConfigSets(LaunchConfiguration resource) : base(resource)
        {
            Instance = resource;
        }

        public ConfigSet GetConfigSet(string configSetName)
        {
            if (this.ContainsKey(configSetName))
            {
                return this[configSetName] as ConfigSet;
            }
            else
            {
                ConfigSet returnValue = this.Add(configSetName, new ConfigSet(this.Instance)) as ConfigSet;
                this.Instance.SetUserData();
                this.Instance.EnableHup();
                return returnValue;
            }
        }

        public LaunchConfiguration Instance { get; }
    }
}
