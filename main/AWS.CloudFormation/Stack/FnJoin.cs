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

        public FnJoin(string delimiter, params object[] elements)
        {
            this.Delimiter = delimiter;
            this.Elements = elements;
        }

        private void PopulateDictionary()
        {
            if (!string.IsNullOrEmpty(this.Delimiter) && this.Elements != null)
            {
                var temp = new List<object>();
                temp.Add(this.Delimiter);
                temp.Add(this.Elements);
                this.Clear();
                this.Add("Fn::Join", temp.ToArray());

            }

        }
    }

    public class PowershellFnJoin : FnJoin
    {
        public PowershellFnJoin(params object[] elements) : base(" ", elements)
        {
            var temp = new List<object>();
            temp.Add("powershell.exe -ExecutionPolicy RemoteSigned");
            temp.AddRange(this.Elements);
            this.Elements = temp.ToArray();
        }


    }
}
