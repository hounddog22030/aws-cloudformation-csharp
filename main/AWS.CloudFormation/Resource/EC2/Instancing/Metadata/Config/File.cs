using System;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.AutoScaling;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config
{
    public class ConfigFile : CloudFormationDictionary
    {
        public ConfigFile(LaunchConfiguration resource) : base(resource)
        {
            
        }

        private ConfigFileContent _content = null;
        public ConfigFileContent Content {
            get
            {
                if (_content == null)
                {
                    _content = (ConfigFileContent)this.Add("content", new ConfigFileContent(this.ResourceRef));
                }
                return _content;
            }
        }

        public string Source
        {
            get
            {
                if (this.ContainsKey("source"))
                {
                    return this["source"] as string;
                }
                return null;
            }
            set
            {
                if (this.ContainsKey("source"))
                {
                    this["source"] = value;
                }
                else
                {
                    this.Add("source", value);
                }
            }
        }

        public string Authentication
        {
            get
            {
                if (this.ContainsKey("authentication"))
                {
                    return this["authentication"] as string;
                }
                return null;
            }
            set
            {
                if (this.ContainsKey("authentication"))
                {
                    this["authentication"] = value;
                }
                else
                {
                    this.Add("authentication", value);
                }
            }
        }
    }

    public class ConfigFileContent : CloudFormationDictionary
    {
        public ConfigFileContent(ResourceBase resource) : base(resource)
        {
        }

        public CloudFormationDictionary AddNode(string name)
        {
            var returnValue = new CloudFormationDictionary(this.ResourceRef);
            this.Add(name, returnValue);
            return returnValue;

        }

    }
}
