using AWS.CloudFormation.Common;
using AWS.CloudFormation.Resource.AutoScaling;

namespace AWS.CloudFormation.Resource.EC2.Instancing.Metadata
{
    public class Metadata : CloudFormationDictionary
    {

        public Metadata(ResourceBase resource) : base(resource)
        {
        }

        public Init Init
        {
            get
            {
                if (this.ContainsKey("AWS::CloudFormation::Init"))
                {
                    return this["AWS::CloudFormation::Init"] as Init;
                }
                else
                {
                    return this.Add("AWS::CloudFormation::Init", new Init((LaunchConfiguration)this.ResourceRef)) as Init;
                }
            }

        }

        public CloudFormationDictionary Authentication
        {
            get
            {
                if (this.ContainsKey("AWS::CloudFormation::Authentication"))
                {
                    return this["AWS::CloudFormation::Authentication"] as CloudFormationDictionary;
                }
                else
                {
                    return this.Add("AWS::CloudFormation::Authentication", new CloudFormationDictionary()) as CloudFormationDictionary;
                }
            }
        }





    }
}
