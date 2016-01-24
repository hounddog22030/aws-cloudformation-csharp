using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.AutoScaling;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config
{
    public class Files : CloudFormationDictionary
    {
        public Files(LaunchConfiguration resource) : base(resource)
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

        public LaunchConfiguration Instance { get; }
    }
}
