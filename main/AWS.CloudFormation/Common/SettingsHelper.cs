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

        public static string GetLocalGateway()
        {
            string url = "http://checkip.dyndns.org";
            System.Net.WebRequest req = System.Net.WebRequest.Create(url);
            System.Net.WebResponse resp = req.GetResponse();
            System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
            string response = sr.ReadToEnd().Trim();
            string[] a = response.Split(':');
            string a2 = a[1].Substring(1);
            string[] a3 = a2.Split('<');
            return a3[0];
        }
    }
}
