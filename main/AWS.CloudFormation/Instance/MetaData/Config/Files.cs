using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;

namespace AWS.CloudFormation.Instance.Metadata.Config
{
    public class Files : CloudFormationDictionary
    {
        public Files(Instance instance) : base(instance)
        {

        }

        public ConfigFile GetFile(string filename)
        {
            if (this.ContainsKey(filename))
            {
                return this[filename] as ConfigFile;
            }
            else
            {
                return this.Add(filename, new ConfigFile((Instance)this.Instance)) as ConfigFile;
            }
        }
    }
}
