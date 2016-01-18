﻿using System.Collections.Generic;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;

using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource
{

    public interface ILogicalId
    {
        string LogicalId { get; }
    }
    public abstract class ResourceBase : ILogicalId
    {

        protected ResourceBase(Template template, string type, string name, bool supportsTags)
            //: this(type, name, supportsTags)
        {
            Type = type;
            Template = template;
            LogicalId = name;

            this.Template.AddResource(this);

            Properties = new CloudFormationDictionary();
            Metadata = new Metadata(this);

            if (supportsTags)
            {
                this.Tags = new TagDictionary();
                this.Tags.Add("Name", name);
            }
        }

        [JsonIgnore]
        internal Template Template { get; private set; }
        public string Type { get; private set; }

        public Metadata Metadata { get; }


        [JsonIgnore]
        public string LogicalId { get ; private set; }

        public string[] DependsOn { get; protected set; }

        public CloudFormationDictionary Properties { get; }

        [JsonIgnore]
        public TagDictionary Tags
        {

            get { return this.Properties.GetValue<TagDictionary>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonArray]
        public class TagDictionary : Dictionary<string, string>
        {
            
        }
    }
}
