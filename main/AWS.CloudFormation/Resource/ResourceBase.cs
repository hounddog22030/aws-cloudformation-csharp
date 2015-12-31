using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Instance.MetaData;
using AWS.CloudFormation.Instance.MetaData.Config.Command;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Resource.Wait;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OperatingSystem = AWS.CloudFormation.Instance.OperatingSystem;

namespace AWS.CloudFormation.Resource
{

    public interface IName
    {
        string Name { get; }
    }
    [JsonConverter(typeof(ResourceJsonConverter))]
    public abstract class ResourceBase : IName
    {
        protected ResourceBase(string type)
        {
            Type = type;
        }

        protected ResourceBase(string type, string name, bool supportsTags) : this(type)
        {
            Name = name;
            if (supportsTags)
            {
                this.Tags = new List<KeyValuePair<string, string>>();
                this.AddTag("Name", name);
            }
        }

        [CloudFormationPropertiesAttribute]
        public List<KeyValuePair<string, string>> Tags { get; private set; }

        protected ResourceBase(Template template, string type, string name, bool supportsTags)
            : this(type, name, supportsTags)
        {
            Template = template;
        }

        [JsonIgnore]
        protected Template Template { get; private set; }
        public string Type { get; private set; }


        public KeyValuePair<string, string> AddTag(string key, string value)
        {
            var returnValue = new KeyValuePair<string, string>(key, value);
            this.Tags.Add(returnValue);
            return returnValue;
        }

        [JsonIgnore]
        public string Name { get ; private set; }

        public void AddDependsOn(Instance.Instance dependsOn, TimeSpan timeout)
        {
            if (dependsOn.OperatingSystem != OperatingSystem.Windows)
            {
                throw new NotSupportedException($"Cannot depend on instance of OperatingSystem:{dependsOn.OperatingSystem}");
            }

            if (!string.IsNullOrEmpty(this.DependsOn))
            {
                throw new NotSupportedException($"Already DependsOn:{this.DependsOn}");
            }

            var finalizeConfig = dependsOn.Metadata.Init.ConfigSets.GetConfigSet(Init.FinalizeConfigSetName).GetConfig(Init.FinalizeConfigName);

            var command = finalizeConfig.Commands.AddCommand<Command>("a-signal-success", Commands.CommandType.CompleteWaitHandle);
            command.WaitAfterCompletion = 0.ToString();

            WaitCondition wait = new WaitCondition(Template, dependsOn.WaitConditionName, timeout);
            Template.Resources.Add(wait.Name, wait);
            this.DependsOn = dependsOn.WaitConditionName;
        }

        public string DependsOn { get; private set; }


    }
}
