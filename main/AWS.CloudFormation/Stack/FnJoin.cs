﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;

namespace AWS.CloudFormation.Stack
{
    public class FnJoin : CloudFormationDictionary
    {

        public FnJoin()
        {
            
        }

        private string _delimiter = null;
        private object[] _elements = null;

        protected string Delimiter
        {
            get { return _delimiter; }
            set
            {
                _delimiter = value;
                this.PopulateDictionary();
            }
        }
        protected object[] Elements {
            get { return _elements; }
            set
            {
                _elements = value;
                this.PopulateDictionary();
            }
        }

        public FnJoin(FnJoinDelimiter delimiter, params object[] elements)
        {
            SetDelimiterAndElements(delimiter,elements);
        }

        protected void SetDelimiterAndElements(FnJoinDelimiter delimiter, object[] elements)
        {
            switch (delimiter)
            {
                case FnJoinDelimiter.Space:
                    this.Delimiter = " ";
                    break;
                case FnJoinDelimiter.Comma:
                    this.Delimiter = ",";
                    break;
                case FnJoinDelimiter.None:
                    this.Delimiter = string.Empty;
                    break;
                case FnJoinDelimiter.Period:
                    this.Delimiter = ".";
                    break;
                default:
                    throw new ArgumentException(nameof(delimiter));
            }
            this.Elements = elements;
        }

        private void PopulateDictionary()
        {
            if (this.Delimiter!=null && this.Elements != null)
            {
                var temp = new List<object>();
                temp.Add(this.Delimiter);
                temp.Add(this.Elements);
                this.Clear();
                this.Add("Fn::Join", temp.ToArray());

            }

        }
    }

    public enum FnJoinDelimiter
    {
        Space = 0,
        Comma = 1,
        None = 3,
        Period = 4
    }

    public interface IResource
    {
        ResourceBase ResourceRef { get; set; }
    }

    public class FnJoinPsExecPowershell : FnJoin
    {
        public FnJoinPsExecPowershell(FnJoin userId, ReferenceProperty password, params object[] powerShellElements) : this((object)userId, password, powerShellElements)
        {

        }
        public FnJoinPsExecPowershell(ReferenceProperty userId, ReferenceProperty password, params object[] powerShellElements) : this((object)userId, password, powerShellElements)
        {

        }
        public FnJoinPsExecPowershell(string userId, string password, params object[] powerShellElements) : this((object)userId, password, powerShellElements)
        {
        }

        private FnJoinPsExecPowershell(object userId, object password, object[] powerShellElements)
        {
            var elementsTemp = new List<object>();
            elementsTemp.Add("c:/cfn/tools/pstools/psexec.exe -accepteula -h -u");
            elementsTemp.Add(userId);
            elementsTemp.Add("-p");
            elementsTemp.Add(password);
            elementsTemp.Add(new FnJoinPowershellCommand(powerShellElements));
            this.SetDelimiterAndElements(FnJoinDelimiter.Space, elementsTemp.ToArray());
        }

        public override ResourceBase ResourceRef
        {
            get { return base.ResourceRef; }
            set
            {
                base.ResourceRef = value;
                LaunchConfiguration resourceAsLaunchConfiguration = value as LaunchConfiguration;
                if (resourceAsLaunchConfiguration != null && resourceAsLaunchConfiguration.Metadata.Init.ConfigSets.Any())
                {
                    ConfigSet firstConfigSet = resourceAsLaunchConfiguration.Metadata.Init.ConfigSets.First().Value as ConfigSet;
                    if (firstConfigSet.Any())
                    {
                        Config firstConfig = firstConfigSet.First().Value as Config;
                        firstConfig.Sources["c:/cfn/tools/pstools"] = "https://s3.amazonaws.com/gtbb/software/pstools.zip";
                    }

                }
            }
        }
    }


    public class FnJoinPowershellCommand : FnJoin
    {
        public FnJoinPowershellCommand(FnJoinDelimiter delimiter, params object[] elements) : base(delimiter, elements)
        {
            var temp = new List<object>();
            var commandLine = "powershell.exe ";
            bool remoteSigned = false;
            elements.ToList().ForEach(e=>remoteSigned=remoteSigned||e.ToString().ToLowerInvariant().Contains(".ps1"));
            if (remoteSigned)
            {
                commandLine += "-ExecutionPolicy RemoteSigned ";
            }
            temp.Add(commandLine);
            temp.AddRange(this.Elements);
            this.Elements = temp.ToArray();
        }

        public FnJoinPowershellCommand(params object[] elements) : this(FnJoinDelimiter.Space, elements)
        {
        }
    }
}
