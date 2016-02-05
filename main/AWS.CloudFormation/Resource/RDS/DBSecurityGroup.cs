using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.RDS
{
    public class DbSecurityGroup : ResourceBase
    {
        public DbSecurityGroup(Vpc vpc, string description) 
            : base(ResourceType.AwsRdsDbSecurityGroup)
        {
            this.GroupDescription = description;
            this.EC2VpcId = new ReferenceProperty(vpc);
        }

        protected override bool SupportsTags => true;

        [JsonIgnore]
        // ReSharper disable once InconsistentNaming
        public object EC2VpcId
        {
            get { return this.Properties.GetValue<object>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public string GroupDescription
        {
            get { return this.Properties.GetValue<string>(); }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        // ReSharper disable once InconsistentNaming
        public DbSecurityGroupIngress[] DBSecurityGroupIngress
        {
            get { return this.Properties.GetValue<DbSecurityGroupIngress[]>(); }
            set { this.Properties.SetValue(value); }
        }

        public void AddDbSecurityGroupIngress(DbSecurityGroupIngress dbSecurityGroupIngress)
        {
            var replaceWith = new List<DbSecurityGroupIngress>();
            if (this.DBSecurityGroupIngress != null && this.DBSecurityGroupIngress.Any())
            {
                replaceWith.AddRange(this.DBSecurityGroupIngress);
            }
            replaceWith.Add(dbSecurityGroupIngress);
            this.DBSecurityGroupIngress = replaceWith.ToArray();
        }

    }

    public class DbSecurityGroupIngress
    {
        public string CIDRIP { get; set; }
        public object EC2SecurityGroupId { get; set; }
        public object EC2SecurityGroupName { get; set; }
    }
}
