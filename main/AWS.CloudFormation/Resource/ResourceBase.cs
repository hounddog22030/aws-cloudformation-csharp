using System;
using System.Collections.Generic;
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

            get
            {
                if (SupportsTags)
                {
                    var returnValue = this.Properties.GetValue<TagDictionary>();
                    if (returnValue == null)
                    {
                        this.Tags = new TagDictionary();
                        return this.Tags;
                    }
                    return returnValue;
                }
                else
                {
                    return null;
                }

            }
            set
            {
                if (SupportsTags)
                {
                    this.Properties.SetValue(value);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        [JsonArray]
        public class TagDictionary : List<Tag>
        {
            
        }


    }

    public class Tag
    {
        public Tag(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get;  }
        public string Value { get; }
    }
}
