using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;

namespace AWS.CloudFormation.Instance.Metadata.Config
{
    public class ConfigFile : CloudFormationDictionary
    {
        public ConfigFile(Instance resource) : base(resource)
        {
            Content = this.Add("content", new CloudFormationDictionary(resource));
        }

        public CloudFormationDictionary Content { get; }

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
    }
}
