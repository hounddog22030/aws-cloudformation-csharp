﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Stack;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Resource.EC2.Networking
{
    public class VpcEndpoint : ResourceBase
    {

        public VpcEndpoint(string serviceName, Vpc vpc) : base(ResourceType.AwsEc2VpcEndpoint)
        {
            
            this.ServiceName = new FnJoin(FnJoinDelimiter.Period,
                "com.amazonaws",
                new ReferenceProperty("AWS::Region"),
                serviceName);
            this.Vpc = vpc;
        }

        protected override bool SupportsTags => false;

        [JsonIgnore]
        public object PolicyDocument
        {
            get { return this.Properties.GetValue<object>(); }
            set { this.Properties.SetValue(value); }
        }
        [JsonIgnore]
        public List<string> RouteTableIds
        {
            get { return this.Properties.GetValue<List<string>>(); }
            set { this.Properties.SetValue(value); }
        }
        [JsonIgnore]
        public object ServiceName
        {
            get { return this.Properties.GetValue<object>(); }
            set { this.Properties.SetValue(value); }
        }
        [JsonIgnore]
        public Vpc Vpc
        {
            get { return this.Properties.GetValue<Vpc>("VpcId"); }
            set { this.Properties.SetValue("VpcId",value); }
        }
    }
}