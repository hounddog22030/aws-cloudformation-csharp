using System.Collections.Generic;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Serializer;
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
                this.Tags = new List<KeyValuePair<string, string>>();
                this.AddTag("Name", name);
            }
        }

        [JsonIgnore]
        internal Template Template { get; private set; }
        public string Type { get; private set; }

        public Metadata Metadata { get; }

        public KeyValuePair<string, string> AddTag(string key, string value)
        {
            //var returnValue = new KeyValuePair<string, string>(key, value);
            //this.Tags.Add(returnValue);
            //return returnValue;
            return new KeyValuePair<string, string>("Name","X");
        }

        [JsonIgnore]
        public string LogicalId { get ; private set; }

        public string[] DependsOn { get; protected set; }

        //[JsonConverter(typeof(CloudFormationDictionaryConverter))]
        public CloudFormationDictionary Properties { get; }
        [CloudFormationProperties]

        [JsonIgnore]
        public List<KeyValuePair<string, string>> Tags 
        {
            get { return null;  }
            set { }
            //get { return (List<KeyValuePair<string, string>>)this.Properties.GetValue(); }
            //set { this.Properties.SetValue(value); }
        }
    }
}
