﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Common
{
    public class CloudFormationDictionary : ObservableDictionary<string,object>, IResource
    {
        public CloudFormationDictionary()
        {
            
        }
        public CloudFormationDictionary(ResourceBase resource)
        {
            this.ResourceRef = resource;
        }

        public ILogicalId Add(ILogicalId objectToAdd)
        {
            this.Add(objectToAdd.LogicalId, objectToAdd);
            return objectToAdd;
        }

        public virtual ResourceBase ResourceRef { get; set; }


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
            return Add(key, new CloudFormationDictionary(this.ResourceRef));
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

        readonly Dictionary<string,ILogicalId> _objects = new Dictionary<string, ILogicalId>();

        public void SetValue(string propertyName, object value)
        {
            ILogicalId valueAsLogicalId = value as ILogicalId;
            if (valueAsLogicalId != null)
            {
                var refDictionary = new CloudFormationDictionary();
                refDictionary.Add("Ref", valueAsLogicalId.LogicalId);
                _objects[propertyName] = valueAsLogicalId;
                this[propertyName] = refDictionary;
            }
            else
            {
                this[propertyName] = value;
            }

        }
        public void SetValue(object value)
        {
            var propertyName = GetPropertyName(value);
            this.SetValue(propertyName,value);
        }

        private static string GetPropertyName(object value)
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame[] stackFrames = stackTrace.GetFrames();

            StackFrame propertyStackFrame = null;
            MethodBase propertyMethod = null;
            PropertyInfo theActualProperty = null;
            for (int i = 1; i < stackFrames.Length - 1; i++)
            {
                propertyStackFrame = stackFrames[i];
                propertyMethod = propertyStackFrame.GetMethod();
                if (propertyMethod.IsSpecialName)
                {
                    theActualProperty = propertyMethod.DeclaringType.GetProperty(propertyMethod.Name.Substring("get_".Length));
                    break;
                }
            }

            var jsonPropertyAttribute = theActualProperty.GetCustomAttributes<JsonPropertyAttribute>().FirstOrDefault();
            string propertyName = theActualProperty.Name;
            if (jsonPropertyAttribute != null)
            {
                propertyName = jsonPropertyAttribute.PropertyName;
            }
            else
            {
                CloudFormationDictionary valueAsCloudFormationDictionary = value as CloudFormationDictionary;
                if (value is ILogicalId && !propertyName.EndsWith("Id"))
                {
                    propertyName += "Id";
                }

            }


            return propertyName;
        }

        public T GetValue<T>(string name)
        {
            if (this.ContainsKey(name))
            {
                if (typeof (ILogicalId).IsAssignableFrom(typeof (T)))
                {
                    return (T) _objects[name];
                }
                else if (typeof (Enum).IsAssignableFrom(typeof (T)))
                {
                    return (T) Enum.Parse(typeof(T), this[name].ToString());
                }
                else
                {
                    return (T)this[name];
                }
            }
            return default(T);

        }
        public T GetValue<T>()
        {
            T returnValue = default(T);
            StackTrace stackTrace = new StackTrace();           // get call stack
            StackFrame[] stackFrames = stackTrace.GetFrames();
            StackFrame propertyStackFrame = stackFrames[1];
            var m = propertyStackFrame.GetMethod();
            MethodInfo mi = m as MethodInfo;
            System.Diagnostics.Debug.WriteLine(mi.ReturnType.FullName);
            string propertyName = m.Name.Substring("get_".Length);

            if (typeof (ILogicalId).IsAssignableFrom(mi.ReturnType))
            {
                propertyName += "Id";
                System.Diagnostics.Debug.WriteLine("Is LogicalId");
                CloudFormationDictionary referenceDictionary = GetValue<CloudFormationDictionary>(propertyName);
                if (referenceDictionary != null)
                {
                    returnValue = (T)_objects[propertyName];
                }
            }
            else
            {
                returnValue = GetValue<T>(propertyName);
            }

            return returnValue;
        }
    }
}
