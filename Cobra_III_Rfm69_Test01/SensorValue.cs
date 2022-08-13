using System;
using Microsoft.SPOT;

namespace Cobra_III_Rfm69_Test01
{
    public class SensorValue
    {
        
        public SensorValue(DateTime pLastNetmfTime, byte pChannel, byte pSensorID, UInt32 pSampleTime, UInt16 pTemp, double pTempDouble, UInt16 pHum, byte pRandomId, bool pBatteryIsLow)
        {
            
            this.LastNetmfTime = pLastNetmfTime;
            this.Channel = pChannel;
            this.SensorId = pSensorID;
            this.SampleTime = pSampleTime;
            this.Temp = pTemp;
            this.TempDouble = pTempDouble;
            this.Hum = pHum;
            this.RandomId = pRandomId;
            this.BatteryIsLow = pBatteryIsLow;
        }
        public DateTime LastNetmfTime { get; set; }
        public byte Channel { get; set; }
        public byte SensorId { get; set; }
        public UInt32 SampleTime { get; set; }
        public UInt16 Temp { get; set; }
        public double TempDouble { get; set; }
        public UInt16 Hum { get; set; }
        public byte RandomId { get; set; }
        public bool BatteryIsLow { get; set; }
    }
}
