using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using AWS.CloudFormation.Resource;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Common
{
    public class CloudFormationDictionary : Dictionary<string, object>
    {
        public CloudFormationDictionary()
        {
            
        }
        public CloudFormationDictionary(ResourceBase resource)
        {
            this.Resource = resource;
        }

        public ResourceBase Resource { get; internal set; }


        public string Type
        {
            get
            {
                if (this.ContainsKey("type"))
                {
                    return this["type"] as string;
                }
                return null;
            }
            set
            {
                if (this.ContainsKey("type"))
                {
                    this["type"] = value;
                }
                else
                {
                    this.Add("type", value);
                }
            }
        }

        public CloudFormationDictionary Add(string key)
        {
            return Add(key, new CloudFormationDictionary(this.Resource));
        }


        public CloudFormationDictionary Add(string key, CloudFormationDictionary value)
        {
            base.Add(key, value);
            return value;
        }

        public void SetFnJoin(params object[] fnJoinElements)
        {
            AddFnJoin("", fnJoinElements);
        }
        private void AddFnJoin(string delimiter, params object[] fnJoinElements)
        {
            var final = new object[] { delimiter, fnJoinElements };
            base.Add("Fn::Join", final);
        }

        public void SetValue(object value)
        {
            StackTrace stackTrace = new StackTrace();           // get call stack
            StackFrame[] stackFrames = stackTrace.GetFrames();
            StackFrame propertyStackFrame = stackFrames[1];
            var m = propertyStackFrame.GetMethod();
            string propertyName = m.Name.Substring("set_".Length);
            System.Diagnostics.Debug.WriteLine(propertyName);
            this[propertyName] = value;
        }

        public object GetValue(string name)
        {
            return this[name];

        }
        public object GetValue()
        {
            StackTrace stackTrace = new StackTrace();           // get call stack
            StackFrame[] stackFrames = stackTrace.GetFrames();
            StackFrame propertyStackFrame = stackFrames[1];
            var m = propertyStackFrame.GetMethod();
            string propertyName = m.Name.Substring("get_".Length);
            System.Diagnostics.Debug.WriteLine(propertyName);
            return GetValue(propertyName);
        }
    }

    //public class CloudFormationDictionaryConverter : JsonConverter
    //{
    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        CloudFormationDictionary valueAsCloudFormationDictionary = value as CloudFormationDictionary;

    //        writer.WriteStartObject();

    //        foreach (var thisValue in valueAsCloudFormationDictionary)
    //        {
    //            ILogicalId valueAsLogicalId = thisValue.Value as ILogicalId;

    //            if (valueAsLogicalId == null)
    //            {

    //                writer.WritePropertyName(thisValue.Key);
    //                writer.WriteValue(thisValue.Value);
    //            }
    //            else
    //            {
    //                writer.WritePropertyName(thisValue.Key);
    //                writer.WriteStartObject();
    //                writer.WritePropertyName("Ref");
    //                writer.WriteValue(valueAsLogicalId.LogicalId);
    //                writer.WriteEndObject();
    //            }

    //        }

    //        writer.WriteEndObject();
    //    }

    //    public override bool CanConvert(Type objectType)
    //    {
    //        throw new System.NotImplementedException();
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        throw new System.NotImplementedException();
    //    }
    //}
}
