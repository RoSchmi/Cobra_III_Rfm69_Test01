using System;
using System.Collections;
using Microsoft.SPOT;
using RoSchmi.Net.Azure.Storage;
using PervasiveDigital.Utilities;

namespace RoSchmi.Net.Azure.Storage
{
    public class CloudStorageAccount
    {
        public string AccountName { get; set; }
        public string AccountKey { get; set; }
        public Hashtable UriEndpoints { get; set; }

        public CloudStorageAccount(string accountName, string accountKey, bool useHttps, Hashtable uriEndpoints)
        {
            AccountName = accountName;
            AccountKey = accountKey;
            UriEndpoints = uriEndpoints;
            //Debug.Print("New Cloudstorageaccount created");
            
            //checkUriEndpoints
            //must be 3 known keys (Blob, Queue, Table) 
            //must not end with a trailing slash
        }

        public CloudStorageAccount(string accountName, string accountKey, bool useHttps) : this (accountName, accountKey, useHttps, GetDefaultUriEndpoints(accountName, useHttps))
        {
        }

        private static Hashtable GetDefaultUriEndpoints(string accountName, bool useHttps)
        {
            string insert = useHttps ? "s" : "";
            var defaults = new Hashtable(3);
            defaults.Add("Blob", StringUtilities.Format("http{0}://{1}.blob.core.windows.net", insert, accountName));
            defaults.Add("Queue", StringUtilities.Format("http{0}://{1}.queue.core.windows.net", insert, accountName));
            defaults.Add("Table", StringUtilities.Format("http{0}://{1}.table.core.windows.net", insert, accountName));
            return defaults;
        }

        public static CloudStorageAccount Parse(string connectionString)
        {
            throw new NotImplementedException();
        }
    }
}
