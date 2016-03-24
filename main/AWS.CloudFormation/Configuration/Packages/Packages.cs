using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Stack;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.DirectoryService;
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

        public Uri Msi { get; set; }


        public string SnapshotId { get; protected set; }


        public virtual void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            this.Instance = configuration;

            if (!string.IsNullOrEmpty(this.SnapshotId))
            {
                BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(this.Instance,
                    this.Instance.GetAvailableDevice());
                blockDeviceMapping.Ebs.SnapshotId = this.SnapshotId;
                this.Instance.BlockDeviceMappings.Add(blockDeviceMapping);
            }
            if (this.Msi != null)
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(Msi.AbsolutePath).Replace(".", string.Empty).Replace("-", String.Empty);
                var configSet = this.Config; 
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

        protected virtual string ConfigSetName => $"{this.GetType().Name.Replace(".", string.Empty)}";
        protected internal virtual string ConfigName => $"{this.GetType().Name.Replace(".", string.Empty)}";

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

                    this.Instance.Template.Resources.Add(name, _waitCondition);

                    this.Config.Commands.AddCommand<Command>(_waitCondition);
                }
                return _waitCondition;
            }
        }

        protected int GetPackageIndex(LaunchConfiguration configuration, Type packageType)
        {
            int returnValue = -1;
            for (int i = 0; i < configuration.Packages.Count; i++)
            {
                if (configuration.Packages[i].GetType() == packageType)
                {
                    returnValue = i;
                    break;
                }
            }
            return returnValue;
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

        protected internal override string ConfigName => "ConfigChef";
        protected override string ConfigSetName => "ConfigSetChef";

        public string CookbookName { get; }
        public string RecipeList { get; private set; }

        public ConfigFileContent GetChefNodeJsonContent(LaunchConfiguration configuration)
        {

            Config chefConfig = ((ConfigSet) configuration.Metadata.Init.ConfigSets.First().Value).First().Value as Config;
            var nodeJson = chefConfig.Files.GetFile("c:/chef/node.json");
            if (!nodeJson.ContainsKey("domain"))
            {
                nodeJson.Add("domain", new ReferenceProperty(ActiveDirectoryBase.DomainNetBiosNameParameterName));
            }
            return nodeJson.Content;
        }

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);

            var appSettingsReader = new AppSettingsReader();

            string accessKeyString = (string)appSettingsReader.GetValue("S3AccessKey", typeof(string));
            string secretKeyString = (string)appSettingsReader.GetValue("S3SecretKey", typeof(string));

            var chefConfigContent = GetChefNodeJsonContent(configuration);

            if (!chefConfigContent.ContainsKey("s3_file"))
            {
                var s3FileNode = chefConfigContent.Add("s3_file");
                s3FileNode.Add("key", accessKeyString);
                s3FileNode.Add("secret", secretKeyString);
            }

            var clientRbFileKey = $"c:/chef/{CookbookName}/client.rb";
            this.Config.Files.GetFile(clientRbFileKey)
                .Content.SetFnJoin(
                    $"cache_path 'c:/chef'\ncookbook_path 'c:/chef/{CookbookName}/cookbooks'\nlocal_mode true\njson_attribs 'c:/chef/node.json'\n");
            this.Config.Sources.Add($"c:/chef/{CookbookName}/",
                $"https://{BucketName}.s3.amazonaws.com/{CookbookName}.tar.gz");

            var chefCommandConfig = AddChefRecipeCall(RecipeList);

            if (this.WaitAfterCompletion != TimeSpan.MinValue)
            {
                chefCommandConfig.WaitAfterCompletion = this.WaitAfterCompletion.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            }

            var node = GetChefNodeJsonContent(configuration);
            if (!node.ContainsKey("domainAdmin"))
            {
                var domainAdminUserInfoNode = node.AddNode("domainAdmin");
                domainAdminUserInfoNode.Add("name", new FnJoin(FnJoinDelimiter.None, new ReferenceProperty(ActiveDirectoryBase.DomainNetBiosNameParameterName), "\\", new ReferenceProperty(ActiveDirectoryBase.DomainAdminUsernameParameterName)));
                domainAdminUserInfoNode.Add("password", new ReferenceProperty(Template.ParameterDomainAdminPassword));

            }
        }

        protected ConfigCommand AddChefRecipeCall(string recipe)
        {
            var chefCommandConfig = this.Config.Commands.AddCommand<Command>(recipe.Replace(':','-'));

            chefCommandConfig.Command = $"C:/opscode/chef/bin/chef-client.bat -z -o {recipe} -c c:/chef/{CookbookName}/client.rb";

            return chefCommandConfig;

        }

        public TimeSpan WaitAfterCompletion { get; set; }
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
            this.WaitAfterCompletion = new TimeSpan(0,0,15);
        }

    }

    public class Iis : PackageChef
    {
        public Iis(string bucketName) : base(null, bucketName, "yadayada_iis")
        {

        }

    }

    public class SqlServerExpressFromAmi : PackageBase<ConfigSet>
    {
        public SqlServerExpressFromAmi(string bucketName) : base(null, null, bucketName)
        {
        }


        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            //const string AddNetworkLocalPath = "c:/cfn/scripts/add-network-to-sysadmin.ps1";
            //const string AddComputersLocalPath = "c:/cfn/scripts/add-network-to-sysadmin2.ps1";
            //const string EnableTcpLocalPath = "c:/cfn/scripts/SqlServer-EnableTcp.ps1";
            const string ConfigureSql4Tfs = "c:/cfn/scripts/configure-sql-4-tfs.ps1";
            base.AddToLaunchConfiguration(configuration);

            ActiveDirectoryBase.AddInstanceToDomain(configuration.RenameConfig);
            var sysadminFile = this.Config.Files.GetFile(ConfigureSql4Tfs);
            sysadminFile.Source = "https://s3.amazonaws.com/gtbb/configure-sql-4-tfs.ps1";
            var command = this.Config.Commands.AddCommand<Command>("SetUserToTfsService");

            command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None, ConfigureSql4Tfs,
                " ",
                new ReferenceProperty(ActiveDirectoryBase.DomainNetBiosNameParameterName),
                " '",
                new ReferenceProperty(ActiveDirectoryBase.DomainNetBiosNameParameterName),
                "\\",
                new ReferenceProperty("TfsServiceAccountName"),
                "' ",
                new ReferenceProperty("TfsServicePassword"));
            command.WaitAfterCompletion = 0.ToString();

            //sysadminFile = this.Config.Files.GetFile(AddNetworkLocalPath);
            //sysadminFile.Source = "https://s3.amazonaws.com/gtbb/add-network-to-sysadmin.ps1";

            //sysadminFile = this.Config.Files.GetFile(AddComputersLocalPath);
            //sysadminFile.Source = "https://s3.amazonaws.com/gtbb/add-network-to-sysadmin2.ps1";

            //command = this.Config.Commands.AddCommand<Command>("AddDomainAdminsToSysadmin");
            //command.Command = new PowershellFnJoin(AddNetworkLocalPath, new ReferenceProperty(SimpleAd.DomainNetBiosNameParameterName));
            //command.WaitAfterCompletion = 0.ToString();

            //command = this.Config.Commands.AddCommand<Command>("AddComputersLocalPath");
            //command.Command = new PowershellFnJoin(AddComputersLocalPath, new ReferenceProperty(SimpleAd.DomainNetBiosNameParameterName));
            //command.WaitAfterCompletion = 0.ToString();

            //sysadminFile = this.Config.Files.GetFile(EnableTcpLocalPath);
            //sysadminFile.Source = "https://s3.amazonaws.com/gtbb/SqlServer-EnableTcp.ps1";
            //command = this.Config.Commands.AddCommand<Command>("SqlServerEnableTcp");
            //command.Command = new PowershellFnJoin(EnableTcpLocalPath);
            //command.WaitAfterCompletion = 0.ToString();


        }


    }
    public abstract class SqlServerBase : PackageChef
    {
        protected SqlServerBase(string snapshotId, string bucketName, string recipeName) : base(snapshotId, bucketName, recipeName)
        {
        }


        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            configuration.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 20);
            configuration.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 10);
            configuration.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 10);
            var node = GetChefNodeJsonContent(configuration);
            var sqlServerNode = node.Add("sqlserver");
            sqlServerNode.Add("SQLUSERDBDIR", "d:\\SqlUserDb");
            sqlServerNode.Add("SQLUSERDBLOGDIR", "e:\\SqlUserDbLog");
            sqlServerNode.Add("INSTALLSQLDATADIR", "f:\\SqlData");
            var backup = configuration.AddDisk(Ebs.VolumeTypes.Magnetic, 20);
            backup.Ebs.DeleteOnTermination = false;
            //var command = this.Config.Commands.AddCommand<Command>("CreateBackupShare");
            //command.Command = new FnJoinPowershellCommand(FnJoinDelimiter.None,
            //    "New-Item \"d:\\Backups\" -type directory;New-SMBShare -Name \"Backups\" -Path \"d:\\Backups\" -FullAccess @('NT AUTHORITY\\NETWORK SERVICE','",
            //    new ReferenceProperty(ActiveDirectoryBase.DomainNetBiosNameParameterName),
            //    "\\",
            //    new ReferenceProperty(ActiveDirectoryBase.DomainAdminUsernameParameterName),
            //    "')");
            //command.WaitAfterCompletion = 0.ToString();
            //command.Test = "IF EXIST G:\\BACKUPS EXIT /B 1";

            // volume for backups
        }


    }

    public class SqlServerExpress : SqlServerBase
    {
        public SqlServerExpress(string bucketName) : base("snap-2cf80f29", bucketName, "sqlserver")
        {
        }

    }
    public class SqlServerStandard : SqlServerBase
    {
        public SqlServerStandard(string bucketName) : base("snap-2cf80f29", bucketName, "make_ami_from_iso")
        {
        }

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            FileInfo iso = new FileInfo("c:/cfn/files/en_sql_server_2014_standard_edition_with_service_pack_1_x64_dvd_6669998.iso");
            this.Config.Files.GetFile(iso.FullName).Source = $"https://s3.amazonaws.com/{this.BucketName}/software/{iso.Name}";

        }
    }

    public enum TeamFoundationServerEdition
    {
        [EnumMember(Value = "snap-4e69d94b")] Express2015,
        [EnumMember(Value = "snap-3a929d2e")] Standard2015Update1
    }

    public abstract class TeamFoundationServer : PackageChef
    {
        public TeamFoundationServer(TeamFoundationServerEdition edition, string bucketName, string recipeName)
            : base(edition.GetType().GetMembers(BindingFlags.Public | BindingFlags.Static).Single(r => r.Name.ToString() == edition.ToString()).GetCustomAttributes<EnumMemberAttribute>().First().Value, bucketName, "tfs", recipeName)
        {
        }
    }

    public class TeamFoundationServerApplicationTier : TeamFoundationServer
    {
        public TeamFoundationServerApplicationTier(TeamFoundationServerEdition edition, string bucketName, LaunchConfiguration sqlServer) : base(edition, bucketName, "iisconfig")
        {
            SqlServer = sqlServer;
        }

        public LaunchConfiguration SqlServer { get; }

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            var node = GetChefNodeJsonContent(configuration);
            var tfsNode = node.Add("tfs");
            var sqlServer = new FnJoin(FnJoinDelimiter.Period, SqlServer.LogicalId, new ReferenceProperty(ActiveDirectoryBase.DomainFqdnParameterName));
            tfsNode.Add("application_server_sqlname", sqlServer);
            tfsNode.Add(TeamFoundationServerBuildServerBase.TfsServiceAccountNameParameterName, new ReferenceProperty(TeamFoundationServerBuildServerBase.TfsServiceAccountNameParameterName));
            tfsNode.Add(TeamFoundationServerBuildServerBase.TfsServicePasswordParameterName, new ReferenceProperty(TeamFoundationServerBuildServerBase.TfsServicePasswordParameterName));
        }

    }

    public class TeamFoundationServerBuildServerBase : TeamFoundationServer
    {


        public const string sqlexpress4build_username_parameter_name = "SqlExpress4BuildUsername";
        public const string sqlexpress4build_password_parameter_name = "SqlExpress4BuildPassword";

        public TeamFoundationServerBuildServerBase( TeamFoundationServerEdition edition,
                                                    LaunchConfiguration applicationServer,
                                                    string bucketName,
                                                    string recipeName,
                                                    DbInstance sqlServer4Build) : base(edition,bucketName, recipeName)
        {
            this.ApplicationServer = applicationServer;
            this.SqlServer4Build = sqlServer4Build;
        }

        public DbInstance SqlServer4Build { get; }

        public LaunchConfiguration ApplicationServer { get; }
        public const string TfsServiceAccountNameParameterName = "TfsServiceAccountName";
        public const string TfsBuildAccountNameParameterName = "TfsBuildAccountName";
        public const string TfsBuildAccountPasswordParameterName = "TfsBuildAccountPassword";

        public const string TfsServicePasswordParameterName = "TfsServicePassword";

        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            var node = GetChefNodeJsonContent(configuration);
            var tfsNode = node.Add("tfs");

            tfsNode.Add("application_server_netbios_name",
                new FnJoin(FnJoinDelimiter.Period,
                    this.ApplicationServer.LogicalId,
                    new ReferenceProperty(ActiveDirectoryBase.DomainFqdnParameterName)));

            if (this.SqlServer4Build != null)
            {
                tfsNode.Add("sqlexpress4build_private_dns_name", new FnGetAtt(this.SqlServer4Build, FnGetAttAttribute.AwsRdsDbInstanceEndpointAddress));
                tfsNode.Add("sqlexpress4build_username",
                    new ReferenceProperty(sqlexpress4build_username_parameter_name));
                tfsNode.Add("sqlexpress4build_password",
                    new ReferenceProperty(sqlexpress4build_password_parameter_name));
            }
        }
    }



    public class TeamFoundationServerBuildServer : TeamFoundationServerBuildServerBase
    {
        public TeamFoundationServerBuildServer(TeamFoundationServerEdition edition, LaunchConfiguration applicationServer, string bucketName, DbInstance sqlExpress4Build)
            : base(edition,applicationServer, bucketName, "build", sqlExpress4Build)
        {
        }
    }

    public class TeamFoundationServerBuildServerAgentOnly : TeamFoundationServerBuildServerBase
    {
        public TeamFoundationServerBuildServerAgentOnly(TeamFoundationServerEdition edition,LaunchConfiguration applicationServer, string bucketName, DbInstance sqlExpress4Build)
            : base(edition,applicationServer, bucketName, "agent", sqlExpress4Build)
        {
            this.SnapshotId = string.Empty;
        }

        //public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        //{
        //    base.AddToLaunchConfiguration(configuration);
        //    var secondConfigSetName = $"ConfigSet{this.ConfigName}HttpToHttps";
        //    var secondConfigName = $"Config{this.ConfigName}HttpToHttps";
        //    var secondConfig = configuration.Metadata.Init.ConfigSets.GetConfigSet(secondConfigSetName).GetConfig(secondConfigName);

        //var msi = secondConfig.Packages.Add("msi", new CloudFormationDictionary());

        //var msiUri = new Uri($"https://s3.amazonaws.com/{BucketName}/software/WebDeploy_amd64_en-US.msi");
        //var fileName = System.IO.Path.GetFileNameWithoutExtension(msiUri.AbsolutePath).Replace(".", "x").Replace("-", "x");
        //msi.Add(fileName, msiUri.AbsoluteUri);

        //}
    }
}
