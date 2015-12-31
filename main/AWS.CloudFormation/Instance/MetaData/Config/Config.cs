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
        public Config(Instance resource) : base(resource)
        {
            Commands = this.Add("commands", new Commands(resource)) as Commands;
            Files = this.Add("files", new Files(resource)) as Files;
            Services = this.Add("services", new CloudFormationDictionary(resource));
        }

        [JsonIgnore]
        public Commands Commands { get; }

        [JsonIgnore]
        public Files Files { get; }

        public CloudFormationDictionary Services { get; }

    }
}
