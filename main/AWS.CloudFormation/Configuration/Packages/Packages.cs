using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Stack;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.RDS;
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
        protected PackageBase(Uri msi, string snapshotId, string bucketName) : this(msi)
        {
            SnapshotId = snapshotId;
            BucketName = bucketName;
        }

        public string BucketName { get; }


        public LaunchConfiguration Instance { get; internal set; }

        public Uri Msi { get; }


        public string SnapshotId { get; protected set; }


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
                var configSet = this.Config; // configuration.Metadata.Init.ConfigSets.GetConfigSet(fileName).GetConfig(fileName);
                if (!configSet.Packages.ContainsKey("msi"))
                {
                    var msi = new CloudFormationDictionary();
                    msi.Add(fileName, Msi.AbsoluteUri);
                    configSet.Packages.Add("msi", msi);

                }
            }
            if (!string.IsNullOrEmpty(this.BucketName))
            {
                var appSettingsReader = new AppSettingsReader();
                string accessKeyString = (string)appSettingsReader.GetValue("S3AccessKey", typeof(string));
                string secretKeyString = (string)appSettingsReader.GetValue("S3SecretKey", typeof(string));

                if (!configuration.Metadata.Authentication.ContainsKey("S3AccessCreds"))
                {
                    var auth = configuration.Metadata.Authentication.Add("S3AccessCreds",
                        new S3Authentication(accessKeyString, secretKeyString, new string[] { BucketName }));
                    auth.Type = "S3";
                }
            }
        }

        private WaitCondition _waitCondition = null;

        protected T ConfigSet
        {
            get
            {
                return this.Instance.Metadata.Init.ConfigSets.GetConfigSet<T>(this.ConfigSetName);
            }
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
                    var name = $"WaitCondition4{this.Instance.LogicalId}4{this.GetType().Name}"
                        .Replace(".", string.Empty)
                        .Replace(":", string.Empty);

                    _waitCondition = new WaitCondition();

                    this.Instance.Template.Resources.Add(name,_waitCondition);

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
            : base(new Uri("https://opscode-omnibus-packages.s3.amazonaws.com/windows/2012r2/i386/chef-client-12.6.0-1-x86.msi"), snapshotId, bucketName)
        {
            CookbookName = cookbookName;
            if (string.IsNullOrEmpty(recipeName))
            {
                recipeName = "default";
            }
            RecipeList = $"{CookbookName}::{recipeName}";
        }


        public PackageChef(string snapshotId, string bucketName, string cookbookName)
            : this(snapshotId, bucketName, cookbookName, null)
        {
        }

        public string CookbookName { get; }
        public string RecipeList { get; private set; }

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);

            var appSettingsReader = new AppSettingsReader();

            string accessKeyString = (string)appSettingsReader.GetValue("S3AccessKey", typeof(string));
            string secretKeyString = (string)appSettingsReader.GetValue("S3SecretKey", typeof(string));


            var chefConfigContent = configuration.GetChefNodeJsonContent();

            if (!chefConfigContent.ContainsKey("s3_file"))
            {
                var s3FileNode = chefConfigContent.Add("s3_file");
                s3FileNode.Add("key", accessKeyString);
                s3FileNode.Add("secret", secretKeyString);
            }

            var chefCommandConfig = this.Config.Commands.AddCommand<Command>(RecipeList.Replace(':', '-'));

            var clientRbFileKey = $"c:/chef/{CookbookName}/client.rb";
            this.Config.Files.GetFile(clientRbFileKey)
                .Content.SetFnJoin(
                    $"cache_path 'c:/chef'\ncookbook_path 'c:/chef/{CookbookName}/cookbooks'\nlocal_mode true\njson_attribs 'c:/chef/node.json'\n");
            this.Config.Sources.Add($"c:/chef/{CookbookName}/",
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

    public class SqlServerExpressFromAmi : PackageBase<ConfigSet>
    {
        public SqlServerExpressFromAmi(string bucketName) : base(null,null,bucketName)
        {
        }


        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            var backup = configuration.AddDisk(Ebs.VolumeTypes.Magnetic, 20);
            backup.Ebs.DeleteOnTermination = false;
            var command = this.Config.Commands.AddCommand<Command>("CreateBackupShare");
            command.Command = new PowershellFnJoin(FnJoinDelimiter.Space,
                "New-Item \"d:\\Backups\" -type directory;New-SMBShare -Name \"Backups\" -Path \"d:\\Backups\" -FullAccess \"NT AUTHORITY\\NETWORK SERVICE\", \"YADAYADA\\johnny\"");
            command.WaitAfterCompletion = 0.ToString();
            command.Test = "IF EXIST d:power\\BACKUPS EXIT /B 1";
            const string AddNetworkLocalPath = "c:/cfn/scripts/add-network-to-sysadmin.ps1";
            var sysadminFile = this.Config.Files.GetFile(AddNetworkLocalPath);
            sysadminFile.Source = "https://s3.amazonaws.com/gtbb/add-network-to-sysadmin.ps1";
            command = this.Config.Commands.AddCommand<Command>("AddNetworkToSysadmin");
            command.Command = new PowershellFnJoin(AddNetworkLocalPath);
            // volume for backups
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
            var backup = configuration.AddDisk(Ebs.VolumeTypes.Magnetic, 20);
            backup.Ebs.DeleteOnTermination = false;
            var command = this.Config.Commands.AddCommand<Command>("CreateBackupShare");
            command.Command = new PowershellFnJoin(FnJoinDelimiter.Space,
                "New-Item \"g:\\Backups\" -type directory;New-SMBShare -Name \"Backups\" -Path \"g:\\Backups\" -FullAccess \"NT AUTHORITY\\NETWORK SERVICE\", \"YADAYADA\\johnny\"");
            command.WaitAfterCompletion = 0.ToString();
            command.Test = "IF EXIST G:\\BACKUPS EXIT /B 1";

            // volume for backups
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
            tfsNode.Add("application_server_sqlname", new FnGetAtt(this.SqlServer, FnGetAttAttribute.AwsEc2InstancePrivateDnsName));

        }
    }

    public class TeamFoundationServerBuildServerBase : TeamFoundationServer
    {


        public const string sqlexpress4build_username_parameter_name = "SqlExpress4BuildUsername";
        public const string sqlexpress4build_password_parameter_name = "SqlExpress4BuildPassword";

        public TeamFoundationServerBuildServerBase( LaunchConfiguration applicationServer, 
                                                    string bucketName,
                                                    string recipeName,
                                                    DbInstance sqlServer4Build) : base(bucketName, recipeName)
        {
            this.ApplicationServer = applicationServer;
            this.SqlServer4Build = sqlServer4Build;
        }

        public DbInstance SqlServer4Build { get; }

        public LaunchConfiguration ApplicationServer { get; }

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            var node = this.Instance.GetChefNodeJsonContent();
            var tfsNode = node.Add("tfs");
            tfsNode.Add("application_server_netbios_name", new FnGetAtt(this.ApplicationServer, FnGetAttAttribute.AwsEc2InstancePrivateDnsName));
            tfsNode.Add("sqlexpress4build_private_dns_name", new FnGetAtt(this.SqlServer4Build, FnGetAttAttribute.AwsRdsDbInstanceEndpointAddress));
            tfsNode.Add("sqlexpress4build_username",
                new ReferenceProperty(sqlexpress4build_username_parameter_name));
            tfsNode.Add("sqlexpress4build_password",
                new ReferenceProperty(sqlexpress4build_password_parameter_name));
        }
    }



    public class TeamFoundationServerBuildServer : TeamFoundationServerBuildServerBase
    {
        public TeamFoundationServerBuildServer(LaunchConfiguration applicationServer, string bucketName, DbInstance sqlExpress4Build)
            : base(applicationServer, bucketName, "build", sqlExpress4Build)
        {
        }
    }

    public class TeamFoundationServerBuildServerAgentOnly : TeamFoundationServerBuildServerBase
    {
        public TeamFoundationServerBuildServerAgentOnly(Instance applicationServer, string bucketName, DbInstance sqlExpress4Build)
            : base(applicationServer, bucketName, "agent", sqlExpress4Build)
        {
            this.SnapshotId = string.Empty;
        }
    }
}
