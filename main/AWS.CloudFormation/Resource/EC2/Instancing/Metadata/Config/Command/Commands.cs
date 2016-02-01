﻿using System;
using System.ComponentModel;
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
            Custom,
            CompleteWaitHandle
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
                case CommandType.CompleteWaitHandle:
                    throw new NotImplementedException();
                    //var returnValue = this.AddCommand<Resource.EC2.Instancing.Metadata.Config.Command.Command>(key);
                    //returnValue.Command.AddCommandLine(true, "cfn-signal.exe -e 0 \"",
                    //                                    new ReferenceProperty(data[0]),
                    //                                    "\"");
                    //return returnValue;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
        public ConfigCommand AddCommand<T>(WaitCondition waitCondition) where T : Resource.EC2.Instancing.Metadata.Config.Command.Command, new()
        {
            var returnValue = this.AddCommand<Resource.EC2.Instancing.Metadata.Config.Command.Command>($"signalComplete{waitCondition.LogicalId}");
            returnValue.Command = new FnJoin(FnJoinDelimiter.None,
                "cfn-signal.exe -e 0 \"",
                new ReferenceProperty(waitCondition.Handle),
                "\"");
            return returnValue;
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
