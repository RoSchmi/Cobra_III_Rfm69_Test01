using System;
using Microsoft.SPOT;
using System.Collections;
using PervasiveDigital.Json;

using RoSchmi.Net.Azure.Storage;

namespace Cobra_III_Rfm69_Test01
{
    public class TempEntity : TableEntity
    {
        public string actTemperature { get; set; }
        public string location { get; set; }

        // Your entity type must expose a parameter-less constructor
        public TempEntity() { }

        // Define the PK and RK
        public TempEntity(string partitionKey, string rowKey, ArrayList pProperties)
            : base(partitionKey, rowKey)
        {
            //this.TimeStamp = DateTime.Now;
            this.Properties = pProperties;    // store the ArrayList

            var myProperties = new PropertyClass()
            {
                PartitionKey = partitionKey,
                RowKey = rowKey              
            };

            this.JsonString = JsonConverter.Serialize(myProperties).ToString();
        }
        private class PropertyClass
        {
            public string RowKey;
            public string PartitionKey;          
        }

    }
}



