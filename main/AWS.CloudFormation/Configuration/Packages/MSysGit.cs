using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;

namespace AWS.CloudFormation.Configuration.Packages
{
    public class MSysGit: PackageBase<ConfigSet>
    {

        public MSysGit(string bucketName) : base(null,null,bucketName)
        {
            
        }
        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            const string fileName = "Git-2.7.1.2-64-bit.exe";
            const string filePath = "c:/cfn/scripts/" + fileName;

            this.Config.Files.Add(filePath)
                .Add("source", $"https://s3.amazonaws.com/{this.BucketName}/software/{fileName}");


            this.Config.Commands.AddCommand<Command>(fileName.Replace('.','-')).Command = $"{filePath} /silent";
        }
    }
}
