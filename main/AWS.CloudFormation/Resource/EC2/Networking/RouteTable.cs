using AWS.CloudFormation.Common;
using AWS.CloudFormation.Serializer;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class RouteTable : ResourceBase
    {
        public RouteTable(Template template, string name, Vpc vpc)
            : base(template, "AWS::EC2::RouteTable", name, true)
        {
            
            this.Vpc = vpc;
        }

        [JsonIgnore]
        public Vpc Vpc
        {
            get
            {
                var vpcId = this.Properties.GetValue<CloudFormationDictionary>();
                return vpcId["Ref"] as Vpc;
            }
            set
            {
                var refDictionary = new CloudFormationDictionary();
                refDictionary.Add("Ref", ((ILogicalId)value).LogicalId);
                this.Properties.SetValue(refDictionary);
            }
        }

    }
}
