using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS.CloudFormation.Instance.MetaData.Config.Command
{
    public class PowerShell : Command
    {
        public PowerShell(ConfigCommand parent) : base(parent)
        {
        }
        public void AddPowershellCommandLine(object[] commandLine)
        {
            List<object> stringCommandLine = new List<object>();
            foreach (var o in commandLine)
            {
                stringCommandLine.Add(o.ToString());
                if (o is FileInfo && ((FileInfo)o).IsOutFileLog())
                {
                    this.Parent.Test = string.Format("IF EXIST \"{0}\" EXIT 1", (o as FileInfo).FullName);
                }
            }

            this.AddFnJoin(stringCommandLine.ToArray());
        }
    }
}
