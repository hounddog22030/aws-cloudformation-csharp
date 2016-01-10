using AWS.CloudFormation.Common;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Instance.Metadata.Config.Command
{
    public class ConfigCommand : CloudFormationDictionary
    {
        public ConfigCommand(Resource.EC2.Instance resource, string key) : base(resource)
        {
            this.Name = key;
            this.WaitAfterCompletion = 0.ToString();
        }

        [JsonIgnore]
        public string Name { get; set; }

        public Command Command
        {
            get
            {
                if (this.ContainsKey("command"))
                {
                    return this["command"] as Command;
                }
                else
                {
                    return this.Add("command", new Command()) as Command;
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
