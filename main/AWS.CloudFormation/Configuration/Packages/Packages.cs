using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Stack;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Resource.Wait;

namespace AWS.CloudFormation.Configuration.Packages
{
    public abstract class PackageBase
    {
        protected PackageBase(Uri msi)
        {
            Msi = msi;
        }

        internal void AddConfiguration(Instance instance)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(Msi.AbsolutePath);
            var configSet = instance.Metadata.Init.ConfigSets.GetConfigSet(fileName).GetConfig(fileName);
            configSet.Packages.Add("msi",Msi.AbsoluteUri);
        }

        protected PackageBase(string snapshotId)
        {
            SnapshotId = snapshotId;
        }


        public Uri Msi { get; }


        public string SnapshotId { get; }


        public WaitCondition WaitCondition { get; protected set; }
    }

    public abstract class PackageChef : PackageBase
    {
        private WindowsInstance sqlServer;
        private string v1;
        private string v2;

        public PackageChef(LaunchConfiguration instance, string snapshotId, string cookbookName, string recipeName) : base(snapshotId)
        {
            Instance = instance;
            CookbookName = cookbookName;
            RecipeName = $"{CookbookName}::{recipeName}";
            this.WaitCondition = this.AddChefExec("gtbb",cookbookName,recipeName);
        }

        public PackageChef(LaunchConfiguration instance, string snapshotId, string cookbookName) : this(instance,snapshotId,cookbookName,"default")
        {
        }

        public LaunchConfiguration Instance { get; }
        public string CookbookName { get; }
        public string RecipeName { get; private set; }

        public WaitCondition AddChefExec(string s3bucketName, string cookbookFileName, string recipeList)
        {
            var chefConfig = this.Instance.Metadata.Init.ConfigSets.GetConfigSet(RecipeName.Replace(":",string.Empty)).GetConfig("run");
            var chefCommandConfig = chefConfig.Commands.AddCommand<Command>($"{this.CookbookName}{recipeList.Replace(':', '-')}");
            chefCommandConfig.Command.SetFnJoin($"C:/opscode/chef/bin/chef-client.bat -z -o {recipeList} -c c:/chef/{cookbookFileName}/client.rb");
            WaitCondition chefComplete = new WaitCondition(this.Instance.Template, $"waitCondition{this.Instance.LogicalId}{cookbookFileName}{recipeList}".Replace(".", string.Empty).Replace(":", string.Empty),
                new TimeSpan(4, 0, 0));
            chefConfig.Commands.AddCommand<Command>(chefComplete);
            return chefComplete;

        }
    }

    public class VisualStudio : PackageChef
    {

        public VisualStudio(LaunchConfiguration instance) : base(instance, "snap-5e27a85a", "vs")
        {
        }
    }
    public class SqlServerExpress : PackageChef
    {
        public SqlServerExpress(WindowsInstance sqlServer) : base(sqlServer,"snap-2cf80f29", "sqlserver")
        {
            sqlServer.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 20);
            sqlServer.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 10);
            sqlServer.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 10);
            var node = sqlServer.GetChefNodeJsonContent();
            var sqlServerNode =  node.Add("sqlserver");
            sqlServerNode.Add("SQLUSERDBDIR", "d:\\SqlUserDb");
            sqlServerNode.Add("SQLUSERDBLOGDIR", "e:\\SqlUserDbLog");
            sqlServerNode.Add("INSTALLSQLDATADIR", "f:\\SqlData");
        }
    }

    public abstract class TeamFoundationServer : PackageChef
    {
        public TeamFoundationServer(WindowsInstance instance, string recipeName) : base (instance,"snap-4e69d94b","tfs", recipeName)
        {
        }
    }

    public class TeamFoundationServerApplicationTier : TeamFoundationServer
    {
        public TeamFoundationServerApplicationTier(WindowsInstance tfsServer) : base(tfsServer, "applicationtier")
        {

        }
    }
    public class TeamFoundationServerBuildServer : TeamFoundationServer
    {
        public TeamFoundationServerBuildServer(WindowsInstance buildServer, WindowsInstance applicationServer) : base(buildServer, "build")
        {
            var node = buildServer.GetChefNodeJsonContent();
            var tfsNode = node.Add("tfs");
            tfsNode.Add("application_server_netbios_name", applicationServer.LogicalId);

        }
    }
    public class TeamFoundationServerBuildServerAgentOnly : TeamFoundationServer
    {
        public TeamFoundationServerBuildServerAgentOnly(WindowsInstance buildServer, WindowsInstance applicationServer) : base(buildServer, "agent")
        {
            var node = buildServer.GetChefNodeJsonContent();
            var tfsNode = node.Add("tfs");
            tfsNode.Add("application_server_netbios_name", applicationServer.LogicalId);

        }
    }

}
