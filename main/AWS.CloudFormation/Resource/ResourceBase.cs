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
                this.Tags = new CloudFormationDictionary();
                this.AddTag("Name", name);
            }
        }

        [JsonIgnore]
        internal Template Template { get; private set; }
        public string Type { get; private set; }

        public Metadata Metadata { get; }

        public void AddTag(string key, string value)
        {
            this.Tags.Add(key, value);
        }

        [JsonIgnore]
        public string LogicalId { get ; private set; }

        public string[] DependsOn { get; protected set; }

        //[JsonConverter(typeof(CloudFormationDictionaryConverter))]
        public CloudFormationDictionary Properties { get; }

        [JsonIgnore]
        public CloudFormationDictionary Tags
        {
            get { return this.Properties.GetValue<CloudFormationDictionary>(); }
            set { this.Properties.SetValue(value); }
        }
    }
}
