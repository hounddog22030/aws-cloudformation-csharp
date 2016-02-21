using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata;
using AWS.CloudFormation.Resource.EC2.Instancing.Metadata.Config.Command;
using AWS.CloudFormation.Stack;

namespace AWS.CloudFormation.Configuration.Packages
{
    public class CreateUsers : PackageBase<ConfigSet>
    {
        public CreateUsers()
        {
            
        }
        public override void AddToLaunchConfiguration(LaunchConfiguration configuration)
        {
            base.AddToLaunchConfiguration(configuration);
            var addUserCommand = this.Config.Commands.AddCommand<Command>("addusercommand");
            var createUserPowershellScript = this.Config.Files.GetFile("c:/cfn/scripts/createuser.ps1");
            createUserPowershellScript.Source = "https://s3.amazonaws.com/gtbb/createuser.ps1";
            this.Config.Sources.Add("c:/cfn/tools/pstools", "https://s3.amazonaws.com/gtbb/software/pstools.zip");
            addUserCommand.Command = new FnJoin(FnJoinDelimiter.None,
                    "c:\\cfn\\tools\\pstools\\psexec.exe -accepteula -h -u ",
                    new ReferenceProperty(DomainControllerPackage.DomainNetBiosNameParameterName),
                    "\\administrator",
                    " -p ",
                    new ReferenceProperty(DomainControllerPackage.DomainAdminPasswordParameterName),
                    " powershell.exe -ExecutionPolicy RemoteSigned c:\\cfn\\scripts\\createuser.ps1");

        }
    }
}
