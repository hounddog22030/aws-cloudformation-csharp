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
        public ConfigSet(Instance resource) : base(resource)
        {
            Instance = resource;
        }

        public Config.Config GetConfig(string configName)
        {
            if (this.ContainsKey(configName))
            {
                return this[configName] as Config.Config;
            }
            else
            {
                return this.Add(configName, new Config.Config(this.Instance)) as Config.Config;
            }
        }

        public Instance Instance { get; }
    }

}
