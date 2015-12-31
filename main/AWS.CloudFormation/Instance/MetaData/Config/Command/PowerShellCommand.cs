using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Instance.MetaData.Config.Command
{
    public class PowerShellCommand : Command
    {
        public override void AddCommandLine(object[] commandLine)
        {
            List<object> stringCommandLine = new List<object>();

            FileInfo outFile = null;

            stringCommandLine.Add("powershell.exe ");

            foreach (var o in commandLine)
            {
                if (o is IName)
                {
                    stringCommandLine.Add(new ReferenceProperty() {Ref = ((IName)o).Name});
                }
                else
                {
                    stringCommandLine.Add(o.ToString());
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
