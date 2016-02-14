using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.CloudFormation.Configuration.Packages
{
    public class InternetInformationServerPackage : PackageChef
    {
        public InternetInformationServerPackage(string snapshotId, string bucketName, string cookbookName) : base(snapshotId, bucketName, cookbookName)
        {
        }
    }
}
