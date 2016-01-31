using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Stack;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.Wait;

namespace AWS.CloudFormation.Configuration.Packages
{


    public abstract class PackageBase
    {
        public readonly static TimeSpan TimeoutMax = new TimeSpan(12,0,0);
        private PackageBase(LaunchConfiguration instance)
        {
            this.Instance = instance;
        }
        protected PackageBase(LaunchConfiguration instance, Uri msi) : this(instance)
        { 
            Msi = msi;
        }
        protected PackageBase(LaunchConfiguration instance, string snapshotId) : this(instance)
        {
            SnapshotId = snapshotId;
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(this.Instance , this.Instance.GetAvailableDevice());
            blockDeviceMapping.Ebs.SnapshotId = this.SnapshotId;
            this.Instance.AddBlockDeviceMapping(blockDeviceMapping);
        }

        internal void AddConfiguration(Instance instance)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(Msi.AbsolutePath);
            var configSet = instance.Metadata.Init.ConfigSets.GetConfigSet(fileName).GetConfig(fileName);
            configSet.Packages.Add("msi",Msi.AbsoluteUri);
        }


        public LaunchConfiguration Instance { get; }

        public Uri Msi { get; }


        public string SnapshotId { get; }


    }

    public abstract class PackageChef : PackageBase
    {
        private WindowsInstance sqlServer;
        private string v1;
        private string v2;

        public PackageChef(LaunchConfiguration instance, string snapshotId, string bucketName, string cookbookName, string recipeName) : base(instance,snapshotId)
        {
            CookbookName = cookbookName;
            BucketName = bucketName;
            if (string.IsNullOrEmpty(recipeName))
            {
                recipeName = "default";
            }
            RecipeList = $"{CookbookName}::{recipeName}";
            this.AddChefExec();
        }

        public string BucketName { get; }

        public PackageChef(LaunchConfiguration instance, string snapshotId, string bucketName, string cookbookName) : this(instance,snapshotId, bucketName, cookbookName, null)
        {
        }

        public string CookbookName { get; }
        public string RecipeList { get; private set; }

        private WaitCondition _waitCondition = null;

        public WaitCondition WaitCondition
        {
            get
            {
                if (_waitCondition==null)
                {
                    _waitCondition = new WaitCondition(this.Instance.Template, $"waitCondition{this.Instance.LogicalId}{CookbookName}{RecipeList}".Replace(".", string.Empty).Replace(":", string.Empty), TimeoutMax);
                    this.ChefConfig.Commands.AddCommand<Command>(_waitCondition);
                }
                return _waitCondition;
            }

        }

        protected Resource.EC2.Instancing.Metadata.Config.Config ChefConfig
        {
            get
            {
                return this.Instance.Metadata.Init.ConfigSets.GetConfigSet(RecipeList.Replace(":", string.Empty)).GetConfig("run");

            }
        }

        public void AddChefExec()
        {

            if (!this.Instance.Metadata.Authentication.ContainsKey("S3AccessCreds"))
            {
                var appSettingsReader = new AppSettingsReader();
                string accessKeyString = (string)appSettingsReader.GetValue("S3AccessKey", typeof(string));
                string secretKeyString = (string)appSettingsReader.GetValue("S3SecretKey", typeof(string));
                var auth = this.Instance.Metadata.Authentication.Add("S3AccessCreds", new S3Authentication(accessKeyString, secretKeyString, new string[] { BucketName }));
                auth.Type = "S3";
                var chefConfigContent = this.Instance.GetChefNodeJsonContent();
                var s3FileNode = chefConfigContent.Add("s3_file");
                s3FileNode.Add("key", accessKeyString);
                s3FileNode.Add("secret", secretKeyString);
            }
            //



            
            this.ChefConfig.Packages.AddPackage("msi", "chef", "https://opscode-omnibus-packages.s3.amazonaws.com/windows/2012r2/i386/chef-client-12.6.0-1-x86.msi");
            var chefCommandConfig = this.ChefConfig.Commands.AddCommand<Command>(RecipeList.Replace(':', '-'));

            var clientRbFileKey = $"c:/chef/{CookbookName}/client.rb";
            this.ChefConfig.Files.GetFile(clientRbFileKey).Content.SetFnJoin($"cache_path 'c:/chef'\ncookbook_path 'c:/chef/{CookbookName}/cookbooks'\nlocal_mode true\njson_attribs 'c:/chef/node.json'\n");
            this.ChefConfig.Sources.Add($"c:/chef/{CookbookName}/", $"https://{BucketName}.s3.amazonaws.com/{CookbookName}.tar.gz");

            chefCommandConfig.Command.SetFnJoin($"C:/opscode/chef/bin/chef-client.bat -z -o {RecipeList} -c c:/chef/{CookbookName}/client.rb");
        }
    }

    public class VisualStudio : PackageChef
    {

        public VisualStudio(LaunchConfiguration instance, string bucketName) : base(instance, "snap-5e27a85a", bucketName, "vs")
        {
        }
    }
    public class SqlServerExpress : PackageChef
    {
        public SqlServerExpress(WindowsInstance instance, string bucketName) : base(instance, "snap-2cf80f29", bucketName, "sqlserver")
        {
            instance.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 20);
            instance.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 10);
            instance.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 10);
            var node = instance.GetChefNodeJsonContent();
            var sqlServerNode =  node.Add("sqlserver");
            sqlServerNode.Add("SQLUSERDBDIR", "d:\\SqlUserDb");
            sqlServerNode.Add("SQLUSERDBLOGDIR", "e:\\SqlUserDbLog");
            sqlServerNode.Add("INSTALLSQLDATADIR", "f:\\SqlData");
        }
    }

    public abstract class TeamFoundationServer : PackageChef
    {
        public TeamFoundationServer(WindowsInstance instance, string bucketName, string recipeName) : base (instance,"snap-4e69d94b", bucketName,"tfs", recipeName)
        {
        }
    }

    public class TeamFoundationServerApplicationTier : TeamFoundationServer
    {
        public TeamFoundationServerApplicationTier(WindowsInstance tfsServer,string bucketName) : base(tfsServer, bucketName,  "applicationtier")
        {

        }
    }
    public class TeamFoundationServerBuildServer : TeamFoundationServer
    {
        public TeamFoundationServerBuildServer(WindowsInstance buildServer, WindowsInstance applicationServer, string bucketName) : base(buildServer, bucketName, "build")
        {
            var node = buildServer.GetChefNodeJsonContent();
            var tfsNode = node.Add("tfs");
            tfsNode.Add("application_server_netbios_name", applicationServer.LogicalId);

        }
    }
    public class TeamFoundationServerBuildServerAgentOnly : TeamFoundationServer
    {
        public TeamFoundationServerBuildServerAgentOnly(WindowsInstance buildServer, WindowsInstance applicationServer, string bucketName) : base(buildServer, bucketName, "agent")
        {
            var node = buildServer.GetChefNodeJsonContent();
            var tfsNode = node.Add("tfs");
            tfsNode.Add("application_server_netbios_name", applicationServer.LogicalId);

        }
    }

}
