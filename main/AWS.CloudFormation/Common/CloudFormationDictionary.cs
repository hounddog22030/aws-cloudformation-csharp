using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
            var propertyName = GetPropertyName(value);
            this[propertyName] = value;
        }

        private static string GetPropertyName(object value)
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame[] stackFrames = stackTrace.GetFrames();

            StackFrame propertyStackFrame = null;
            MethodBase propertyMethod = null;
            for (int i = 0; i < stackFrames.Length -1; i++)
            {
                propertyStackFrame = stackFrames[i];
                propertyMethod = propertyStackFrame.GetMethod();
                if (propertyMethod.IsSpecialName)
                {
                    break;
                }
            }

            string propertyName = propertyMethod.Name.Substring("set_".Length);

            CloudFormationDictionary valueAsCloudFormationDictionary = value as CloudFormationDictionary;
            if (valueAsCloudFormationDictionary != null && valueAsCloudFormationDictionary.First().Key == "Ref" && !propertyName.EndsWith("Id"))
            {
                propertyName += "Id";
            }
            return propertyName;
        }

        public T GetValue<T>(string name)
        {
            if (this.ContainsKey(name))
            {
                return (T) this[name];
            }
            return default(T);

        }
        public T GetValue<T>()
        {
            StackTrace stackTrace = new StackTrace();           // get call stack
            StackFrame[] stackFrames = stackTrace.GetFrames();
            StackFrame propertyStackFrame = stackFrames[1];
            var m = propertyStackFrame.GetMethod();
            string propertyName = m.Name.Substring("get_".Length);
            System.Diagnostics.Debug.WriteLine(propertyName);
            return GetValue<T>(propertyName);
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
