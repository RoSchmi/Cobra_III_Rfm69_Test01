using System;
using Microsoft.SPOT;
using System.Collections;
using PervasiveDigital.Utilities;

namespace RoSchmi.Net.SparkPost
{
    public class SparkPostAccount
    {
        public string AccountName { get; private set; }
        public string AccountUser { get; private set; }
        public string AccountPassword { get; private set; }
        public bool AccountUseHttps { get; private set; }

        public Hashtable UriEndpoints { get; private set; }

        public SparkPostAccount(string accountName, string accountUser, string accountPassword, bool useHttps, Hashtable uriEndpoints)
        {
            AccountName = accountName;
            AccountUser = accountUser;
            AccountPassword = accountPassword;
            AccountUseHttps = useHttps;
            UriEndpoints = uriEndpoints;
            //Debug.Print("New Cloudstorageaccount created");
        }

        public SparkPostAccount(string accountName, string accountUser, string accountPassword, bool useHttps)
            : this(accountName, accountUser, accountPassword, useHttps, GetDefaultUriEndpoints(accountName, useHttps))
        {
        }

        private static Hashtable GetDefaultUriEndpoints(string accountName, bool useHttps)
        {
            string insert = useHttps ? "s" : "";
            var defaults = new Hashtable(3);
            defaults.Add("Default", StringUtilities.Format("http{0}://{1}", insert, accountName));
            return defaults;
        }

        public static SparkPostAccount Parse(string connectionString)
        {
            throw new NotImplementedException();
        }
    }
}
