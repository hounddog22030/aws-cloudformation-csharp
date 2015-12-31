using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Instance.Metadata.Config;
using AWS.CloudFormation.Instance.Metadata.Config.Command;
using AWS.CloudFormation.Property;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Common
{
    public class CloudFormationDictionary : Dictionary<string, object>
    {
        public CloudFormationDictionary()
        {
            
        }
        public CloudFormationDictionary(Instance.Instance instance)
        {
            this.Instance = instance;
        }

        public Instance.Instance Instance { get; internal set; }

        public CloudFormationDictionary Add(string key)
        {
            return Add(key, new CloudFormationDictionary(this.Instance));
        }


        public CloudFormationDictionary Add(string key, CloudFormationDictionary value)
        {
            base.Add(key, value);
            return value;
        }

        public void SetFnJoin(params object[] fnJoinElements)
        {
            AddFnJoin("", fnJoinElements);
        }
        private void AddFnJoin(string delimiter, params object[] fnJoinElements)
        {
            var final = new object[] { delimiter, fnJoinElements };
            base.Add("Fn::Join", final);
        }
    }
}
