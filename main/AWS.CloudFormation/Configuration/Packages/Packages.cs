﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Configuration.Packages
{
    public abstract class PackageBase
    {
        protected PackageBase(string cookbookName, string snapshotId) : this(cookbookName,snapshotId, "default")
        {
        }
        protected PackageBase(string cookbookName, string snapshotId, string recipeName)
        {
            CookbookName = cookbookName;
            SnapshotId = snapshotId;
            RecipeName = $"{CookbookName}::{recipeName}";
        }

        public string CookbookName { get; }

        public string SnapshotId { get; }

        public string RecipeName { get; private set; }
    }
    public class VisualStudio : PackageBase
    {
        public VisualStudio() : base("vs", "snap-5e27a85a")
        {
        }
    }
    public class SqlServerExpress : PackageBase
    {
        public SqlServerExpress(WindowsInstance sqlServer) : base("sqlserver", "snap-2cf80f29")
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

    public abstract class TeamFoundationServer : PackageBase
    {
        public TeamFoundationServer(string recipeName) : base ("tfs", "snap-4e69d94b", recipeName)
        {
            
        }
    }

    public class TeamFoundationServerApplicationTier : TeamFoundationServer
    {
        public TeamFoundationServerApplicationTier() : base("applicationtier")
        {
            
        }
    }

}
