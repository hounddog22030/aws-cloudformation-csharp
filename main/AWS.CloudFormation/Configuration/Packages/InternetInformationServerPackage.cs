using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.AutoScaling;

namespace AWS.CloudFormation.Configuration.Packages
{
    public class InternetInformationServerPackage : PackageChef
    {
        
        public InternetInformationServerPackage(string snapshotId, string bucketName, string cookbookName): base(snapshotId, bucketName, cookbookName)
        {
        }

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);

            var secondConfigSetName = $"ConfigSet{this.ConfigName}HttpToHttps";
            var secondConfigName = $"Config{this.ConfigName}HttpToHttps";
            var secondConfig =
                configuration.Metadata.Init.ConfigSets.GetConfigSet(secondConfigSetName).GetConfig(secondConfigName);

            var msiUri = new Uri("https://s3.amazonaws.com/gtbb/software/rewrite_amd64.msi");

            var fileName = System.IO.Path.GetFileNameWithoutExtension(msiUri.AbsolutePath).Replace(".", string.Empty).Replace("-", String.Empty);
            var msi = new CloudFormationDictionary();
            msi.Add(fileName, msiUri.AbsoluteUri);
            secondConfig.Packages.Add("msi", msi);
            secondConfig.Files.GetFile("c:/inetpub/wwwroot/web.config").Source = "https://s3.amazonaws.com/gtbb/web.config";
            secondConfig.Files.GetFile("c:/inetpub/wwwroot/healthcheck.htm").Source = "https://s3.amazonaws.com/gtbb/healthcheck.htm";
        }
    }
}
