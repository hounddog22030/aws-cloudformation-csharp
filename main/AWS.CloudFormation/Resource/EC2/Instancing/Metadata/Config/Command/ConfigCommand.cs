using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.AutoScaling;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command
{
    public class ConfigCommand : CloudFormationDictionary
    {
        public ConfigCommand(LaunchConfiguration resource, string key) : base(resource)
        {
            this.Name = key;
            this.WaitAfterCompletion = 0.ToString();
        }

        [JsonIgnore]
        public string Name { get; set; }

        public object Command
        {
            get
            {
                return this["command"];
            }
            set
            {
                this["command"]=value;
            }

        }

        public T GetCommand<T>()
        {
            return (T)this.Command;
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
