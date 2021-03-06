﻿using System.Linq;
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

        public T GetConfigSet<T>(string configSetName) where T: ConfigSet, new()
        {
            
            if (this.ContainsKey(configSetName))
            {
                return this[configSetName] as T;
            }
            else
            {
                T returnValue = this.Add(configSetName, new T()) as T;
                returnValue.Instance = this.Instance;
                this.Instance.SetUserData();
                this.Instance.EnableHup();
                return returnValue;
            }

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
