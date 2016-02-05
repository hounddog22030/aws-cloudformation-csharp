using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.RDS
{
    public class DbSubnetGroup : ResourceBase
    {
        public DbSubnetGroup(string description) 
            : base(ResourceType.AwsRdsDbSubnetGroup)
        {
            this.DBSubnetGroupDescription = description;
        }

        protected override bool SupportsTags => true;

        //  "DBSubnetGroupDescription" : String,
        //"SubnetIds"
        [JsonIgnore]
        public string DBSubnetGroupDescription
        {
            get
            {
                return this.Properties.GetValue<string>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public object[] SubnetIds
        {
            get
            {
                return this.Properties.GetValue<object[]>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        public void AddSubnet(Subnet subnet)
        {
            var replaceWith = new List<object>();
            if (this.SubnetIds != null && this.SubnetIds.Any())
            {
                replaceWith.AddRange(this.SubnetIds);
            }
            replaceWith.Add(new ReferenceProperty(subnet));
            this.SubnetIds = replaceWith.ToArray();

        }

    }
}
