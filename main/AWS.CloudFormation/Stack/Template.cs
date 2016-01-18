﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.EC2;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Serialization;

using Newtonsoft.Json;
using VpcGatewayAttachment = AWS.CloudFormation.Resource.EC2.Networking.Vpc.VpcGatewayAttachment;

namespace AWS.CloudFormation.Stack
{
    public class Template
    {

        public const string AwsTemplateFormatVersion20100909 = "2010-09-09";
        public const string CIDR_IP_THE_WORLD = "0.0.0.0/0";

        [JsonConverter(typeof(EnumConverter))]
        public enum AvailabilityZone
        {
            [EnumMember(Value = "invalid")]
            None,
            [EnumMember(Value="us-east-1a")]
            UsEast1A
        }

        public Template(string defaultKeyName, string vpcName, string vpcCidrBlock)
        {
            AwsTemplateFormatVersion = AwsTemplateFormatVersion20100909;
            this.Resources = new Dictionary<string, ResourceBase>();
            this.Parameters = new Dictionary<string, ParameterBase>();
            this.Parameters.Add(Instance.ParameterNameDefaultKeyPairKeyName, new ParameterBase(Instance.ParameterNameDefaultKeyPairKeyName, "AWS::EC2::KeyPair::KeyName", defaultKeyName));
            this.AddVpc(vpcName, vpcCidrBlock);
        }

        [JsonProperty(PropertyName = "AWSTemplateFormatVersion")]
        public string AwsTemplateFormatVersion { get; }

        public Dictionary<string, ResourceBase> Resources { get; private set; }
        public Dictionary<string, ParameterBase> Parameters { get; private set; }
        [JsonIgnore]
        public IEnumerable<Vpc> Vpcs
        {
            get { return this.Resources.Where(r => r.Value is Vpc).Select(r=>r.Value).OfType<Vpc>(); }
        }

        public Subnet AddSubnet(string name, Vpc vpc, string cidrBlock, AvailabilityZone availabilityZone)
        {
            return new Subnet(this, name, vpc, cidrBlock, availabilityZone);
        }

        public SecurityGroup GetSecurityGroup(string name, Vpc vpc, string description)
        {
            SecurityGroup securityGroup = null;
            if (this.Resources.ContainsKey(name))
            {
                securityGroup = this.Resources[name] as SecurityGroup;
            }
            else
            {
                securityGroup = new SecurityGroup(this, name, description, vpc);
                AddResource(securityGroup);
            }
            return securityGroup;

        }

        public void AddInstance(Instance resource)
        {
            AddResource(resource);
        }

        public void AddResource(ResourceBase resource)
        {
            this.Resources.Add(resource.LogicalId, resource);
        }

        public ElasticIp AddElasticIp(Template template, string name, Instance instance)
        {
            ElasticIp eip = new ElasticIp(instance, name);
            this.Resources.Add(name, eip);
            return eip;
        }

        public InternetGateway AddInternetGateway(string name, Vpc vpc)
        {
            InternetGateway gateway = new InternetGateway(this, name);
            VpcGatewayAttachment attachment = new VpcGatewayAttachment(this, name + "Attachment")
            {
                InternetGateway = gateway,
                Vpc = vpc
            };
            return gateway;
        }

        public RouteTable AddRouteTable(string key, Vpc vpc)
        {
            RouteTable returnValue = null;

            if (this.Resources.ContainsKey(key))
            {
                returnValue = (RouteTable)this.Resources[key];
            }
            else
            {
                returnValue = new RouteTable(this, key, vpc);
                return returnValue;

            }
            return returnValue;

        }

        public Route AddRoute(string routeName, InternetGateway gateway, string destinationCidrBlock, RouteTable routeTable)
        {

            Route route = null;
            if (this.Resources.ContainsKey(routeName))
            {
                route = (Route) this.Resources[routeName];
            }
            else
            {
                route = new Route(this, routeName, gateway, destinationCidrBlock, routeTable);
            }
            return route;
        }

        public Vpc AddVpc(string name, string cidrBlock)
        {
            return new Vpc(this,name,cidrBlock);
        }

        public Route AddRoute(string routeName, string cidr, RouteTable routeTable)
        {
            Route route = new Route(this, routeName, cidr, routeTable);
            return route;
        }

        public void AddParameter(ParameterBase parameter)
        {
            Parameters.Add(parameter.LogicalId,parameter);
        }
    }

    public class ParameterBase : Dictionary<string,object>, ILogicalId
    {
        public ParameterBase(string name, string type, object defaultValue)
        {
            LogicalId = name;
            this.Add("Type",type);
            this.Add("Default", defaultValue);

        }

        public string Type => this["Type"].ToString();
        public object Default => this["Default"];

        public string LogicalId { get; }
    }
}
