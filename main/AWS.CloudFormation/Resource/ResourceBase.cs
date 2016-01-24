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

        protected ResourceBase(Template template, string name)
            //: this(type, name, supportsTags)
        {
            Template = template;
            LogicalId = name;
            DependsOn2 = new List<string>();

            this.Template.Resources.Add(name,this);

            Properties = new CloudFormationDictionary();
            Metadata = new Metadata(this);

            // ReSharper disable once VirtualMemberCallInContructor
            if (SupportsTags)
            {
                this.Tags = new TagDictionary {{"Name", name}};
            }
        }

        protected abstract bool SupportsTags { get; }

        [JsonIgnore]
        internal Template Template { get; private set; }

        public abstract string Type { get;  }

        public Metadata Metadata { get; }


        [JsonIgnore]
        public string LogicalId { get ; private set; }


        [JsonIgnore]
        public List<string> DependsOn2 { get; }

        public string[] DependsOn { get { return this.DependsOn2.ToArray(); } }

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
