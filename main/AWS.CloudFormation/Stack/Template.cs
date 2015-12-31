﻿using System.Collections.Generic;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.Networking;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Stack
{
    public class Template
    {

        public const string AwsTemplateFormatVersion20100909 = "2010-09-09";
        public const string CIDR_IP_THE_WORLD = "0.0.0.0/0";

        public enum AvailabilityZone
        {
            None,
            UsEast1A
        }

        public Template()
        {
            AwsTemplateFormatVersion = AwsTemplateFormatVersion20100909;
            this.Resources = new Dictionary<string, ResourceBase>();
            this.Parameters = new Dictionary<string, ParameterBase>();
        }

        [JsonProperty(PropertyName = "AWSTemplateFormatVersion")]
        public string AwsTemplateFormatVersion { get; }

        public Dictionary<string, ResourceBase> Resources { get; private set; }
        public Dictionary<string, ParameterBase> Parameters { get; private set; }

        public Subnet AddSubnet(string name, Vpc vpc, string cidrBlock, AvailabilityZone availabilityZone)
        {
            var subnet = new Subnet(this, name)
            {
                Vpc = vpc,
                CidrBlock = cidrBlock,
                AvailabilityZone = Subnet.AVAILIBILITY_ZONE_US_EAST_1A
            };
            AddResource(subnet);
            return subnet;
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

        public void AddInstance(Instance.Instance resource)
        {
            AddResource(resource);
        }

        public void AddResource(ResourceBase resource)
        {
            this.Resources.Add(resource.Name, resource);
        }

        public ElasticIP AddElasticIp(string name, Instance.Instance instance)
        {
            ElasticIP eip = new ElasticIP(name) { Instance = instance };
            this.Resources.Add(name, eip);
            return eip;
        }

        public InternetGateway AddInternetGateway(string name, Vpc vpc)
        {
            InternetGateway gateway = new InternetGateway(this, name);
            this.AddResource(gateway);
            VPCGatewayAttachment attachment = new VPCGatewayAttachment(this, name + "Attachment")
            {
                InternetGateway = gateway,
                Vpc = vpc
            };
            this.AddResource(attachment);
            return gateway;
        }

        public RouteTable AddRouteTable(string key, Vpc vpc)
        {
            var routeTable = new RouteTable(this, key, vpc);
            this.AddResource(routeTable);
            return routeTable;
        }

        public Route AddRoute(string routeName, InternetGateway gateway, string destinationCidrBlock, RouteTable routeTable)
        {
            Route route = new Route(this, routeName, gateway, destinationCidrBlock, routeTable);
            this.AddResource(route);
            return route;
        }

        public Vpc AddVpc(Template template, string name, string cidrBlock)
        {
            Vpc vpc = new Vpc(template,name,cidrBlock);
            this.Resources.Add(name, vpc);
            return vpc;
        }

        public Route AddRoute(string routeName, string cidr, RouteTable routeTable)
        {
            Route route = new Route(this, routeName, cidr, routeTable);
            this.AddResource(route);
            return route;
        }

        public void AddParameter(ParameterBase parameter)
        {
            Parameters.Add(parameter.Name,parameter);
        }
    }

    public class ParameterBase : Dictionary<string,object>, IName
    {
        public ParameterBase(string name, string type, object defaultValue)
        {
            Name = name;
            this.Add("Type",type);
            this.Add("Default", defaultValue);

        }

        public string Type => this["Type"].ToString();
        public object Default => this["Default"];

        public string Name { get; }
    }
}
