using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;

namespace AWS.CloudFormation.Instance.MetaData
{
    public class ConfigSet : CloudFormationDictionary
    {
        public ConfigSet(Instance instance) : base(instance)
        {
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


    }

}
