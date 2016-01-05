using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Instance.Metadata.Config.Command;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Instance.Metadata.Config
{
    public class Sources : CloudFormationDictionary
    {
        public Sources(Instance resource) : base(resource)
        {
            Instance = resource;
        }

        public ConfigFile GetFile(string filename)
        {
            if (this.ContainsKey(filename))
            {
                return this[filename] as ConfigFile;
            }
            else
            {
                return this.Add(filename, new ConfigFile(this.Instance)) as ConfigFile;
            }
        }

        public Instance Instance { get; }
    }

    public class Config : CloudFormationDictionary
    {
        public Config(Instance resource) : base(resource)
        {
            Commands = this.Add("commands", new Commands(resource)) as Commands;
            Files = this.Add("files", new Files(resource)) as Files;
            Services = this.Add("services", new CloudFormationDictionary(resource));
            Sources = this.Add("sources", new Sources(resource)) as Sources;
        }

        [JsonIgnore]
        public Commands Commands { get; }

        [JsonIgnore]
        public Files Files { get; }

        public CloudFormationDictionary Services { get; }
        public Sources Sources { get;  }


    }
}
