using System.ComponentModel;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command
{
    public class Commands : CloudFormationDictionary
    {
        public enum CommandType
        {
            None,
            Custom,
            CompleteWaitHandle
        }

        public Commands(Instance resource) : base(resource)
        {
            Instance = resource;
        }

        [JsonIgnore]
        public Instance Instance { get; }

        public ConfigCommand AddCommand<T>(string key, CommandType commandType) where T : Resource.EC2.Instancing.Metadata.Config.Command.Command, new()
        {
            switch (commandType)
            {
                case CommandType.Custom:
                    return this.AddCommand<Resource.EC2.Instancing.Metadata.Config.Command.Command>(key); 
                case CommandType.CompleteWaitHandle:
                    var returnValue = this.AddCommand<Resource.EC2.Instancing.Metadata.Config.Command.Command>(key);
                    returnValue.Command.AddCommandLine( true, "cfn-signal.exe -e 0 \"", 
                                                        new ReferenceProperty() { Ref = this.Instance.WaitConditionHandleName }, 
                                                        "\"");
                    return returnValue;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        public ConfigCommand AddCommand<T>(string key) where T : Resource.EC2.Instancing.Metadata.Config.Command.Command,new()
        {
            ConfigCommand newConfigCommand = new ConfigCommand(this.Instance, key);
            this.Add(key, newConfigCommand);
            newConfigCommand.Command = new T() {Parent = newConfigCommand };
            return newConfigCommand;
        }



    }
}
