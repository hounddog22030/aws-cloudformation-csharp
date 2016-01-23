using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config
{
    public class Sources : CloudFormationDictionary
    {
        public Sources(Instance resource) : base(resource)
        {
            Instance = resource;
        }

        public ConfigFile GetFile(string filename)
        {
            if (this.ContainsKey(filename))
            {
                return this[filename] as ConfigFile;
            }
            else
            {
                return this.Add(filename, new ConfigFile(this.Instance)) as ConfigFile;
            }
        }

        public Instance Instance { get; }
    }

    public class Config : CloudFormationDictionary
    {
        public Config(Instance resource) : base(resource)
        {
            Commands = this.Add("commands", new Commands(resource)) as Commands;
            Files = this.Add("files", new Files(resource)) as Files;
            Services = this.Add("services", new CloudFormationDictionary(resource));
            Sources = this.Add("sources", new Sources(resource)) as Sources;
            Packages = this.Add("packages", new Packages(resource)) as Packages;
            this.Add("ignoreErrors", true);
        }

        [JsonIgnore]
        public Commands Commands { get; }

        [JsonIgnore]
        public Files Files { get; }

        public CloudFormationDictionary Services { get; }
        public Sources Sources { get;  }
        public Packages Packages { get; set; }

        public bool IgnoreErrors {
            get
            {
                return (bool)this["ignoreErrors"];
            }
            set { this["ignoreErrors"] = value; }
        }
    }

    public class Packages : CloudFormationDictionary
    {
        public Packages(Instance resource) : base(resource)
        {
        }

        public Package AddPackage(string type, string name, string source)
        {
            Package newPackage = new Package(this.Resource) {{ name, source}};
            this.Add(type, newPackage);
            return newPackage;
        }
    }

    public class Package : CloudFormationDictionary
    {
        public Package(ResourceBase resource) : base(resource)
        {
        }
    }
}
