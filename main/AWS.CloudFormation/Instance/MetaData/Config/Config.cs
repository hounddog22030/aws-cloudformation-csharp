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
    public class Config : CloudFormationDictionary
    {
        public Config(Instance instance) : base(instance)
        {
            Commands = this.Add("commands", new Commands(instance)) as Commands;
            Files = this.Add("files", new Files(instance)) as Files;
            Services = this.Add("services", new CloudFormationDictionary(instance));
        }

        [JsonIgnore]
        public Commands Commands { get; }

        [JsonIgnore]
        public Files Files { get; }

        public CloudFormationDictionary Services { get; }

    }
}
