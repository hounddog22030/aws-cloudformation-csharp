using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Stack;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Wait;

namespace AWS.CloudFormation.Configuration.Packages
{

    public interface IAddToLaunchConfiguration
    {
        void AddToLaunchConfiguration(LaunchConfiguration configuration);
    }


    public abstract class PackageBase<T> : IAddToLaunchConfiguration where T : ConfigSet, new()
    {
        public static readonly TimeSpan TimeoutMax = new TimeSpan(12, 0, 0);

        public virtual void Participate(ResourceBase participant)
        {
            throw new NotSupportedException();
        }

        protected PackageBase()
        {

        }

        protected PackageBase(Uri msi)
        {
            Msi = msi;
        }

        protected PackageBase(string snapshotId)
        {
            SnapshotId = snapshotId;
        }
        protected PackageBase(Uri msi, string snapshotId) : this(msi)
        {
             SnapshotId = snapshotId;
        }

        public LaunchConfiguration Instance { get; internal set; }

        public Uri Msi { get; }


        public string SnapshotId { get; }


        public virtual void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            this.Instance = configuration;

            if (!string.IsNullOrEmpty(this.SnapshotId))
            {
                BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(this.Instance,
                    this.Instance.GetAvailableDevice());
                blockDeviceMapping.Ebs.SnapshotId = this.SnapshotId;
                this.Instance.AddBlockDeviceMapping(blockDeviceMapping);
            }
            if (this.Msi != null)
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(Msi.AbsolutePath).Replace(".",string.Empty).Replace("-",String.Empty);
                var configSet = configuration.Metadata.Init.ConfigSets.GetConfigSet(fileName).GetConfig(fileName);
                if (!configSet.Packages.ContainsKey("msi"))
                {
                    var msi = new CloudFormationDictionary();
                    msi.Add(fileName, Msi.AbsoluteUri);
                    configSet.Packages.Add("msi", msi);

                }
            }
        }

        private WaitCondition _waitCondition = null;

        protected T ConfigSet
        {
            get { return this.Instance.Metadata.Init.ConfigSets.GetConfigSet<T>(this.ConfigSetName); }
        }

        protected string ConfigSetName => $"{this.GetType().Name.Replace(".", string.Empty)}";
        protected string ConfigName => $"{this.GetType().Name.Replace(".", string.Empty)}";

        protected Config Config
        {
            get { return this.ConfigSet.GetConfig(this.ConfigName); }
        }


        public WaitCondition WaitCondition
        {
            get
            {
                if (_waitCondition == null)
                {
                    _waitCondition = new WaitCondition(this.Instance.Template,
                        $"waitCondition{this.Instance.LogicalId}{this.GetType().Name}".Replace(".", string.Empty)
                            .Replace(":", string.Empty), TimeoutMax);

                    this.Config.Commands.AddCommand<Command>(_waitCondition);
                }
                return _waitCondition;
            }
        }
    }

    public class Dir1 : PackageBase<ConfigSet>
    {

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            var command = this.Config.Commands.AddCommand<Command>("dir1");
            command.Command = "dir>dir1.txt";
        }
    }
    public class Dir2 : PackageBase<ConfigSet>
    {

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            var command = this.Config.Commands.AddCommand<Command>("dir2");
            command.Command = "dir>dir2.txt";
        }
    }

    public abstract class PackageChef : PackageBase<ConfigSet>
    {

        public PackageChef(string snapshotId, string bucketName, string cookbookName, string recipeName)
            : base(new Uri("https://opscode-omnibus-packages.s3.amazonaws.com/windows/2012r2/i386/chef-client-12.6.0-1-x86.msi"), snapshotId)
        {
            CookbookName = cookbookName;
            BucketName = bucketName;
            if (string.IsNullOrEmpty(recipeName))
            {
                recipeName = "default";
            }
            RecipeList = $"{CookbookName}::{recipeName}";
        }

        public string BucketName { get; }

        public PackageChef(string snapshotId, string bucketName, string cookbookName)
            : this(snapshotId, bucketName, cookbookName, null)
        {
        }

        public string CookbookName { get; }
        public string RecipeList { get; private set; }


        protected Resource.EC2.Instancing.Metadata.Config.Config ChefConfig
        {
            get
            {
                return
                    this.Instance.Metadata.Init.ConfigSets.GetConfigSet<ChefConfigSet>(RecipeList.Replace(":",
                        string.Empty)).Run;
            }
        }


        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);

            if (!configuration.Metadata.Authentication.ContainsKey("S3AccessCreds"))
            {
                var appSettingsReader = new AppSettingsReader();
                string accessKeyString = (string) appSettingsReader.GetValue("S3AccessKey", typeof (string));
                string secretKeyString = (string) appSettingsReader.GetValue("S3SecretKey", typeof (string));
                var auth = configuration.Metadata.Authentication.Add("S3AccessCreds",
                    new S3Authentication(accessKeyString, secretKeyString, new string[] {BucketName}));
                auth.Type = "S3";
                var chefConfigContent = configuration.GetChefNodeJsonContent();
                var s3FileNode = chefConfigContent.Add("s3_file");
                s3FileNode.Add("key", accessKeyString);
                s3FileNode.Add("secret", secretKeyString);
            }

            //var chefDict = new CloudFormationDictionary();
            //chefDict.Add("chef","https://opscode-omnibus-packages.s3.amazonaws.com/windows/2012r2/i386/chef-client-12.6.0-1-x86.msi");

            //this.ChefConfig.Add("msi", chefDict);
            var chefCommandConfig = this.ChefConfig.Commands.AddCommand<Command>(RecipeList.Replace(':', '-'));

            var clientRbFileKey = $"c:/chef/{CookbookName}/client.rb";
            this.ChefConfig.Files.GetFile(clientRbFileKey)
                .Content.SetFnJoin(
                    $"cache_path 'c:/chef'\ncookbook_path 'c:/chef/{CookbookName}/cookbooks'\nlocal_mode true\njson_attribs 'c:/chef/node.json'\n");
            this.ChefConfig.Sources.Add($"c:/chef/{CookbookName}/",
                $"https://{BucketName}.s3.amazonaws.com/{CookbookName}.tar.gz");

            chefCommandConfig.Command = $"C:/opscode/chef/bin/chef-client.bat -z -o {RecipeList} -c c:/chef/{CookbookName}/client.rb";
        }
    }

    public class ChefConfigSet : ConfigSet
    {
        public ChefConfigSet()
        {
        }

        public Config Run
        {
            get { return this.GetConfig("run"); }
        }
    }

    public class VisualStudio : PackageChef
    {

        public VisualStudio(string bucketName) : base("snap-5e27a85a", bucketName, "vs")
        {
        }
    }

    public class SqlServerExpress : PackageChef
    {
        public SqlServerExpress(string bucketName) : base("snap-2cf80f29", bucketName, "sqlserver")
        {
        }


        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            configuration.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 20);
            configuration.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 10);
            configuration.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 10);
            var node = configuration.GetChefNodeJsonContent();
            var sqlServerNode = node.Add("sqlserver");
            sqlServerNode.Add("SQLUSERDBDIR", "d:\\SqlUserDb");
            sqlServerNode.Add("SQLUSERDBLOGDIR", "e:\\SqlUserDbLog");
            sqlServerNode.Add("INSTALLSQLDATADIR", "f:\\SqlData");
        }
    }

    public abstract class TeamFoundationServer : PackageChef
    {
        public TeamFoundationServer(string bucketName, string recipeName)
            : base("snap-4e69d94b", bucketName, "tfs", recipeName)
        {
        }
    }

    public class TeamFoundationServerApplicationTier : TeamFoundationServer
    {
        public TeamFoundationServerApplicationTier(string bucketName, LaunchConfiguration sqlServer) : base(bucketName, "applicationtier")
        {
            SqlServer = sqlServer;
        }

        public LaunchConfiguration SqlServer { get; }

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            var node = this.Instance.GetChefNodeJsonContent();
            var tfsNode = node.Add("tfs");
            tfsNode.Add("application_server_netbios_name", new FnGetAtt(this.SqlServer, "PrivateDnsName"));

        }
    }

    public class TeamFoundationServerBuildServerBase : TeamFoundationServer
    {
        public TeamFoundationServerBuildServerBase(LaunchConfiguration applicationServer, string bucketName,
            string recipeName) : base(bucketName, recipeName)
        {
            this.ApplicationServer = applicationServer;
        }

        public LaunchConfiguration ApplicationServer { get; }

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            var node = this.Instance.GetChefNodeJsonContent();
            var tfsNode = node.Add("tfs");
            tfsNode.Add("application_server_netbios_name", this.ApplicationServer.LogicalId);
        }
    }



    public class TeamFoundationServerBuildServer : TeamFoundationServerBuildServerBase
    {
        public TeamFoundationServerBuildServer(LaunchConfiguration applicationServer, string bucketName)
            : base(applicationServer, bucketName, "build")
        {
        }
    }

    public class TeamFoundationServerBuildServerAgentOnly : TeamFoundationServerBuildServerBase
    {
        public TeamFoundationServerBuildServerAgentOnly(WindowsInstance applicationServer, string bucketName)
            : base(applicationServer, bucketName, "agent")
        {
        }
    }
}
