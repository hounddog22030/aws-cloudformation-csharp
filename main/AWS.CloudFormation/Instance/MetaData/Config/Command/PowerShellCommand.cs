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
            List<object> commandLineList = new List<object>(commandLine);
            commandLineList.Insert(0,"powershell.exe ");
            base.AddCommandLine(commandLineList.ToArray());
        }
    }
}
