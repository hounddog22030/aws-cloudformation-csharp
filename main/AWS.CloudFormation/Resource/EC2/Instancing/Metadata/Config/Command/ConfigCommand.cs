using AWS.CloudFormation.Common;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command
{
    public class ConfigCommand : CloudFormationDictionary
    {
        public ConfigCommand(Instance resource, string key) : base(resource)
        {
            this.Name = key;
            this.WaitAfterCompletion = 0.ToString();
        }

        [JsonIgnore]
        public string Name { get; set; }

        public Resource.EC2.Instancing.Metadata.Config.Command.Command Command
        {
            get
            {
                if (this.ContainsKey("command"))
                {
                    return this["command"] as Resource.EC2.Instancing.Metadata.Config.Command.Command;
                }
                else
                {
                    return this.Add("command", new Resource.EC2.Instancing.Metadata.Config.Command.Command()) as Resource.EC2.Instancing.Metadata.Config.Command.Command;
                }
            }
            set
            {
                this["command"]=value;
            }

        }

        public string WaitAfterCompletion
        {
            get
            {
                if (this.ContainsKey("waitAfterCompletion"))
                {
                    return (string)this["waitAfterCompletion"];
                }
                return null;
            }
            set
            {
                if (this.ContainsKey("waitAfterCompletion"))
                {
                    this["waitAfterCompletion"] = value;
                }
                else
                {
                    this.Add("waitAfterCompletion", value);
                }
            }
        }

        public string Test
        {
            get
            {
                if (this.ContainsKey("test"))
                {
                    return (string)this["test"];
                }
                return null;
            }
            set
            {
                if (this.ContainsKey("test"))
                {
                    this["test"] = value;
                }
                else
                {
                    this.Add("test", value);
                }
            }
        }
    }
}
