using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Configuration.Packages
{
    public static class ExecuteRemotePowershellScript
    {
        public static void AddExecuteRemotePowershellScript(Config config, Uri remoteUri, TimeSpan waitAfterCompletion)
        {
            AddExecuteRemotePowershellScript(config,remoteUri,waitAfterCompletion,null);
        }
        public static void AddExecuteRemotePowershellScript(Config config, Uri remoteUri, TimeSpan waitAfterCompletion, PowershellFnJoin test)
        {
            var fileName = Path.GetFileName(remoteUri.AbsoluteUri);
            var localFileName = Path.Combine("c:/cfn/scripts", fileName);
            var fileCheckAdReplicationSite = config.Files.GetFile(localFileName);

            fileCheckAdReplicationSite.Source = remoteUri.AbsoluteUri;

            ConfigCommand currentCommand = config.Commands.AddCommand<Command>(fileName.Replace(".", string.Empty).Replace("-", string.Empty));
            currentCommand.WaitAfterCompletion = 0.ToString();
            currentCommand.Command = new PowershellFnJoin(localFileName);

            if (test != null)
            {
                currentCommand.Test = test;
            }
            currentCommand.WaitAfterCompletion = waitAfterCompletion.TotalSeconds.ToString(CultureInfo.InvariantCulture);
        }
    }
}
