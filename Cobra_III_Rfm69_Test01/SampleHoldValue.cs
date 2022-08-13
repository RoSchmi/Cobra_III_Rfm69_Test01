using System;
using Microsoft.SPOT;

namespace Cobra_III_Rfm69_Test01
{
    class SampleHoldValue
    {
        public SampleHoldValue(ushort pTemp, ushort pHumid)
        {
            this.Temp = pTemp;
            this.Humid = pHumid;
        }
        public ushort Temp {get; set;}
        public ushort Humid { get; set; }
        
    }
}
