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
                return this.Properties.GetValue<Vpc>();
            }
            set
            {
                this.Properties.SetValue(value);
            }
        }

    }
}
