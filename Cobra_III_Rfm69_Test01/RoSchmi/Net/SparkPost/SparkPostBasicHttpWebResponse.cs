// Copyright RoSchmi 2016, License Apache 2.0
// Version 1.0 09.09.2016
// NETMF 4.3, GHI SDK 2016 R1 
// Struct used to return values of a System.Net.HttpWebResponse to control the SparkPost Email Client

using System;
using Microsoft.SPOT;
using System.Net;

namespace RoSchmi.Net.SparkPost
{
   public struct SparkPostBasicHttpWebResponse

    {
        public bool ResponseIsValid { get; set; }
        public string Date { get; set; }
        public string Body { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
