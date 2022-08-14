using System;
using Microsoft.SPOT;
using System.Collections;
using PervasiveDigital.Json;
using RoSchmi.Net.Azure.Storage;

namespace Cobra_III_Rfm69_Test01
{
    class ParamEntity : TableEntity
    {
         public string actTemperature { get; set; }
        public string location { get; set; }

        // Your entity type must expose a parameter-less constructor
        public ParamEntity() { }

        // Define the PK and RK
        public ParamEntity(string partitionKey, string rowKey, ArrayList pProperties)
            : base(partitionKey, rowKey)
        {
            //this.TimeStamp = DateTime.Now;
            this.Properties = pProperties;    // store the ArrayList

            var myProperties = new PropertyClass()
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                // get the values out of the ArrayList
                AlertLevelLow = ((string[])this.Properties[0])[2],        // Row 0, arrayfield 2
                AlertLevelHigh = ((string[])this.Properties[1])[2],            // Row 1, arrayfield 2
                AlertHysteresis = ((string[])this.Properties[2])[2],            // Row 2, arrayfield 2   
                SwitchLevelLow = ((string[])this.Properties[3])[2],
                SwitchHysteresis = ((string[])this.Properties[4])[2],
                Email_01_Sender = ((string[])this.Properties[5])[2],
                Email_01_Recipient = ((string[])this.Properties[6])[2],
                Email_01_Name = ((string[])this.Properties[7])[2],
                Email_02_Sender = ((string[])this.Properties[8])[2],
                Email_02_Recipient = ((string[])this.Properties[9])[2],
                Email_02_Name = ((string[])this.Properties[10])[2],
            };

            this.JsonString = JsonConverter.Serialize(myProperties).ToString();
        }
        private class PropertyClass
        {
            public string RowKey;
            public string PartitionKey;
            public string AlertLevelLow;
            public string AlertLevelHigh;
            public string AlertHysteresis;
            public string SwitchLevelLow;
            public string SwitchHysteresis;
            public string Email_01_Sender;
            public string Email_01_Recipient;
            public string Email_01_Name;
            public string Email_02_Sender;
            public string Email_02_Recipient;
            public string Email_02_Name;
        }

    }
}
