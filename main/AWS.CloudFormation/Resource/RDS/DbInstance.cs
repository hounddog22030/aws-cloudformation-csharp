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
        DbT2Micro,
        [EnumMember(Value = "db.r3.large")]
        DbR3Large

    }

    //The name of the database engine to be used for this instance.

    //Valid Values: MySql | mariadb | oracle-se1 | oracle-se | oracle-ee | sqlserver-ee | sqlserver-se | sqlserver-ex | sqlserver-web | postgres | aurora
    [JsonConverter(typeof(EnumConverter))]
    public enum EngineType
    {
        [EnumMember(Value = "mysql")]
        MySql,
        [EnumMember(Value = "mariadb")]
        MariaDb,
        [EnumMember(Value = "sqlserver-ex")]
        SqlServerExpress,
        [EnumMember(Value = "aurora")]
        Aurora
    }
    [JsonConverter(typeof(EnumConverter))]
    public enum LicenseModelType
    {
        [EnumMember(Value = "general-public-license")]
        GeneralPublicLicense,
        [EnumMember(Value = "license-included")]
        LicenseIncluded
    }
    //"general-public-license"
    //"license-included"
    public class DbInstance : ResourceBase
    {
        public DbInstance(DbInstanceClassEnum instanceType, 
            EngineType engineType, 
            LicenseModelType licenseType,
            Ebs.VolumeTypes storageType,
            int allocatedStorage,
            object masterUserName,
            object masterPassword 
            ) : base(ResourceType.AwsRdsDbInstance)
        {
            this.Type = ResourceType.AwsRdsDbInstance;
            this.DBInstanceClass = instanceType;
            this.AllocatedStorage = allocatedStorage.ToString();
            this.Engine = engineType;
            this.MasterUsername = masterUserName;
            this.MasterUserPassword = masterPassword;
            this.LicenseModel = licenseType;
            this.StorageType = storageType;

        }
        public DbInstance(DbInstanceClassEnum instanceType, 
            EngineType engineType,
            LicenseModelType licenseType,
            Ebs.VolumeTypes storageType,
            int allocatedStorage,
            object masterUserName,
            object masterPassword,
            DbSubnetGroup subnetGroup, 
            DbSecurityGroup dbSecurityGroup
            ) : this(instanceType,engineType,licenseType, storageType, allocatedStorage, masterUserName, masterPassword)
        {
            this.DBSubnetGroupName = new ReferenceProperty(subnetGroup);
            this.AddDbSecurityGroup(dbSecurityGroup);
        }

        public DbInstance(DbInstanceClassEnum instanceType,
            EngineType engineType,
            LicenseModelType licenseType,
            Ebs.VolumeTypes storageType,
            int allocatedStorage,
            DbSubnetGroup subnetGroup,
            SecurityGroup dbSecurityGroup,
            object masterUserName,
            object masterPassword
            ) : this(instanceType, engineType, licenseType, storageType, allocatedStorage, masterUserName, masterPassword)
        {
            this.DBSubnetGroupName = new ReferenceProperty(subnetGroup);
            this.AddVpcSecurityGroup(dbSecurityGroup);
        }

        protected override bool SupportsTags => true;

        [JsonIgnore]
        public Ebs.VolumeTypes StorageType
        {
            get
            {
                return this.Properties.GetValue<Ebs.VolumeTypes>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public LicenseModelType LicenseModel
        {
            get
            {
                return this.Properties.GetValue<LicenseModelType>();
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
        public object MasterUsername
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

        [JsonIgnore]
        public object MasterUserPassword
        {
            get
            {
                return this.Properties.GetValue<object>();
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
        [JsonIgnore]
        // ReSharper disable once InconsistentNaming
        public ReferenceProperty[] VPCSecurityGroups
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
        public void AddVpcSecurityGroup(SecurityGroup securityGroup)
        {
            var replaceWith = new List<ReferenceProperty>();
            if (this.VPCSecurityGroups != null && this.VPCSecurityGroups.Any())
            {
                replaceWith.AddRange(this.VPCSecurityGroups.ToArray());
            }
            replaceWith.Add(new ReferenceProperty(securityGroup));
            this.VPCSecurityGroups = replaceWith.ToArray();

        }







    }
}
