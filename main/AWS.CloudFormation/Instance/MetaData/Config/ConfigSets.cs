using AWS.CloudFormation.Common;

namespace AWS.CloudFormation.Instance.Metadata.Config
{
    public class ConfigSets : CloudFormationDictionary
    {
        public ConfigSets(Instance instance) : base(instance)
        {
        }

        public ConfigSet GetConfigSet(string configSetName)
        {
            if (this.ContainsKey(configSetName))
            {
                return this[configSetName] as ConfigSet;
            }
            else
            {
                Instance resourceAsInstance = (Instance)this.Instance;
                ConfigSet returnValue = this.Add(configSetName, new ConfigSet((Instance)this.Instance)) as ConfigSet;
                resourceAsInstance.SetUserData();
                resourceAsInstance.EnableHup();
                return returnValue;
            }
        }
    }
}
