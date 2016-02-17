using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.Wait;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command
{
    public class Commands : CloudFormationDictionary
    {
        public enum CommandType
        {
            None,
            Custom
        }

        public Commands(LaunchConfiguration resource) : base(resource)
        {
            Instance = resource;
        }

        [JsonIgnore]
        public LaunchConfiguration Instance { get; }

        public ConfigCommand AddCommand<T>(string key, CommandType commandType, params string[] data) where T : Resource.EC2.Instancing.Metadata.Config.Command.Command, new()
        {
            switch (commandType)
            {
                case CommandType.Custom:
                    return this.AddCommand<Resource.EC2.Instancing.Metadata.Config.Command.Command>(key);
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
        public ConfigCommand AddCommand<T>(WaitCondition waitCondition) where T : Resource.EC2.Instancing.Metadata.Config.Command.Command, new()
        {
            
            var returnValue = this.AddCommand<Resource.EC2.Instancing.Metadata.Config.Command.Command>($"SignalComplete{waitCondition.LogicalId}");
            var logFile = $"c:\\cfn\\log\\{waitCondition.LogicalId}.log";
            returnValue.Command = new FnJoin(FnJoinDelimiter.None,
                "cfn-signal.exe -e 0 \"",
                new ReferenceProperty(waitCondition.Handle),
                $"\">{logFile}");
            returnValue.Test = $"IF EXIST \"{logFile}\" EXIT /B 1";
            returnValue.WaitAfterCompletion = 0.ToString();
            
            return returnValue;
        }


        public ConfigCommand AddCommand<T>(string key) where T : Resource.EC2.Instancing.Metadata.Config.Command.Command,new()
        {
            char firstChar = key.ToCharArray()[0];
            if (firstChar >= '0' && firstChar <= '9')
            {
                throw new Exception(key);
            }
            key = this.Count().ToString().PadLeft(3, '0') + key;
            ConfigCommand newConfigCommand = new ConfigCommand(this.Instance, key);
            this.Add(key, newConfigCommand);
            newConfigCommand.Command = new T() {Parent = newConfigCommand };
            return newConfigCommand;
        }

        public ConfigCommand AddCommand<T>(string key,TimeSpan waitAfterCompletion, object test, params object[] commandText)
            where T : Resource.EC2.Instancing.Metadata.Config.Command.Command, new()
        {
            var returnValue = this.AddCommand<T>(key);
            returnValue.WaitAfterCompletion = waitAfterCompletion.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            if (test != null)
            {
                returnValue.Test = test;
            }
            returnValue.Command = commandText;
            return returnValue;
        }



    }
}
