using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace AWS.CloudFormation.Common
{
    public static class SettingsHelper
    {
        public static string GetSetting(string key)
        {
            var appSettingsReader = new AppSettingsReader();
            return (string)appSettingsReader.GetValue(key, typeof(string));
        }
    }
}
