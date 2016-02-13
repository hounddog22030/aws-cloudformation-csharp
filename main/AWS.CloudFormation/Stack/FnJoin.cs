using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;

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

    public class PowershellFnJoin : FnJoin
    {
        public PowershellFnJoin(FnJoinDelimiter delimiter, params object[] elements) : base(delimiter, elements)
        {
            var temp = new List<object>();
            var commandLine = "start /high powershell.exe ";
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

        public PowershellFnJoin(params object[] elements) : this(FnJoinDelimiter.Space, elements)
        {
        }
    }
}
