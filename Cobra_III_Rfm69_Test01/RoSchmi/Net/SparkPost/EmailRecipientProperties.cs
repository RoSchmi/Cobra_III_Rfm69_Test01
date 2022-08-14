using System;
using Microsoft.SPOT;

namespace RoSchmi.Net.SparkPost
{
    public class EmailRecipientProperties

    {
        public string returnEmailAddress {get; set;}
        public string recipientEmailAddress {get; set;}
        public string recipientName { get; set; }

        public EmailRecipientProperties(string returnEmailAddress, string recipientEmailAddress, string recipientName)
        {
            this.returnEmailAddress = returnEmailAddress; 
            this.recipientEmailAddress = recipientEmailAddress;
            this.recipientName = recipientName;
        }
    }
}
