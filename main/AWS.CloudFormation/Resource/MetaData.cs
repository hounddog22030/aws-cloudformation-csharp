using AWS.CloudFormation.Common;
using AWS.CloudFormation.Instance.MetaData;

namespace AWS.CloudFormation.Resource
{
    public class Metadata : CloudFormationDictionary
    {

        public Metadata(Instance.Instance instance) : base(instance)
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
                    return this.Add("AWS::CloudFormation::Init", new Init(this.Instance)) as Init;
                }
            }

        }



    }
}
