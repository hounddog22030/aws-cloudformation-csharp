using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;

namespace AWS.CloudFormation.Instance.MetaData.Config.Command
{
    public class Commands : CloudFormationDictionary
    {
        public enum CommandType
        {
            None,
            Custom,
            CompleteWaitHandle
        }

        public Commands(Instance instance) : base(instance)
        {

        }
        //public ConfigCommand AddCommand(string key)
        //{
        //    return this.Add(key, new ConfigCommand(this.Instance)) as ConfigCommand;
        //}

        public CloudFormationDictionary AddCommand(string key, CommandType commandType)
        {
            switch (commandType)
            {
                case CommandType.Custom:
                    return this.Add(key).Add("command");
                case CommandType.CompleteWaitHandle:
                    var command = this.Add(key).Add("command");
                    command.SetFnJoin( "cfn-signal.exe -e 0 \"", new ReferenceProperty() { Ref = this.Instance.WaitConditionHandleName }, "\"");
                    return command;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        public ConfigCommand AddCommand<T>(string key) where T : Command,new()
        {
            ConfigCommand newConfigCommand = new ConfigCommand(this.Instance,key);
            this.Add(key, newConfigCommand);
            newConfigCommand.Command = new T() {Parent = newConfigCommand };
            return newConfigCommand;
        }



    }
}
