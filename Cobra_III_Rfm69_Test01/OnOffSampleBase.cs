using System;
using Microsoft.SPOT;

namespace Cobra_III_Rfm69_Test01
{
    public class OnOffSampleBase
    {
        public string PartitionKey { get; set;}        
        public DateTime TimeOfSample { get; set;}
        public int TimeOffsetUTC { get; set; }
        public string Location { get; set;}        
        public TimeSpan TimeFromLast { get; set;}

        public int Iterations { get; set;}        
        public uint RemainingRam { get; set;}        
        public int ForcedReboots { get; set;}        
        public int BadReboots { get; set;}        
        public int SendErrors { get; set;}        
        public char BootReason { get; set;}       
        public bool ForceSend { get; set;}        
        public string Message { get; set;}      
    }
}
