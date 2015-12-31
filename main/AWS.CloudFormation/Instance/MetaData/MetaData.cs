using AWS.CloudFormation.Common;

namespace AWS.CloudFormation.Instance.MetaData
{
    public class MetaData : CloudFormationDictionary
    {

        public MetaData(Instance instance) : base(instance)
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
