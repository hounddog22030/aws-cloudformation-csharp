using System.Configuration;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.AutoScaling;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata
{
    public class ConfigSet : CloudFormationDictionary
    {
        public ConfigSet()
        {
            
        }
        public ConfigSet(LaunchConfiguration resource) : base(resource)
        {
            Instance = resource;
        }

        public Resource.EC2.Instancing.Metadata.Config.Config GetConfig(string configName)
        {
            if (this.ContainsKey(configName))
            {
                return this[configName] as Resource.EC2.Instancing.Metadata.Config.Config;
            }
            else
            {
                if (!this.Instance.Metadata.Authentication.ContainsKey("S3AccessCreds"))
                {
                    var appSettingsReader = new AppSettingsReader();
                    string accessKeyString = (string)appSettingsReader.GetValue("S3AccessKey", typeof(string));
                    string secretKeyString = (string)appSettingsReader.GetValue("S3SecretKey", typeof(string));
                    var auth = this.Instance.Metadata.Authentication.Add("S3AccessCreds", new S3Authentication(accessKeyString, secretKeyString, new string[] { "gtbb" }));
                    auth.Type = "S3";
                }
                return this.Add(configName, new Resource.EC2.Instancing.Metadata.Config.Config(this.Instance)) as Resource.EC2.Instancing.Metadata.Config.Config;
            }
        }

        public LaunchConfiguration Instance { get; internal set; }
    }

}
