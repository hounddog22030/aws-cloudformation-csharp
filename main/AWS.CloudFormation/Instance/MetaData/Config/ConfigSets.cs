﻿using AWS.CloudFormation.Common;

namespace AWS.CloudFormation.Instance.Metadata.Config
{
    public class ConfigSets : CloudFormationDictionary
    {
        public ConfigSets(Instance resource) : base(resource)
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

        public Instance Instance { get; }
    }
}
