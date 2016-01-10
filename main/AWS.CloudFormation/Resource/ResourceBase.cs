using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Instance.Metadata;
using AWS.CloudFormation.Instance.Metadata.Config.Command;
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
        //protected ResourceBase(string type)
        //{
        //}

        //protected ResourceBase(string type, string name, bool supportsTags) : this(type)
        //{
        //}

        [CloudFormationPropertiesAttribute]
        public List<KeyValuePair<string, string>> Tags { get; private set; }

        protected ResourceBase(Template template, string type, string name, bool supportsTags)
            //: this(type, name, supportsTags)
        {
            Type = type;
            Template = template;
            Name = name;
            Metadata = new Resource.Metadata(this);

            if (supportsTags)
            {
                this.Tags = new List<KeyValuePair<string, string>>();
                this.AddTag("Name", name);
            }
        }

        [JsonIgnore]
        internal Template Template { get; private set; }
        public string Type { get; private set; }

        public Resource.Metadata Metadata { get; }

        public KeyValuePair<string, string> AddTag(string key, string value)
        {
            var returnValue = new KeyValuePair<string, string>(key, value);
            this.Tags.Add(returnValue);
            return returnValue;
        }

        [JsonIgnore]
        public string Name { get ; private set; }

        public string[] DependsOn { get; protected set; }
    }
}
