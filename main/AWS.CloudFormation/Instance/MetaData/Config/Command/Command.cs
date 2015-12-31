using System;
using System.Collections.Generic;
using System.IO;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Instance.MetaData.Config.Command
{


    public class CommandConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new System.NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            throw new System.NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new System.NotImplementedException();
        }
    }

    public class Command : CloudFormationDictionary
    {
        public ConfigCommand Parent { get; internal set; }

        public virtual void AddCommandLine(params object[] commandLine)
        {
            List<object> stringCommandLine = new List<object>();

            FileInfo outFile = null;

            foreach (var o in commandLine)
            {
                if (o is IName)
                {
                    stringCommandLine.Add(new ReferenceProperty() { Ref = ((IName)o).Name });
                }
                else
                {
                    stringCommandLine.Add(o);
                }

                if (o is FileInfo && ((FileInfo)o).IsOutFileLog())
                {
                    outFile = o as FileInfo;
                }
            }

            if (outFile == null)
            {
                outFile = new FileInfo($"c:\\cfn\\log\\{this.Parent.Name}.log");
                stringCommandLine.Add($" > \"{outFile.FullName}\"");
            }

            this.Parent.Test = $"IF EXIST \"{outFile.FullName}\" EXIT 1";

            this.SetFnJoin(stringCommandLine.ToArray());
        }
    }
}
