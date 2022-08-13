using System;
using Microsoft.SPOT;

namespace Cobra_III_Rfm69_Test01
{
    public class SampleValue
    {
       
        public SampleValue(string pTableName, string pPartitionKey, DateTime pTimeOfSample, int pTimeOffsetUTC, double pSampleValue, double pDayMin, double pDayMax, double pT_0, byte pID_0, UInt16 pH_0, bool pLo_0, double pT_1, byte pID_1, UInt16 pH_1, bool pLo_1, double pT_2, byte pID_2, UInt16 pH_2, bool pLo_2, double pT_3, byte pID_3, UInt16 pH_3, bool pLo_3, double pT_4, byte pID_4, UInt16 pH_4, bool pLo_4, double pT_5, byte pID_5, UInt16 pH_5, bool pLo_5, double pT_6, byte pID_6, UInt16 pH_6, bool pLo_6, double pT_7, byte pID_7, UInt16 pH_7, bool pLo_7, string pSecondReport, string pStatus, string pLocation, TimeSpan ptimeFromLast, UInt16 pSendInfo, int pRSSI, int pIterations, uint pRemainingRam, int pforcedReboots, int pbadReboots, int pSendErrors, char pBootReason, bool pForceSend, string pMessage)
        {
            this.TableName = pTableName;
            this.PartitionKey = pPartitionKey;
            this.TimeOfSample = pTimeOfSample;
            this.TimeOffSetUTC = pTimeOffsetUTC;
            this.TheSampleValue = pSampleValue;
            this.DayMin = pDayMin;
            this.DayMax = pDayMax;
            this.T_0 = pT_0;
            this.T_1 = pT_1;
            this.T_2 = pT_2;
            this.T_3 = pT_3;
            this.T_4 = pT_4;
            this.T_5 = pT_5;
            this.T_6 = pT_6;
            this.T_7 = pT_7;
            this.ID_0 = pID_0;
            this.ID_1 = pID_1;
            this.ID_2 = pID_2;
            this.ID_3 = pID_3;
            this.ID_4 = pID_4;
            this.ID_5 = pID_5;
            this.ID_6 = pID_6;
            this.ID_7 = pID_7;
            this.H_0 = pH_0;
            this.H_1 = pH_1;
            this.H_2 = pH_2;
            this.H_3 = pH_3;
            this.H_4 = pH_4;
            this.H_5 = pH_5;
            this.H_6 = pH_6;
            this.H_7 = pH_7;

            this.Lo_0 = pLo_0;
            this.Lo_1 = pLo_1;
            this.Lo_2 = pLo_2;
            this.Lo_3 = pLo_3;
            this.Lo_4 = pLo_4;
            this.Lo_5 = pLo_5;
            this.Lo_6 = pLo_6;
            this.Lo_7 = pLo_7;

            this.SecondReport = pSecondReport;
            this.Status = pStatus;
            this.Location = pLocation;
            this.TimeFromLast = ptimeFromLast;
            this.SendInfo = pSendInfo;
            this.RSSI = pRSSI;
            this.Iterations = pIterations;
            this.RemainingRam = pRemainingRam;
            this.ForcedReboots = pforcedReboots;
            this.BadReboots = pbadReboots;
            this.SendErrors = pSendErrors;
            this.BootReason = pBootReason;
            this.ForceSend = pForceSend;
            this.Message = pMessage;
            
        }

        public string TableName { get; set; }
        public string PartitionKey {get; set;}
        public DateTime TimeOfSample {get; set;}
        public int TimeOffSetUTC { get; set; }
        public double TheSampleValue  { get; set; }
        public double DayMin { get; set; }
        public double DayMax { get; set; }
        public double T_0 { get; set; }
        public double T_1 { get; set; }
        public double T_2 { get; set; }
        public double T_3 { get; set; }
        public double T_4 { get; set; }
        public double T_5 { get; set; }
        public double T_6 { get; set; }
        public double T_7 { get; set; }
        public byte ID_0 { get; set; }
        public byte ID_1 { get; set; }
        public byte ID_2 { get; set; }
        public byte ID_3 { get; set; }
        public byte ID_4 { get; set; }
        public byte ID_5 { get; set; }
        public byte ID_6 { get; set; }
        public byte ID_7 { get; set; }
        public UInt16 H_0 { get; set; }
        public UInt16 H_1 { get; set; }
        public UInt16 H_2 { get; set; }
        public UInt16 H_3 { get; set; }
        public UInt16 H_4 { get; set; }
        public UInt16 H_5 { get; set; }
        public UInt16 H_6 { get; set; }
        public UInt16 H_7 { get; set; }
        public bool Lo_0 { get; set; }
        public bool Lo_1 { get; set; }
        public bool Lo_2 { get; set; }
        public bool Lo_3 { get; set; }
        public bool Lo_4 { get; set; }
        public bool Lo_5 { get; set; }
        public bool Lo_6 { get; set; }
        public bool Lo_7 { get; set; }
        public string SecondReport { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public TimeSpan TimeFromLast { get; set; }
        public UInt16 SendInfo { get; set; }
        public int RSSI { get; set; }
        public int Iterations { get; set; }
        public uint RemainingRam { get; set; }
        public int ForcedReboots { get; set; }
        public int BadReboots { get; set; }
        public int SendErrors { get; set; }
        public char BootReason { get; set; }
        public bool ForceSend { get; set; }
        public string Message { get; set; }
    }
}
