﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using AWS.CloudFormation.Common;
using AWS.CloudFormation.Configuration.Packages;
using AWS.CloudFormation.Property;
using AWS.CloudFormation.Resource;
using AWS.CloudFormation.Resource.AutoScaling;
using AWS.CloudFormation.Resource.DirectoryService;
using AWS.CloudFormation.Resource.EC2;
using AWS.CloudFormation.Resource.EC2.Instancing;
using AWS.CloudFormation.Resource.EC2.Networking;
using AWS.CloudFormation.Resource.Networking;
using AWS.CloudFormation.Resource.RDS;
using AWS.CloudFormation.Resource.Wait;
using AWS.CloudFormation.Stack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperatingSystem = AWS.CloudFormation.Resource.EC2.Instancing.OperatingSystem;

namespace AWS.CloudFormation.Test
{
    [TestClass]
    public class StackTest
    {

        public const string CidrPrimeVpc = "10.0.0.0/16";
        public const string CidrPrimeActiveDirectorySubnet1 = "10.0.1.0/24";
        public const string CidrPrimeActiveDirectorySubnet2 = "10.0.2.0/24";
        public const string CidrPrimeDmz1Subnet = "10.0.3.0/24";
        public const string CidrPrimeNatGatewaySubnet = "10.0.5.0/24";
        public const string KeyPairName = "corp.getthebuybox.com";
        public const string UsEastWindows2012R2Ami = "ami-3d787d57";
        private const string UsEastWindows2012R2SqlServerExpressAmi = "ami-ff0f0a95";
        private const string UsEastWindows2012R2SqlServerStandardAmi = "ami-3d939d57";
        public const string UsEastWindows2012R2VisualStudioAmi = "ami-1d919f77";
        private const string BucketNameSoftware = "gtbb";
        private const string TopLevelDomainName = "yadayadasoftware.com";
        private const string FullyQualifiedDomainName = "prime." + TopLevelDomainName;


        public static Uri GetMasterTemplateUri(Create developmentCreate, Greek minDevelopment, Greek maxDevelopment)
        {

            var gitSuffix = $"{GetGitBranch()}:{GetGitHash()}";
            var description = $"Master Stack:{gitSuffix}";

            Template masterTemplate = new Template($"MasterStackYadaYadaSoftwareCom{DateTime.Now.Ticks}", description);
            Template primeTemplate = GetPrimeTemplate(gitSuffix);


            Uri primeUri = TemplateEngine.UploadTemplate(primeTemplate, "gtbb/templates");
            CloudFormation.Resource.CloudFormation.Stack prime =
                new CloudFormation.Resource.CloudFormation.Stack(primeUri);
            masterTemplate.Resources.Add("PrimeYadaYadaSoftwareCom", prime);


            var vpcPrime = new FnGetAtt("PrimeYadaYadaSoftwareCom", "Outputs.VpcPrime");

            for (Greek i = minDevelopment; i <= maxDevelopment; i++)
            {
                Template development = GetTemplateFullStack(StackTest.TopLevelDomainName, "prime", i, developmentCreate, gitSuffix);
                Uri developmentUri = TemplateEngine.UploadTemplate(development, "gtbb/templates");
                CloudFormation.Resource.CloudFormation.Stack devStack = new CloudFormation.Resource.CloudFormation.Stack(developmentUri);
                devStack.Parameters.Add("PrimeVpcId", vpcPrime);
                devStack.Parameters.Add("PrimeRouteTableForAdSubnets", new FnGetAtt("PrimeYadaYadaSoftwareCom", "Outputs.RouteTableForAdSubnets"));
                devStack.Parameters.Add("PrimeRouteTable4SubnetDmz1", new FnGetAtt("PrimeYadaYadaSoftwareCom", "Outputs.RouteTable4SubnetDmz1"));
                devStack.Parameters.Add("DhcpOptionsId", new FnGetAtt("PrimeYadaYadaSoftwareCom", "Outputs.DhcpOptionsId"));
                devStack.Parameters.Add(ActiveDirectoryBase.DomainAdminUsernameParameterName, new FnGetAtt("PrimeYadaYadaSoftwareCom", $"Outputs.{ActiveDirectoryBase.DomainAdminUsernameParameterName}"));
                devStack.Parameters.Add(ActiveDirectoryBase.DomainAdminPasswordParameterName, SettingsHelper.GetSetting("admin@prime.yadayadasoftware.com"));
                devStack.Parameters.Add(ActiveDirectoryBase.DomainFqdnParameterName, new FnGetAtt("PrimeYadaYadaSoftwareCom", $"Outputs.{ActiveDirectoryBase.DomainFqdnParameterName}"));
                masterTemplate.Resources.Add($"{i}DevYadaYadaSoftwareCom", devStack);
            }

            return TemplateEngine.UploadTemplate(masterTemplate, "gtbb/templates");
        }

        public static Uri GetPrimeTemplateUri(string gitSuffix)
        {
            return TemplateEngine.UploadTemplate(GetPrimeTemplate(gitSuffix), "gtbb/templates"); 
        }

        public static Template GetPrimeTemplate(string gitSuffix)
        {
            Template primeTemplate = new Template(FullyQualifiedDomainName, KeyPairName, "VpcPrime", CidrPrimeVpc, $"Stack for prime Vpc (AD):{gitSuffix}");

            Vpc vpc = primeTemplate.Vpcs.First();

            RouteTable routeTableForAdSubnets = new RouteTable(vpc);
            primeTemplate.Resources.Add("RouteTableForAdSubnets", routeTableForAdSubnets);

            Subnet subnetForActiveDirectory1 = new Subnet(vpc, CidrPrimeActiveDirectorySubnet1, AvailabilityZone.UsEast1A, routeTableForAdSubnets, null);
            primeTemplate.Resources.Add("SubnetAd1", subnetForActiveDirectory1);

            Subnet subnetForActiveDirectory2 = new Subnet(vpc, CidrPrimeActiveDirectorySubnet2, AvailabilityZone.UsEast1E, routeTableForAdSubnets, null);
            primeTemplate.Resources.Add("SubnetAd2", subnetForActiveDirectory2);

            string activeDirectoryAdminPassword = SettingsHelper.GetSetting("admin@prime.yadayadasoftware.com");
            string tfsServicePassword = SettingsHelper.GetSetting("tfsservice@prime.yadayadasoftware.com");

            var simpleAd = new SimpleActiveDirectory(FullyQualifiedDomainName,activeDirectoryAdminPassword,DirectorySize.MicrosoftAd, vpc,subnetForActiveDirectory1,subnetForActiveDirectory2);
            primeTemplate.Resources.Add(simpleAd.LogicalId, simpleAd);

            DhcpOptions dhcpOptions = new DhcpOptions(vpc,simpleAd);
            primeTemplate.Resources.Add(dhcpOptions.LogicalId, dhcpOptions);

            Output outputDhcpOptions = new Output("DhcpOptionsId",new ReferenceProperty(dhcpOptions.LogicalId));
            primeTemplate.Outputs.Add(outputDhcpOptions.LogicalId, outputDhcpOptions);

            Subnet subnetDmz = new Subnet(vpc, CidrPrimeDmz1Subnet, AvailabilityZone.UsEast1A, true);
            primeTemplate.Resources.Add("SubnetDmz1", subnetDmz);


            Instance instanceRdp = new Instance(subnetDmz, InstanceTypes.T2Micro, UsEastWindows2012R2Ami, OperatingSystem.Windows, Ebs.VolumeTypes.GeneralPurpose, 40);
            primeTemplate.Resources.Add("Rdp", instanceRdp);
            instanceRdp.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 50, false);
            instanceRdp.Packages.Add(new RemoteDesktopGatewayPackage());
            instanceRdp.DependsOn.Add(simpleAd.LogicalId);
            instanceRdp.DependsOn.Add(routeTableForAdSubnets.LogicalId);
            string ou = "OU=Users,OU=prime,DC=prime,DC=yadayadasoftware,DC=com";
            simpleAd.AddUser(instanceRdp, ou, new ReferenceProperty(TeamFoundationServerBuildServerBase.TfsServiceAccountNameParameterName), tfsServicePassword);
            simpleAd.AddUser(instanceRdp, ou, new ReferenceProperty(TeamFoundationServerBuildServerBase.TfsBuildAccountNameParameterName), new ReferenceProperty(TeamFoundationServerBuildServerBase.TfsBuildAccountPasswordParameterName));

            instanceRdp.Packages.Add(new WindowsShare("d:/backups", "backups", CidrPrimeVpc, new FnJoin(FnJoinDelimiter.None, "'", new ReferenceProperty(ActiveDirectoryBase.DomainNetBiosNameParameterName), "\\", new ReferenceProperty(TeamFoundationServerBuildServerBase.TfsServiceAccountNameParameterName), "'"), new FnJoin(FnJoinDelimiter.None, "'", new ReferenceProperty(ActiveDirectoryBase.DomainNetBiosNameParameterName), "\\Admins'")));


            primeTemplate.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainAdminUsernameParameterName, "String", simpleAd.AdministratorAccountName, "Admin username"));
            primeTemplate.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainAdminPasswordParameterName, "String", activeDirectoryAdminPassword, "Admin password"));
            primeTemplate.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainNetBiosNameParameterName, "String", "prime", "NetBIOS name of the domain for the stack.  (e.g. Dev,Test,Production)"));
            primeTemplate.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainFqdnParameterName, "String", FullyQualifiedDomainName, "Fully qualified domain name"));
            primeTemplate.Parameters.Add(new ParameterBase(TeamFoundationServerBuildServerBase.TfsServiceAccountNameParameterName, "String", "tfsservice", "Fully qualified domain name"));
            primeTemplate.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainTopLevelParameterName, "String", TopLevelDomainName, "Fully qualified domain name"));
            var tfsBuildPassword = SettingsHelper.GetSetting("tfsbuild@prime.yadayadasoftware.com");
            primeTemplate.Parameters.Add(new ParameterBase(TeamFoundationServerBuildServerBase.TfsBuildAccountPasswordParameterName, "String", tfsBuildPassword, "Password for tfsbuild account"));
            primeTemplate.Parameters.Add(new ParameterBase(TeamFoundationServerBuildServerBase.TfsBuildAccountNameParameterName, "String", "tfsbuild", "Name for tfsbuild account"));

            Output outputDomainAdminUserName = new Output(ActiveDirectoryBase.DomainAdminUsernameParameterName, new ReferenceProperty(ActiveDirectoryBase.DomainAdminUsernameParameterName));
            primeTemplate.Outputs.Add(outputDomainAdminUserName.LogicalId, outputDomainAdminUserName);

            Output outputFqdn = new Output(ActiveDirectoryBase.DomainFqdnParameterName, new ReferenceProperty(ActiveDirectoryBase.DomainFqdnParameterName));
            primeTemplate.Outputs.Add(outputFqdn.LogicalId, outputFqdn);


            return primeTemplate;

        }

        

        [TestMethod]
        public void GetPrimeTemplateTest()
        {
            var parentTemplate = GetPrimeTemplateUri(GetGitSuffix());
            Assert.IsNotNull(parentTemplate);
            Assert.IsFalse(string.IsNullOrEmpty(parentTemplate.AbsoluteUri));
        }


        [TestMethod]
        public void GetMasterTemplateTest()
        {
            var templateUri = GetMasterTemplateUri(Create.None, Greek.Alpha, Greek.Alpha);
        }

        [TestMethod]
        public void UpdatePrimeTest()
        {
            var primeUri = GetPrimeTemplateUri(GetGitSuffix());
            Stack.Stack.UpdateStack("StackYadaYadaSoftwareComMaster-template-StackPrime-ZEE4RL4MR8DQ",primeUri);

        }

        private static string GetGitSuffix()
        {
            return $"{GetGitBranch()}:{GetGitHash()}";
        }

        [TestMethod]
        public void CreateMasterTemplate()
        {
            var templateUri = GetMasterTemplateUri(Create.None, Greek.Alpha, Greek.Alpha);
            var response = Stack.Stack.CreateStack(templateUri);
        }

        [TestMethod]
        public void CreateStackWithCredentials()
        {
            var awsAccessKey = SettingsHelper.GetSetting("AWSAccessKey");
            var awsSecretKey = SettingsHelper.GetSetting("AWSSecretKey");
            var t = new Template("Test", "corp.getthebuybox.com", "VpcTest", CidrPrimeVpc);
            Uri templateUri = TemplateEngine.UploadTemplate(t, "gtbb");
            Stack.Stack.CreateStack(templateUri.AbsoluteUri, awsAccessKey,awsSecretKey);

        }

        [TestMethod]
        public void UpdateMasterTemplate()
        {
            var templateUri = GetMasterTemplateUri(Create.FullStack, Greek.Alpha, Greek.Alpha) ;
            Stack.Stack.UpdateStack("MasterStackYadaYadaSoftwareCom635945715836092476", templateUri);
        }

        public static Template GetTemplateFullStack(string topLevel, string appNameNetBiosName, Greek version, Create instancesToCreate, string gitSuffix)
        {
            var developmentVpcCidr = $"10.{(int)version}.0.0/16";
            var template = new Template($"{version}.{appNameNetBiosName}.{topLevel}", KeyPairName, $"Vpc{version}", developmentVpcCidr, $"{GetGitBranch()}:{GetGitHash()}" );

            template.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainAdminUsernameParameterName, "String", "admin", "Admin username"));
            template.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainAdminPasswordParameterName, "String", "invalid", "Admin password") { NoEcho = true });
            template.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainVersionParameterName, "String", version.ToString().ToLowerInvariant(), "Fully qualified domain name for the stack (e.g. example.com)"));
            template.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainNetBiosNameParameterName, "String", appNameNetBiosName, "NetBIOS name of the domain for the stack.  (e.g. Dev,Test,Production)"));
            template.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainFqdnParameterName, "String", $"{appNameNetBiosName}.{topLevel}", "Fully qualified domain name"));
            template.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainTopLevelParameterName, "String", topLevel, "Fully qualified domain name"));
            template.Parameters.Add(new ParameterBase(ActiveDirectoryBase.CidrPrimeDmz1SubnetParameterName, "String", CidrPrimeDmz1Subnet, "Cidr for PrimeDmz1 (Rdp)"));

            template.Parameters.Add(new ParameterBase(TeamFoundationServerBuildServerBase.TfsServiceAccountNameParameterName, "String", "tfsservice", "Account name for Tfs Application Server Service and Tfs SqlServer Service"));
            string tfsServicePassword = SettingsHelper.GetSetting("tfsservice@prime.yadayadasoftware.com");
            template.Parameters.Add(new ParameterBase(TeamFoundationServerBuildServerBase.TfsServicePasswordParameterName, "String", tfsServicePassword, "Passowrd for tfsservice account.") {NoEcho = true} );
            template.Parameters.Add(new ParameterBase(TeamFoundationServerBuildServerBase.sqlexpress4build_username_parameter_name, "String", "sqlservermasteruser", "Master User For RDS SqlServer"));
            template.Parameters.Add(new ParameterBase(TeamFoundationServerBuildServerBase.sqlexpress4build_password_parameter_name, "String", "askjd871hdj11", "Password for Master User For RDS SqlServer") { NoEcho = true });

            template.Parameters.Add(new ParameterBase("PrimeVpcId", "String","Invalid", "Prime VpcId"));
            template.Parameters.Add(new ParameterBase("PrimeRouteTableForAdSubnets", "String", "Invalid", "Prime RouteTable For Active Directory Subnets"));
            template.Parameters.Add(new ParameterBase("PrimeRouteTable4SubnetDmz1", "String", "Invalid", "Prime RouteTable For Dmz1 Subnet"));
            template.Parameters.Add(new ParameterBase("DhcpOptionsId", "String", "Invalid", "Id of DhcpOptions from Prime"));

            Vpc vpc = template.Vpcs.First();
            vpc.EnableDnsHostnames = true;
            vpc.EnableDnsSupport = true;

            VpcDhcpOptionsAssociation association = new VpcDhcpOptionsAssociation(new ReferenceProperty("DhcpOptionsId"), vpc);
            template.Resources.Add($"VpcDhcpOptionsAssociation", association);

            var vpcPeeringAlphaToPrime = new VpcPeeringConnection(vpc, new ReferenceProperty("PrimeVpcId"));
            template.Resources.Add($"Vpc{version}ToPrime", vpcPeeringAlphaToPrime);

            SecurityGroup natSecurityGroup = AddNatSecurityGroup(vpc, template);

            Subnet subnetDmz1 = AddDmz1(vpc, template, version);
            Subnet subnetDmz2 = AddDmz2(vpc, template, version);

            Instance nat1 = AddNat(template, subnetDmz1, natSecurityGroup);
            Instance nat2 = null;
            nat1.DependsOn.Add(vpc.VpcGatewayAttachment.LogicalId);

            RouteTable routeTableForSubnetsToNat1 = new RouteTable(vpc);
            template.Resources.Add($"RouteTableForPrivateSubnets", routeTableForSubnetsToNat1);

            Route routeFromAz1ToNat = new Route(Template.CidrIpTheWorld, routeTableForSubnetsToNat1);
            template.Resources.Add($"RouteForAz1", routeFromAz1ToNat);
            routeFromAz1ToNat.DestinationCidrBlock = "0.0.0.0/0";
            routeFromAz1ToNat.Instance = nat1;
            routeFromAz1ToNat.RouteTable = routeTableForSubnetsToNat1;

            SecurityGroup sqlServer4TfsSecurityGroup = AddSqlServer4TfsSecurityGroup(vpc, template);
            Subnet subnetSqlServer4Tfs = AddSubnetSqlServer4Tfs(vpc, routeTableForSubnetsToNat1, natSecurityGroup, template,version);

            Subnet subnetTfsServer = AddSubnetTfsServer(vpc, routeTableForSubnetsToNat1, natSecurityGroup, template,version);
            sqlServer4TfsSecurityGroup.AddIngress((ICidrBlock)subnetTfsServer, Protocol.Tcp, Ports.MsSqlServer);
            sqlServer4TfsSecurityGroup.AddIngress((ICidrBlock)subnetTfsServer, Protocol.Tcp, Ports.Smb);

            SecurityGroup tfsServerSecurityGroup = AddTfsServerSecurityGroup(vpc, template, subnetDmz1, subnetDmz2);

            Subnet subnetBuildServer = AddSubnet4BuildServer(vpc, routeTableForSubnetsToNat1, natSecurityGroup, template,version);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetBuildServer, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetBuildServer, Protocol.Tcp, Ports.TeamFoundationServerBuild);

            SecurityGroup securityGroupDb4Build = AddSecurityGroupDb4Build(vpc, template);
            securityGroupDb4Build.AddIngress((ICidrBlock)subnetBuildServer, Protocol.Tcp, Ports.MySql);
            securityGroupDb4Build.AddIngress((ICidrBlock)subnetBuildServer, Protocol.Tcp, Ports.MySql);

            SecurityGroup securityGroupSqlSever4Build = AddSecurityGroupSqlSever4Build(vpc, template);
            securityGroupSqlSever4Build.AddIngress((ICidrBlock)subnetBuildServer, Protocol.Tcp, Ports.MsSqlServer);

            Subnet subnetDatabase4BuildServer2 = AddSubnetDatabase4BuildServer2(vpc, template,version);

            SecurityGroup securityGroupBuildServer = AddSecurityGroupBuildServer(vpc, template);
            securityGroupBuildServer.AddIngress(CidrPrimeDmz1Subnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            securityGroupBuildServer.AddIngress((ICidrBlock)subnetTfsServer, Protocol.Tcp, Ports.TeamFoundationServerBuild);

            SecurityGroup workstationSecurityGroup = AddWorkstationSecurityGroup(vpc, template, subnetDmz1);
            tfsServerSecurityGroup.AddIngress(workstationSecurityGroup, Protocol.Tcp, Ports.TeamFoundationServerHttp);

            Subnet subnetWorkstation = AddSubnetWorkstation(vpc, routeTableForSubnetsToNat1, natSecurityGroup, template,version);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetWorkstation, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            tfsServerSecurityGroup.AddIngress((ICidrBlock)subnetWorkstation, Protocol.Tcp, Ports.TeamFoundationServerBuild);
            // give db access to the workstations
            securityGroupSqlSever4Build.AddIngress((ICidrBlock)subnetWorkstation, Protocol.Tcp, Ports.MsSqlServer);
            securityGroupDb4Build.AddIngress((ICidrBlock)subnetWorkstation, Protocol.Tcp, Ports.MySql);

            DbSubnetGroup mySqlSubnetGroupForDatabaseForBuild = new DbSubnetGroup("Second subnet for database for build server");
            template.Resources.Add("DbSubnetGroup4Build2Database", mySqlSubnetGroupForDatabaseForBuild);

            mySqlSubnetGroupForDatabaseForBuild.AddSubnet(subnetBuildServer);
            mySqlSubnetGroupForDatabaseForBuild.AddSubnet(subnetDatabase4BuildServer2);

            LaunchConfiguration instanceTfsSqlServer = null;

            // new route
            Route routeFromAlphaToPrime = new Route(vpcPeeringAlphaToPrime, CidrPrimeVpc, routeTableForSubnetsToNat1);
            template.Resources.Add("RouteFromAlphaToPrime", routeFromAlphaToPrime);
            routeFromAlphaToPrime.DependsOn.Add(routeTableForSubnetsToNat1.LogicalId);
            routeFromAlphaToPrime.DependsOn.Add(vpcPeeringAlphaToPrime.LogicalId);

            Route routeFromPrimeAdSubnetsToAlpha = new Route(vpcPeeringAlphaToPrime, developmentVpcCidr, new ReferenceProperty("PrimeRouteTableForAdSubnets"));
            template.Resources.Add("RouteFromPrimeAdSubnetsToAlpha", routeFromPrimeAdSubnetsToAlpha);
            routeFromPrimeAdSubnetsToAlpha.DependsOn.Add(routeTableForSubnetsToNat1.LogicalId);
            routeFromPrimeAdSubnetsToAlpha.DependsOn.Add(vpcPeeringAlphaToPrime.LogicalId);

            Route routeFromPrimeSubnetDmz1ToAlpha = new Route(vpcPeeringAlphaToPrime, developmentVpcCidr, new ReferenceProperty("PrimeRouteTable4SubnetDmz1"));
            template.Resources.Add("RouteFromPrimeSubnetDmz1ToAlpha", routeFromPrimeSubnetDmz1ToAlpha);
            routeFromPrimeSubnetDmz1ToAlpha.DependsOn.Add(routeTableForSubnetsToNat1.LogicalId);
            routeFromPrimeSubnetDmz1ToAlpha.DependsOn.Add(vpcPeeringAlphaToPrime.LogicalId);
            // new route

            if (instancesToCreate.HasFlag(Create.Sql4Tfs))
            {
                instanceTfsSqlServer = AddSql(template, $"Sql4Tfs{version}", InstanceTypes.T2Small, subnetSqlServer4Tfs, sqlServer4TfsSecurityGroup);
                instanceTfsSqlServer.DependsOn.Add(routeFromPrimeAdSubnetsToAlpha.LogicalId);
                instanceTfsSqlServer.DependsOn.Add(routeFromAz1ToNat.LogicalId);
                instanceTfsSqlServer.DependsOn.Add(routeFromAlphaToPrime.LogicalId);
                instanceTfsSqlServer.DependsOn.Add(vpcPeeringAlphaToPrime.LogicalId);
                instanceTfsSqlServer.DependsOn.Add(vpc.VpcGatewayAttachment.LogicalId);
            }

            LaunchConfiguration tfsServer = null;
            WaitCondition tfsApplicationTierInstalled = null;

            if (instancesToCreate.HasFlag(Create.Tfs))
            {
                tfsServer = AddTfsServer(template, InstanceTypes.T2Small, subnetTfsServer, instanceTfsSqlServer, tfsServerSecurityGroup,version);
                tfsServer.DependsOn.Add(routeFromPrimeAdSubnetsToAlpha.LogicalId);
                tfsServer.DependsOn.Add(routeFromAz1ToNat.LogicalId);
                tfsServer.DependsOn.Add(routeFromAlphaToPrime.LogicalId);

                //tfsServer.DependsOn.Add(simpleAd.LogicalId);
                ActiveDirectoryBase.AddInstanceToDomain(tfsServer.RenameConfig);

                var package =  tfsServer.Packages.OfType<TeamFoundationServerApplicationTier>().FirstOrDefault();
                if (package != null)
                {
                    tfsApplicationTierInstalled = package.WaitCondition;
                }
            }

            DbSubnetGroup subnetGroupSqlExpress4Build = new DbSubnetGroup("DbSubnet Group for SQL Server database for build server");
            template.Resources.Add("SubnetGroup4Build2SqlServer", subnetGroupSqlExpress4Build);

            subnetGroupSqlExpress4Build.AddSubnet(subnetBuildServer);
            subnetGroupSqlExpress4Build.AddSubnet(subnetDatabase4BuildServer2);

            DbInstance mySql4Build = null;

            const string mySqlMasterUserName = "mysqlmasteruser";
            const string mySqlPassword = "thisismypassword";

            if (instancesToCreate.HasFlag(Create.MySql4Build))
            {
                mySql4Build = new DbInstance(
                    DbInstanceClassEnum.DbT2Micro,
                    EngineType.MySql,
                    LicenseModelType.GeneralPublicLicense,
                    Ebs.VolumeTypes.GeneralPurpose,
                    20,
                    mySqlMasterUserName,
                    mySqlPassword)
                {
                    DBSubnetGroupName = new ReferenceProperty(subnetGroupSqlExpress4Build)
                };
                template.Resources.Add("MySql4Build", mySql4Build);
                mySql4Build.AddVpcSecurityGroup(securityGroupSqlSever4Build);
            }


            DbInstance rdsSqlExpress4Build = null;

            if (instancesToCreate.HasFlag(Create.SqlServer4Build))
            {
                rdsSqlExpress4Build = new DbInstance(DbInstanceClassEnum.DbT2Micro,
                    EngineType.SqlServerExpress,
                    LicenseModelType.LicenseIncluded,
                    Ebs.VolumeTypes.GeneralPurpose,
                    30,
                    new ReferenceProperty(TeamFoundationServerBuildServerBase.sqlexpress4build_username_parameter_name),
                    new ReferenceProperty(TeamFoundationServerBuildServerBase.sqlexpress4build_password_parameter_name))
                {
                    DBSubnetGroupName = new ReferenceProperty(subnetGroupSqlExpress4Build)
                };
                template.Resources.Add("SqlServer4Build", rdsSqlExpress4Build);
                rdsSqlExpress4Build.AddVpcSecurityGroup(securityGroupSqlSever4Build);
            }

            if (instancesToCreate.HasFlag(Create.Tfs) && instancesToCreate.HasFlag(Create.Build))
            {
                var buildServer = AddBuildServer(template, InstanceTypes.T2Small, subnetBuildServer, tfsServer, tfsApplicationTierInstalled, securityGroupBuildServer, rdsSqlExpress4Build);
                buildServer.DependsOn.Add(routeFromPrimeAdSubnetsToAlpha.LogicalId);
                buildServer.DependsOn.Add(routeFromAz1ToNat.LogicalId);
                buildServer.DependsOn.Add(routeFromAlphaToPrime.LogicalId);




                //buildServer.DependsOn.Add(simpleAd.LogicalId);
                ActiveDirectoryBase.AddInstanceToDomain(buildServer.RenameConfig);

            }

            if (instancesToCreate.HasFlag(Create.Workstation))
            {
                //uses 33gb
                var workstation = AddWorkstation(template, subnetWorkstation, workstationSecurityGroup, version);
                workstation.DependsOn.Add(routeFromPrimeAdSubnetsToAlpha.LogicalId);
                workstation.DependsOn.Add(routeFromAz1ToNat.LogicalId);
                workstation.DependsOn.Add(routeFromAlphaToPrime.LogicalId);

                //workstation.DependsOn.Add(simpleAd.LogicalId);
                ActiveDirectoryBase.AddInstanceToDomain(workstation.RenameConfig);
            }

            //////SecurityGroup elbSecurityGroup = new SecurityGroup(template, "ElbSecurityGroup", "Enables access to the ELB", vpc);
            //////elbSecurityGroup.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.TeamFoundationServerHttp);
            //////tfsServerSecurityGroup.AddIngress(elbSecurityGroup, Protocol.Tcp, Ports.TeamFoundationServerHttp);

            //////////////LoadBalancer elb = new LoadBalancer(template, "elb1");
            //////////////elb.AddInstance(tfsServer);
            //////////////elb.AddListener("8080", "8080", "http");
            //////////////elb.AddSubnet(DMZSubnet);
            //////////////elb.AddSecurityGroup(elbSecurityGroup);
            //////////////template.AddResource(elb);

            return template;
        }


        public static Template GetTemplateWithParameters()
        {
            var template = new Template("StackWithParameters", KeyPairName, "Vpc", "10.1.0.0/16");
            var password = System.Web.Security.Membership.GeneratePassword(8, 0);
            var domainPassword = new ParameterBase(Template.ParameterDomainAdminPassword, "String", password, "Password for domain administrator.")
            {
                NoEcho = true
            };

            template.Parameters.Add(Template.ParameterDomainAdminPassword, domainPassword);
            return template;
        }

        [TestMethod]
        public void TestParameters()
        {
            var template = GetTemplateWithParameters();
            CreateTestStack(template, this.TestContext);
        }

        [Flags]
        public enum Create
        {
            None = 0,
            Sql4Tfs = 1, //1
            Tfs = Sql4Tfs + 2, //2
            Build = Tfs + 4,
            SqlServer4Build = Build + 8,
            MySql4Build = 16,
            Workstation = 32,
            FullStack = int.MaxValue
        }

        public static Subnet AddSubnetWorkstation(Vpc vpc, RouteTable nat1, SecurityGroup natSecurityGroup, Template template,Greek version)
        {
            var subnetWorkstation = new Subnet(vpc, $"10.{((int)version)}.7.0/24", AvailabilityZone.UsEast1A, nat1, natSecurityGroup);
            template.Resources.Add("Subnet4Workstation", subnetWorkstation);
            return subnetWorkstation;
        }

        private static SecurityGroup AddWorkstationSecurityGroup(Vpc vpc, Template template, Subnet subnetDmz1)
        {
            SecurityGroup workstationSecurityGroup = new SecurityGroup("Security Group To Contain Workstations", vpc);
            template.Resources.Add("SecurityGroup4Workstation", workstationSecurityGroup);
            workstationSecurityGroup.AddIngress(CidrPrimeDmz1Subnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            return workstationSecurityGroup;
        }

        private static SecurityGroup AddSecurityGroupBuildServer(Vpc vpc, Template template)
        {
            SecurityGroup securityGroupBuildServer = new SecurityGroup("Allows build controller to build agent communication",
                vpc);
            template.Resources.Add("SecurityGroup4BuildServer", securityGroupBuildServer);
            return securityGroupBuildServer;
        }

        private static Subnet AddSubnetDatabase4BuildServer2(Vpc vpc, Template template,Greek version)
        {
            var subnetDatabase4BuildServer2 = new Subnet(vpc, $"10.{((int)version)}.6.0/24", AvailabilityZone.UsEast1E, false);
            template.Resources.Add("Subnet4Build2Database", subnetDatabase4BuildServer2);
            return subnetDatabase4BuildServer2;
        }

        private static SecurityGroup AddSecurityGroupSqlSever4Build(Vpc vpc, Template template)
        {
            SecurityGroup securityGroupSqlSever4Build = new SecurityGroup("Allows communication to SqlServer", vpc);
            template.Resources.Add("SecurityGroup4Build2SqlSever", securityGroupSqlSever4Build);
            return securityGroupSqlSever4Build;
        }

        private static SecurityGroup AddSecurityGroupDb4Build(Vpc vpc, Template template)
        {
            SecurityGroup securityGroupDb4Build = new SecurityGroup("Allows communication to Db", vpc);
            template.Resources.Add("SecurityGroup4Build2Db", securityGroupDb4Build);
            return securityGroupDb4Build;
        }

        private static Subnet AddSubnet4BuildServer(Vpc vpc, RouteTable nat1, SecurityGroup natSecurityGroup, Template template,Greek version)
        {
            var subnetBuildServer = new Subnet(vpc, $"10.{((int)version)}.5.0/24", AvailabilityZone.UsEast1A, nat1, natSecurityGroup);
            template.Resources.Add("Subnet4BuildServer", subnetBuildServer);
            return subnetBuildServer;
        }

        private static SecurityGroup AddTfsServerSecurityGroup(Vpc vpc, Template template, Subnet subnetDmz1, Subnet subnetDmz2)
        {
            SecurityGroup tfsServerSecurityGroup = new SecurityGroup("Allows various TFS communication", vpc);
            template.Resources.Add("SecurityGroup4TfsServer", tfsServerSecurityGroup);
            tfsServerSecurityGroup.AddIngress(CidrPrimeDmz1Subnet, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            tfsServerSecurityGroup.AddIngress((ICidrBlock) subnetDmz2, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            return tfsServerSecurityGroup;
        }

        private static Subnet AddSubnetTfsServer(Vpc vpc, RouteTable nat1, SecurityGroup natSecurityGroup, Template template, Greek version)
        {
            var subnetTfsServer = new Subnet(vpc, $"10.{((int)version)}.4.0/24", AvailabilityZone.UsEast1A, nat1, natSecurityGroup);
            template.Resources.Add("Subnet4TfsServer", subnetTfsServer);
            return subnetTfsServer;
        }

        private static Subnet AddDmz1(Vpc vpc, Template template, Greek version)
        {
            Subnet subnetDmz1 = new Subnet(vpc, $"10.{((int)version)}.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("SubnetDmz1", subnetDmz1);
            return subnetDmz1;
        }

        private static Subnet AddDmz2(Vpc vpc, Template template, Greek version)
        {
            Subnet subnetDmz2 = new Subnet(vpc, $"10.{((int)version)}.2.0/24", AvailabilityZone.UsEast1E, true);
            template.Resources.Add("SubnetDmz2", subnetDmz2);
            return subnetDmz2;
        }



        private static Subnet AddSubnetSqlServer4Tfs(Vpc vpc, RouteTable nat1, SecurityGroup natSecurityGroup, Template template, Greek version)
        {
            var subnetSqlServer4Tfs = new Subnet(vpc, $"10.{((int)version)}.3.0/24", AvailabilityZone.UsEast1A, nat1, natSecurityGroup);
            template.Resources.Add("Subnet4SqlServer4Tfs", subnetSqlServer4Tfs);
            return subnetSqlServer4Tfs;
        }

        private static SecurityGroup AddSqlServer4TfsSecurityGroup(Vpc vpc, Template template)
        {
            SecurityGroup sqlServer4TfsSecurityGroup = new SecurityGroup("Allows communication to SQLServer Service", vpc);
            template.Resources.Add("SecurityGroup4SqlServer4Tfs", sqlServer4TfsSecurityGroup);
            sqlServer4TfsSecurityGroup.AddIngress(IPNetwork.Parse("10.0.0.0", 16), Protocol.Tcp, Ports.RemoteDesktopProtocol);
            sqlServer4TfsSecurityGroup.AddIngress(IPNetwork.Parse("0.0.0.0", 0), Protocol.Tcp, Ports.Min, Ports.Max);
            sqlServer4TfsSecurityGroup.AddIngress(IPNetwork.Parse("0.0.0.0", 0), Protocol.Udp, Ports.Min, Ports.Max);
            sqlServer4TfsSecurityGroup.AddIngress(IPNetwork.Parse("0.0.0.0", 0), Protocol.Icmp, Ports.Ping);
            return sqlServer4TfsSecurityGroup;
        }

        public static SecurityGroup AddNatSecurityGroup(Vpc vpc, Template template)
        {
            SecurityGroup natSecurityGroup =
                new SecurityGroup(
                    "Enables Ssh access to NAT1 in AZ1 via port 22 and outbound internet access via private subnets", vpc);
            template.Resources.Add("SecurityGroup4Nat", natSecurityGroup);
            natSecurityGroup.AddIngress(PredefinedCidr.LocalGateway, Protocol.Tcp, Ports.Ssh);
            natSecurityGroup.AddIngress(PredefinedCidr.LocalGateway, Protocol.Icmp, Ports.All);
            return natSecurityGroup;
        }

        //internal static string GetPassword()
        //{
        //    var random = new Random(((int) DateTime.Now.Ticks%int.MaxValue));

        //    string password = string.Empty;

        //    for (int i = 0; i < 4; i++)
        //    {
        //        char charToAdd = ((char) random.Next((int) 'A', (int) 'Z'));
        //        password += charToAdd;
        //    }

        //    for (int i = 0; i < 4; i++)
        //    {
        //        char charToAdd = ((char) random.Next((int) '0', (int) '9'));
        //        password += charToAdd;
        //    }

        //    for (int i = 0; i < 4; i++)
        //    {
        //        char charToAdd = ((char) random.Next((int) 'a', (int) 'z'));
        //        password += charToAdd;
        //    }
        //    return password;
        //}

        private static string GetGitBranch()
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "git.exe";
            p.StartInfo.Arguments = "rev-parse --symbolic-full-name --abbrev-ref HEAD";
            p.Start();

            // To avoid deadlocks, always read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            byte newLineByte = 10;
            char newLine = (char)newLineByte;
            byte nullByte = 0;
            char nullChar = (char)nullByte;
            return output.Replace(newLine.ToString(), string.Empty);
        }

        public static Instance AddRdp2(Subnet subnetDmz1, Template template, Vpc vpc)
        {
            var instanceRdp2 = new Instance(subnetDmz1, InstanceTypes.T2Nano, "ami-e4034a8e", OperatingSystem.Windows);

            template.Resources.Add("rdp2", instanceRdp2);

            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("SecurityGroupForRdp2Rdp2", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);

            instanceRdp2.SecurityGroupIds.Add(new ReferenceProperty(rdp));
            instanceRdp2.AddElasticIp();

            return instanceRdp2;
        }

        private static LaunchConfiguration AddSql(Template template, string instanceName, InstanceTypes instanceSize, 
            Subnet subnet, SecurityGroup sqlServerSecurityGroup)
        {
            var sqlServer = new Instance(subnet, instanceSize, UsEastWindows2012R2SqlServerStandardAmi, OperatingSystem.Windows, Ebs.VolumeTypes.GeneralPurpose, 70);
            template.Resources.Add(instanceName,sqlServer);
            var sqlServerPackage = new SqlServerExpressFromAmi(BucketNameSoftware);
            sqlServer.Packages.Add(sqlServerPackage);
            sqlServer.SecurityGroupIds.Add(new ReferenceProperty(sqlServerSecurityGroup));
            return sqlServer;
        }

        private static Instance AddDomainController(Template template, Subnet subnet)
        {
            //"ami-805d79ea",
            var DomainController = new Instance(subnet,InstanceTypes.C4Large, UsEastWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add("DomainController", DomainController);

            return DomainController;
        }

        public Template GetTemplateVolumeOnly(TestContext testContext)
        {
            Template t = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            Volume v = new Volume();
            t.Resources.Add("Volume1",v);
            v.SnapshotId = "snap-c4d7f7c3";
            v.AvailabilityZone = AvailabilityZone.UsEast1A;

            return t;
        }

        [TestMethod]
        public void CreateAutoScalingGroupTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);


            var launchConfig = new LaunchConfiguration(null, InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows, ResourceType.AwsAutoScalingLaunchConfiguration,false);
            template.Resources.Add("Xyz", launchConfig );
            launchConfig.AssociatePublicIpAddress = true;
            launchConfig.SecurityGroups.Add(new ReferenceProperty(rdp));



            var launchGroup = new AutoScalingGroup();
            template.Resources.Add("AutoGroup",launchGroup);
            launchGroup.LaunchConfiguration = launchConfig;
            launchGroup.MinSize = 1.ToString();
            launchGroup.MaxSize = 2.ToString();
            launchGroup.AddAvailabilityZone(AvailabilityZone.UsEast1A);
            launchGroup.AddSubnetToVpcZoneIdentifier(DMZSubnet);
            Stack.Stack.CreateStack(template,this.TestContext.TestName + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty));
        }

        [TestMethod]
        public void GetHashOfSourceTest()
        {
            var r = GetGitHash();

            StringAssert.Contains(r, "0");

            Assert.AreEqual("0e34235e264c315ab1efa46d3316d84ca21a688f".Length,r.Length);
        }

        public static string GetGitHash()
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "git.exe";
            p.StartInfo.Arguments = "rev-parse HEAD";
            p.Start();

            // To avoid deadlocks, always read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            byte newLineByte = 10;
            char newLine = (char)newLineByte;
            byte nullByte = 0;
            char nullChar = (char) nullByte;
            return output.Replace(newLine.ToString(), string.Empty);

        }

        [TestMethod]
        public void GetGitDiffTest()
        {
            //
            Assert.IsTrue(HasGitDifferences());
        }

        public static bool HasGitDifferences()
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "git.exe";
            p.StartInfo.Arguments = "diff";
            p.Start();

            // To avoid deadlocks, always read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            return output.Length != 0;
        }

        [TestMethod]
        public void CreateStackVolumeTest()
        {
            Stack.Stack.CreateStack(GetTemplateVolumeOnly(this.TestContext));
        }


        [TestMethod]
        public void TestCidr()
        {
            IPNetwork n = IPNetwork.Parse("8.8.8.8");
            Assert.AreEqual("x",n.Network);

        }
        [TestMethod]
        public void CreateStackVolumeAttachmentTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet,InstanceTypes.T2Large, UsEastWindows2012R2Ami,OperatingSystem.Windows,Ebs.VolumeTypes.GeneralPurpose, 50);

            template.Resources.Add("workstation",w);
            w.SecurityGroupIds.Add(new ReferenceProperty(rdp));
            w.AddElasticIp();
            Volume v = new Volume(1);
            template.Resources.Add("Volume1",v);
            w.AddDisk(v);
            template.Resources.Add("VolumeAttachment1",v);
            Stack.Stack.CreateStack(template);
        }

        [TestMethod]
        public void CreateStackBlockDeviceMappingFromSnapshotTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(w.LogicalId,w);
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "/dev/xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.BlockDeviceMappings.Add(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "/dev/xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.BlockDeviceMappings.Add(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "/dev/xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.BlockDeviceMappings.Add(blockDeviceMapping);
            w.SecurityGroupIds.Add(new ReferenceProperty(rdp));

            w.AddElasticIp();
            Stack.Stack.CreateStack(template);
        }

        [TestMethod]
        public void CreateStackWithMountedSqlTfsVsIsosTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet( vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(w.LogicalId, w);

            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-87e3eb87";
            w.BlockDeviceMappings.Add(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-5e27a85a";
            w.BlockDeviceMappings.Add(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-4e69d94b";
            w.BlockDeviceMappings.Add(blockDeviceMapping);

            //w.AddChefExec(BucketNameSoftware, "MountDrives.tar.gz", "MountDrives");
            //w.AddSecurityGroup(rdp);
            //w.AddElasticIp();
            //var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".",string.Empty) ;
            //Stack.Stack.CreateStack(template, name);
        }

        [TestMethod]
        public void UpdateStackWithVisualStudio()
        {
            var template = GetNewBlankTemplateWithVpc($"VpcCreateStackWithVisualStudio");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows );
            template.Resources.Add(w.LogicalId, w);


            w.Packages.Add(new VisualStudio(BucketNameSoftware));
            w.Packages.Add(new SqlServerExpress(BucketNameSoftware));

            w.SecurityGroupIds.Add(new ReferenceProperty(rdp));
            w.AddElasticIp();
            var name = "CreateStackWithVisualStudio-2016-01-30T1711018619021-0500";
            Stack.Stack.UpdateStack(name, template);
        }

        [TestMethod]
        public void CreateStackWithSimpleCommand()
        {
            var template = GetCreateStackWithSimpleCommand();
            var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);
            Stack.Stack.CreateStack(template, name);
        }
        [TestMethod]
        public void UpdateStackWithSimpleCommand()
        {
            var template = GetCreateStackWithSimpleCommand();
            var name = "CreateStackWithSimpleCommand-2016-01-31T2139494959420-0500";
            Stack.Stack.UpdateStack(name,template);
        }

        private static Template GetCreateStackWithSimpleCommand()
        {
            var template = GetNewBlankTemplateWithVpc($"VpcCreateStackWithVisualStudio");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet,InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(w.LogicalId,w);

            Dir1 d = new Dir1();
            w.Packages.Add(d);
            WaitCondition wc = d.WaitCondition;
            Dir2 d2 = new Dir2();
            w.Packages.Add(d2);
            WaitCondition wc2 = d2.WaitCondition;
            w.SecurityGroupIds.Add(new ReferenceProperty(rdp));
            w.AddElasticIp();
            return template;
        }


        [TestMethod]
        public void CreateStackWithVisualStudio()
        {
            var template = GetNewBlankTemplateWithVpc($"VpcCreateStackWithVisualStudio");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows );
            template.Resources.Add(w.LogicalId, w);


            w.Packages.Add(new VisualStudio(BucketNameSoftware));
            w.SecurityGroupIds.Add(new ReferenceProperty(rdp));
            w.AddElasticIp();
            var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);
            Stack.Stack.CreateStack(template, name);
        }

        [TestMethod]
        public void CreateDeveloperWorkstation()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = AddWorkstation(template, DMZSubnet, rdp, Greek.Alpha);
            w.AddElasticIp();
            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateGenericInstance()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(w.LogicalId,w);
            w.SecurityGroupIds.Add(new ReferenceProperty(rdp));
            w.AddElasticIp();

            CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        public void CreateBuildServer()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            var dc1 = AddDomainController(template, DMZSubnet);
            dc1.AddElasticIp();
            dc1.SecurityGroupIds.Add(new ReferenceProperty(rdp));
            var w = AddBuildServer(template, InstanceTypes.T2Nano,  DMZSubnet, null, null, rdp,null);
            throw new NotImplementedException();
            //w.AddElasticIp();

            //CreateTestStack(template, this.TestContext);

        }


        [TestMethod]
        public void CreateMinimalInstanceTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);


            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows,false);
            template.Resources.Add("w", w);

            var configSet = w.Metadata.Init.ConfigSets.GetConfigSet("TestVsix").GetConfig("TestVsix");
            configSet.Files.GetFile("c:/cfn/files/PowerShellTools.14.0.vsix").Source =
                "https://visualstudiogallery.msdn.microsoft.com/c9eb3ba8-0c59-4944-9a62-6eee37294597/file/199313/2/PowerShellTools.14.0.vsix";


            w.SecurityGroupIds.Add(new ReferenceProperty(rdp));
            w.AddElasticIp();

            //SecurityGroup blah = new SecurityGroup("blah",template.Vpcs.Last());
            //template.Resources.Add("blah",blah);
            CreateTestStack(template, this.TestContext);
            //Stack.Stack.UpdateStack("CreateMinimalInstanceTest-active-directory-backup-2016-02-07-15-04-25", template);

        }


        [TestMethod]
        public void MakeSqlServerAmiTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            template.StackName = $"{this.TestContext.TestName}{DateTime.Now.Ticks}";
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);


            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows, false);
            template.Resources.Add("w", w);
            w.Packages.Add(new SqlServerStandard(BucketNameSoftware));

            w.SecurityGroupIds.Add(new ReferenceProperty(rdp));
            w.AddElasticIp();

            template.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainAdminPasswordParameterName, "String", "invalid", "for testing"));
            template.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainAdminUsernameParameterName, "String", "invalid", "for testing"));
            template.Parameters.Add(new ParameterBase(ActiveDirectoryBase.DomainNetBiosNameParameterName, "String", "invalid", "for testing"));
            
            CreateTestStack(template, this.TestContext);

        }


        [TestMethod]
        public void MakeVisualStudioAmiTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            template.StackName = $"{this.TestContext.TestName}{DateTime.Now.Ticks}";
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);


            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows, false);
            template.Resources.Add("w", w);
            w.Packages.Add(new VisualStudio(BucketNameSoftware));

            w.SecurityGroupIds.Add(new ReferenceProperty(rdp));
            w.AddElasticIp();


            CreateTestStack(template, this.TestContext);

        }


        [TestMethod]
        public void CreateISOMaker()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);


            Instance workstation = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(workstation.LogicalId, workstation);

            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(workstation, "/dev/sda1");
            blockDeviceMapping.Ebs.VolumeType = Ebs.VolumeTypes.GeneralPurpose;
            blockDeviceMapping.Ebs.VolumeSize = 30;
            workstation.BlockDeviceMappings.Add(blockDeviceMapping);
            workstation.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 6);


            workstation.SecurityGroupIds.Add(new ReferenceProperty(rdp));



            workstation.AddElasticIp();

            CreateTestStack(template, this.TestContext);
        }

        //ISOMaker

        [TestMethod]
        public void CreateSubnetTest()
        {
            throw new NotImplementedException();
            //var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            //var vpc = template.Vpcs.First();
            //var subnet1 = new Subnet(vpc, CidrDmz1, AvailabilityZone.UsEast1A, true);
            //template.Resources.Add("subnet1", subnet1);

            //var subnet2 = new Subnet(vpc, CidrDmz2, AvailabilityZone.UsEast1A, true);
            //template.Resources.Add("subnet2", subnet2);

            //var subnet3 = new Subnet(vpc, CidrDomainController1Subnet, AvailabilityZone.UsEast1A, false);
            //template.Resources.Add("subnet3", subnet3);

            //var subnet4 = new Subnet(vpc, CidrDomainController2Subnet, AvailabilityZone.UsEast1A, false);
            //template.Resources.Add("subnet4", subnet4);


            //CreateTestStack(template, this.TestContext);

        }

        [TestMethod]
        [Timeout(int.MaxValue)]
        public void CfnInitOverWriteTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);


            Instance workstation = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(workstation.LogicalId, workstation);

            workstation.SecurityGroupIds.Add(new ReferenceProperty(rdp));
            workstation.AddElasticIp();


            var name = this.TestContext.TestName + "-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);

            CreateTestStack(template, this.TestContext, name);

            throw new NotImplementedException();
            //workstation.Metadata.Init.ConfigSets.GetConfigSet("x").GetConfig("y").Commands.AddCommand<Command>("z").Command.AddCommandLine(true, "dir");

            //Thread.Sleep(new TimeSpan(0, 60, 0));
            //Stack.Stack.UpdateStack(name, template);
        }


        [TestMethod]
        public void SerializerTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("Allows Remote Desktop Access", vpc);
            template.Resources.Add("SecurityGroupRdp", rdp);

            System.Diagnostics.Debug.WriteLine(rdp.Vpc);
            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            
            Subnet DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            RouteTable dmzRouteTable = new RouteTable(vpc);
            template.Resources.Add("DMZRouteTable", dmzRouteTable);

            Route dmzRoute = new Route(vpc.InternetGateway, "0.0.0.0/0", dmzRouteTable);
            template.Resources.Add("DMZRoute", dmzRoute);

            SubnetRouteTableAssociation DMZSubnetRouteTableAssociation = new SubnetRouteTableAssociation(DMZSubnet, dmzRouteTable);
            template.Resources.Add(DMZSubnetRouteTableAssociation.LogicalId, DMZSubnetRouteTableAssociation);
            Instance workstation = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(workstation.LogicalId, workstation);

            workstation.SecurityGroupIds.Add(new ReferenceProperty(rdp));
            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(workstation, "/dev/sda1");
            blockDeviceMapping.Ebs.VolumeType = Ebs.VolumeTypes.GeneralPurpose;
            blockDeviceMapping.Ebs.VolumeSize = 30;
            workstation.BlockDeviceMappings.Add(blockDeviceMapping);
            workstation.AddDisk(Ebs.VolumeTypes.GeneralPurpose, 6);
            workstation.AddElasticIp();


            CreateTestStack(template, this.TestContext);

        }


        public static void CreateTestStack(Template template, TestContext context)
        {
            var name = template.StackName;
            if (string.IsNullOrEmpty(name))
            {
                name = $"{context.TestName}{DateTime.Now.Ticks}";
                using ( var r = new StreamReader(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".git", "HEAD")))
                {
                    var line = r.ReadLine();
                    var parts = line.Split('/');
                    name += $"-{parts[parts.Length - 1]}-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}";
                }
            }
            CreateTestStack(template, context, name);

        }
        public static void CreateTestStack(Template template, TestContext context, string name)
        {
            Stack.Stack.CreateStack(template, name);

        }



        [TestMethod]
        public void CreateStackWithMounterTest()
        {
            var template = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var vpc = template.Vpcs.First();
            SecurityGroup rdp = new SecurityGroup("rdp", vpc);
            template.Resources.Add("rdp", rdp);

            rdp.AddIngress(PredefinedCidr.TheWorld, Protocol.Tcp, Ports.RemoteDesktopProtocol);
            var DMZSubnet = new Subnet(vpc, "10.1.1.0/24", AvailabilityZone.UsEast1A, true);
            template.Resources.Add("DMZSubnet", DMZSubnet);

            Instance w = new Instance(DMZSubnet, InstanceTypes.T2Nano, UsEastWindows2012R2Ami, OperatingSystem.Windows);
            template.Resources.Add(w.LogicalId, w);

            BlockDeviceMapping blockDeviceMapping = new BlockDeviceMapping(w, "xvdf");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.BlockDeviceMappings.Add(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdg");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.BlockDeviceMappings.Add(blockDeviceMapping);

            blockDeviceMapping = new BlockDeviceMapping(w, "xvdh");
            blockDeviceMapping.Ebs.SnapshotId = "snap-b3fe64a9";
            w.BlockDeviceMappings.Add(blockDeviceMapping);
            throw new NotImplementedException();
            //w.AddChefExec(BucketNameSoftware, "MountDrives.tar.gz", "MountDrives");


            //w.AddSecurityGroup(rdp);
            //w.AddElasticIp();
            //var name = "CreateStackWithMounterTest-" + DateTime.Now.ToString("O").Replace(":", string.Empty).Replace(".", string.Empty);
            //Stack.Stack.CreateStack(template, name);
        }


        private static LaunchConfiguration AddBuildServer(
            Template template, 
            InstanceTypes instanceSize, 
            Subnet subnet,
            LaunchConfiguration tfsServer,
            WaitCondition tfsServerComplete, 
            SecurityGroup buildServerSecurityGroup, 
            DbInstance sqlExpress4Build)
        {

            AutoScalingGroup launchGroup = new AutoScalingGroup();
            template.Resources.Add("BuildServerAutoScalingGroup",launchGroup);
            launchGroup.MinSize = 1.ToString();
            launchGroup.MaxSize = 2.ToString();
            launchGroup.AddAvailabilityZone(AvailabilityZone.UsEast1A);
            launchGroup.AddSubnetToVpcZoneIdentifier(subnet);


            var buildServer = new LaunchConfiguration(null,instanceSize, UsEastWindows2012R2VisualStudioAmi, OperatingSystem.Windows, ResourceType.AwsAutoScalingLaunchConfiguration,false);
            template.Resources.Add("LaunchConfigurationBuildServer",buildServer);

            launchGroup.LaunchConfiguration = buildServer;
            buildServer.SecurityGroups.Add(new ReferenceProperty(buildServerSecurityGroup));

            buildServer.AddBlockDeviceMapping("/dev/sda1", 100, Ebs.VolumeTypes.GeneralPurpose);

            buildServer.Packages.Add(new VisualStudio(BucketNameSoftware));
            buildServer.Packages.Add(new TeamFoundationServerBuildServerAgentOnly(TeamFoundationServerEdition.Standard2015Update1, tfsServer, BucketNameSoftware, sqlExpress4Build));
            buildServer.Packages.Add(new AmazonAwsCli());
            buildServer.Packages.Add(new TfsCrossPlatformCommandLineInterface());

            if (tfsServerComplete != null)
            {
                buildServer.AddDependsOn(tfsServerComplete);
            }

            var x = buildServer.Packages.Last().WaitCondition;

            return buildServer;
        }


        private static Instance AddWorkstation(Template template, Subnet subnet, SecurityGroup workstationSecurityGroup, Greek version)
        {
            if (subnet == null) throw new ArgumentNullException(nameof(subnet));

            Instance workstation = new Instance(subnet, InstanceTypes.T2Large, UsEastWindows2012R2VisualStudioAmi, OperatingSystem.Windows, Ebs.VolumeTypes.GeneralPurpose, 214);
            template.Resources.Add($"Work{version}",workstation);

            if (workstationSecurityGroup != null)
            {
                workstation.SecurityGroupIds.Add(new ReferenceProperty(workstationSecurityGroup));
            }

            //workstation.Packages.Add(new SqlServerExpress(BucketNameSoftware));
            workstation.Packages.Add(new Iis(BucketNameSoftware));
            workstation.Packages.Add(new VisualStudio(BucketNameSoftware));
            workstation.Packages.Add(new ReSharper());
            workstation.Packages.Add(new Chrome());
            workstation.Packages.Add(new MSysGit(BucketNameSoftware));
            workstation.Packages.Add(new TfsCrossPlatformCommandLineInterface());
            workstation.Packages.Add(new VisualStudioPowershellTools());


            //var waitConditionWorkstationAvailable = workstation.AddFinalizer("waitConditionWorkstationAvailable",TimeoutMax);


            return workstation;
        }

        private static Instance AddTfsServer(Template template,
            InstanceTypes instanceSize, 
            Subnet privateSubnet1, 
            LaunchConfiguration sqlServer4Tfs, 
            SecurityGroup tfsServerSecurityGroup,
            Greek version)
        {
            var tfsServer = new Instance(privateSubnet1,instanceSize,UsEastWindows2012R2Ami, OperatingSystem.Windows,Ebs.VolumeTypes.GeneralPurpose,
                                                    214);

            template.Resources.Add($"Tfs{version}", tfsServer);


            //dc1.Participate(tfsServer);
            if (sqlServer4Tfs != null)
            {
                tfsServer.AddDependsOn(sqlServer4Tfs.Packages.Last().WaitCondition);
                var packageTfsApplicationTier = new TeamFoundationServerApplicationTier(TeamFoundationServerEdition.Standard2015Update1, BucketNameSoftware, sqlServer4Tfs);
                tfsServer.Packages.Add(packageTfsApplicationTier);
            }
            tfsServer.SecurityGroupIds.Add(new ReferenceProperty(tfsServerSecurityGroup));

            return tfsServer;
        }

        public static Instance AddNat(Template template, Subnet subnet, SecurityGroup natSecurityGroup)
        {
            var nat = new Instance(null,InstanceTypes.T2Micro, "ami-e284b888", OperatingSystem.Linux)
            {
                SourceDestCheck = false
            };

            var natCount = template.Resources.Count(r => r.Key.StartsWith("Nat"));

            var natName = $"Nat{natCount + 1}";

            template.Resources.Add(natName, nat);


            var natNetworkInterface = new NetworkInterface(subnet)
            {
                AssociatePublicIpAddress = true,
                DeviceIndex = 0,
                DeleteOnTermination = true
            };

            natNetworkInterface.GroupSet.Add(natSecurityGroup);
            nat.NetworkInterfaces.Add(natNetworkInterface);
            return nat;
        }

        public enum Greek
        {
            None = 0,
            Alpha,
            Beta,
            Gamma,
            Delta,
            Epsilon,
            Zeta,
            Eta,
            Theta,
            Iota,
            Kappa,
            Lambda,
            Mu,
            Nu,
            Xi,
            Omicron,
            Pi,
            Rho,
            Sigma,
            Tau,
            Upsilon,
            Phi,
            Chi,
            Psi,
            Omega,
            TwentySix,
            TwentySeven,
            TwentyEight,
            TwentyNine,
            Thirty,
            ThirtyOne
        }

        [TestMethod]
        public void CreateDevelopmentTest()
        {
            Assert.IsFalse(HasGitDifferences());

            var stacks = Stack.Stack.GetActiveStacks();

            Greek version = Greek.None;

            foreach (var thisGreek in Enum.GetValues(typeof (Greek)))
            {
                if (
                    stacks.Any(
                        s =>
                            s.Name.ToLowerInvariant()
                                .StartsWith(thisGreek.ToString().ToLowerInvariant().Replace('.', '-'))))
                {
                    version = (Greek) thisGreek;
                }
            }

            version += 1;

            var topLevel = "yadayadasoftware.com";
            var appName = "dev";

            //Create instances = Create.Dc2 | Create.BackupServer | Create.Rdp1;
            //Create instances = Create.FullStack;
            Create instances = (Create)0;
            //var templateToCreateStack = GetTemplateFullStack(topLevel, appName, version, $"{version}-{appName}-{topLevel}".Replace('.', '-'), instances);
            var templateToCreateStack = GetTemplateFullStack(topLevel, appName, version, instances, GetGitSuffix());

            CreateTestStack(templateToCreateStack, this.TestContext);
        }


        [TestMethod]
        public void CreateDevelopmentTemplateFileTest()
        {
            var templateToCreateStack = GetTemplateFullStack("yadayadasoftware.com", "dev", Greek.Alpha, Create.FullStack, GetGitSuffix());
            TemplateEngine.CreateTemplateFile(templateToCreateStack);
        }

        [TestMethod]
        public void UpdateDevelopmentTest()
        {

            Greek version = Greek.Xi;

            var fullyQualifiedDomainName = $"{version}.dev.yadayadasoftware.com";
            Create instances = Create.FullStack;
            var template = GetTemplateFullStack("yadayadasoftware.com", "dev", version, instances, GetGitSuffix());
            ((ParameterBase)template.Parameters[Template.ParameterDomainAdminPassword]).Default = "dg68ug0K7U83MWQF";

            Assert.IsFalse(HasGitDifferences());

            Stack.Stack.UpdateStack(fullyQualifiedDomainName.Replace('.', '-'), template);
        }

        [TestMethod]
        public void UpdatePsiTest()
        {
            Assert.IsFalse(HasGitDifferences());

            Greek version = Greek.Psi;

            var fullyQualifiedDomainName = $"{version}.dev.yadayadasoftware.com";

            //colors &= ~Blah.BLUE;

            Create instances = Create.FullStack;

            var template = GetTemplateFullStack("yadayadasoftware.com", "dev", version,  instances, GetGitSuffix());
            ((ParameterBase)template.Parameters[Template.ParameterDomainAdminPassword]).Default = "IDJP5673lwip";
            Stack.Stack.UpdateStack(fullyQualifiedDomainName.Replace('.', '-'), template);
        }



        [TestMethod]
        public void AddingSameResourceTwiceFails()
        {
            var t = GetNewBlankTemplateWithVpc($"Vpc{this.TestContext.TestName}");
            var v = new Vpc("10.1.0.0/16");
            t.Resources.Add("X", v);

            var s = new Subnet(v,null,AvailabilityZone.UsEast1A, true);
            t.Resources.Add("Vpc1", s);


            ArgumentException expectedException = null;

            try
            {
                s = new Subnet(v, null, AvailabilityZone.UsEast1A, true);
                t.Resources.Add("Vpc1", s);

            }
            catch (ArgumentException e)
            {
                expectedException = e;
            }
            Assert.IsNotNull(expectedException);
        }

        [TestMethod]
        public void GetStacksTest()
        {
            List<Stack.Stack> stacks = Stack.Stack.GetActiveStacks();
            Assert.IsNotNull(stacks);
            Assert.IsTrue(stacks.Any());
            var create = stacks.Where(r => r.Name.Contains("Create"));
            Assert.IsTrue(create.Any());
        }

        internal static Template GetNewBlankTemplateWithVpc(string vpcName)
        {
            if (string.IsNullOrEmpty(vpcName))
            {
                throw new ArgumentNullException(nameof(vpcName));
            }
            return new Template($"Stack{vpcName}", KeyPairName, vpcName, "10.1.0.0/16");

        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion
    }

}
