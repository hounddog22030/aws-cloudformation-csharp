using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Resource.EC2.Networking;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.DirectoryService
{
    public class SimpleActiveDirectory : ActiveDirectoryBase
    {
        public SimpleActiveDirectory(object name, object password, DirectorySize size, Vpc vpc, params Subnet[] subnets) : base(ResourceType.AwsDirectoryServiceSimpleAd, name, password, vpc, subnets)
        {
            Size = size;
        }

        [JsonIgnore]
        public DirectorySize Size
        {
            get
            {
                return this.Properties.GetValue<DirectorySize>();
            }
            private set { this.Properties.SetValue(value); }
        }
    }
}
