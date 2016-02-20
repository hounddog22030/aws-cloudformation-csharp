using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.SSM
{
    public class Document<T> : ResourceBase where T: CloudFormationDictionary, new()
    {
        public Document() : base(ResourceType.AwsSsmDocument)
        {
            this.Content.Add("schemaVersion", "1.2");
        }

        protected override bool SupportsTags => false;

        [JsonIgnore]
        public T Content
        {
            get
            {
                if (this.Properties.GetValue<T>() == null)
                {
                    this.Content = new T();
                }
                return this.Properties.GetValue<T>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }
    }

    public abstract class SsmRuntime<T> : CloudFormationDictionary where T: SsmRuntimeConfigProperties, new()
    {
        protected SsmRuntime()
        {
            this.Properties = this.Add("runtimeConfig", new T()) as T;
        }

        [JsonIgnore]
        public T Properties
        {
            get
            {
                return this.GetValue<T>();
            }
            set
            {
                this.SetValue(value);
            }
        }
    }

    public class SsmRuntimeConfigDomainJoin : SsmRuntime<SsmRuntimeConfigDomainJoinProperties>
    {

    }

    public abstract class SsmRuntimeConfigProperties : CloudFormationDictionary
    {
        
    }

    public class SsmRuntimeConfigDomainJoinProperties : SsmRuntimeConfigProperties
    {
        [JsonIgnore]
        public object DirectoryId
        {
            get
            {
                return this.GetValue<object>();
            }
            set
            {
                this.SetValue(value);
            }
        }
        [JsonIgnore]
        public object DirectoryName
        {
            get
            {
                return this.GetValue<object>();
            }
            set
            {
                this.SetValue(value);
            }
        }
        [JsonIgnore]
        public object DnsIpAddresses
        {
            get
            {
                return this.GetValue<object>();
            }
            set
            {
                this.SetValue(value);
            }
        }

    }
}
