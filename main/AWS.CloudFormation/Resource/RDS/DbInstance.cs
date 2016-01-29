using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Serialization;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;

namespace AWS.CloudFormation.Resource.RDS
{

    [JsonConverter(typeof(EnumConverter))]
    public enum DbInstanceClassEnum
    {
        [EnumMember(Value = "db.t2.micro")]
        DbT2Micro

    }

    //The name of the database engine to be used for this instance.

    //Valid Values: MySql | mariadb | oracle-se1 | oracle-se | oracle-ee | sqlserver-ee | sqlserver-se | sqlserver-ex | sqlserver-web | postgres | aurora
    [JsonConverter(typeof(EnumConverter))]
    public enum EngineType
    {
        [EnumMember(Value = "MySQL")]
        MySql,
        [EnumMember(Value = "mariadb")]
        MariaDb,
        [EnumMember(Value = "sqlserver-ex")]
        SqlServerExpress
    }
    public class DbInstance : ResourceBase
    {
        public DbInstance(Template template, 
            string name, 
            DbInstanceClassEnum instanceType, 
            EngineType engineType, 
            string masterUserName,
            string masterPassword,
            int allocatedStorage,
            DbSubnetGroup subnetGroup) : base(template, name, ResourceType.AwsRdsDbInstance)
        {
            this.Type = ResourceType.AwsRdsDbInstance;
            this.DBInstanceClass = instanceType;
            this.AllocatedStorage = allocatedStorage.ToString();
            this.Engine = engineType;
            this.MasterUsername = masterUserName;
            this.MasterUserPassword = masterPassword;
            this.DBSubnetGroupName = new ReferenceProperty(subnetGroup);
            this.LicenseModel = "license-included";
            this.EngineVersion = "12.00.4422.0.v1";
        }


        protected override bool SupportsTags => true;

        //LicenseModel
        [JsonIgnore]
        public string LicenseModel
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
        public DbInstanceClassEnum DBInstanceClass
        {
            get
            {
                return this.Properties.GetValue<DbInstanceClassEnum>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore] public object AllocatedStorage
        {
            get
            {
                return this.Properties.GetValue<DbInstanceClassEnum>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public EngineType Engine
        {
            get
            {
                return this.Properties.GetValue<EngineType>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public string MasterUsername
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
        public string MasterUserPassword
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
        public ReferenceProperty DBSubnetGroupName
        {
            get
            {
                return this.Properties.GetValue<ReferenceProperty>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public string EngineVersion
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

        //PubliclyAccessible

        [JsonIgnore]
        public string PubliclyAccessible
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

        //DBSecurityGroups
        [JsonIgnore]
        // ReSharper disable once InconsistentNaming
        public ReferenceProperty[] DBSecurityGroups
        {
            get
            {
                return this.Properties.GetValue<ReferenceProperty[]>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        public void AddDbSecurityGroup(DbSecurityGroup securityGroup)
        {
            var replaceWith = new List<ReferenceProperty>();
            if (this.DBSecurityGroups != null && this.DBSecurityGroups.Any())
            {
                replaceWith.AddRange(this.DBSecurityGroups.ToArray());
            }
            replaceWith.Add(new ReferenceProperty(securityGroup));
            this.DBSecurityGroups = replaceWith.ToArray();

        }







    }
}
