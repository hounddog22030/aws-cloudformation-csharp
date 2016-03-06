using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.Lambda
{
//{
//  "Type" : "AWS::Lambda::Function",
//  "Properties" : {
//    "Code" : Code,
//    "Description" : String,
//    "Handler" : String,
//    "MemorySize" : Integer,
//    "Role" : String,
//    "Runtime" : String,
//    "Timeout" : Integer
//}
//}

    public enum FunctionMemory
    {
        [EnumMember(Value = "128")]
        Megabyte128,
        [EnumMember(Value = "192")]
        Megabyte192,
        [EnumMember(Value = "256")]
        Megabyte256,
    }

    public enum FunctionRuntime
    {
        [EnumMember(Value = "nodejs")]
        NodeJs,
        [EnumMember(Value = "java8")]
        Java8,
        [EnumMember(Value = "python2.7")]
        Python27
    }
    public class Function : ResourceBase
    {
        public Function(ReferenceProperty role, string handler, Code code, FunctionRuntime runtime, FunctionMemory memory) : base(ResourceType.AwsLambdaFunction)
        {
            this.Code = code;
            this.Handler = handler;
            this.Memory = memory;
            this.Role = role;
            this.Runtime = runtime;
        }
        public Function(ReferenceProperty role, string handler, Code code, FunctionRuntime runtime) : this(role, handler, code, runtime, FunctionMemory.Megabyte128)
        {
        }


        public FunctionMemory Memory
        {
            get
            {
                return this.Properties.GetValue<FunctionMemory>();
            }
            set { this.Properties.SetValue(value); }
        }


        protected override bool SupportsTags => false;

        [JsonIgnore]
        public string Description
        {
            get
            {
                return this.Properties.GetValue<string>();
            }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public int Timeout
        {
            get
            {
                return this.Properties.GetValue<int>();
            }
            set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public Code Code
        {
            get
            {
                return this.Properties.GetValue<Code>();
            }
            private set { this.Properties.SetValue(value); }
        }

        [JsonIgnore]
        public FunctionRuntime Runtime
        {
            get { return this.Properties.GetValue<FunctionRuntime>(); }
            private set { this.Properties.SetValue(value); }
        }


        [JsonIgnore]
        public string Handler
        {
            get
            {
                return this.Properties.GetValue<string>();
            }
            private set { this.Properties.SetValue(value); }
        }
        [JsonIgnore]
        public object Role
        {
            get
            {
                return this.Properties.GetValue<object>();
            }
            private set { this.Properties.SetValue(value); }
        }
    }
    public class Code
    {
        public string S3Bucket { get; set; }
        public string S3Key { get; set; }
        public string S3ObjectVersion { get; set; }
        public string ZipFile { get; set; }
    }
}
