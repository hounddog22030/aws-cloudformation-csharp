using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.CloudFormation.Configuration.Packages
{
    public abstract class PackageBase
    {
        protected PackageBase(string cookbookName, string snapshotId)
        {
            CookbookName = cookbookName;
            SnapshotId = snapshotId;
        }

        public string CookbookName { get; }

        public string SnapshotId { get; }
    }
    public class VisualStudio : PackageBase
    {
        public VisualStudio() : base("vs", "snap-5e27a85a")
        {
        }
    }
    public class SqlServerExpress : PackageBase
    {
        public SqlServerExpress() : base("sqlserver", "snap-2cf80f29")
        {
        }
    }

}
