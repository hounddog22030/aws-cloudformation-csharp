//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using AWS.CloudFormation.Property;
//using Newtonsoft.Json;

//namespace AWS.CloudFormation.Resource
//{
//    public class SecurityGroup : ResourceBase
//    {
//        public SecurityGroup(string key) : base("AWS::EC2::SecurityGroup",key)
//        {
            
//        }

//        private Vpc _vpc;
//        [JsonIgnore]
//        public Vpc Vpc
//        {
//            get { return _vpc; }
//            set
//            {
//                if (_vpc != null)
//                {
//                    properties.Remove(_vpc.Key);
//                }
//                properties["VpcId"] = new ReferenceProperty() { Ref = value.Key };
//                _vpc = value;
//            }
//        }
//    }
//}
