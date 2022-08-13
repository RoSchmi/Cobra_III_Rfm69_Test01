using System;
using Microsoft.SPOT;

namespace Cobra_III_Rfm69_Test01
{
    public class OnOffSample : OnOffSampleBase
    {
        private OnOffSample() { }       // Parameterless constructor, not really needed here (only needed when used in EntityFramework)

        public OnOffSample(string pPartitionKey, DateTime pTimeOfSample, int pTimeOffsetUTC, string pStatus, string pLastStatus, string pLocation, TimeSpan ptimeFromLast, TimeSpan pOnTimeDay, int pCD, TimeSpan pOnTimeWeek, int pCW,TimeSpan pOnTimeMonth, int pCM, TimeSpan pOnTimeYear, int pCY,  int pIterations, uint pRemainingRam, int pforcedReboots, int pbadReboots, int pSendErrors, char pBootReason, bool pForceSend, string pMessage, UInt16 pSendInfo = 0, string pVal_1 = "", string pVal_2 = "", string pVal_3 = "")
        {
            PartitionKey = pPartitionKey;
            TimeOfSample = pTimeOfSample;
            TimeOffsetUTC = pTimeOffsetUTC;
            Status = pStatus;
            LastStatus = pLastStatus;
            Location = pLocation;
            TimeFromLast = ptimeFromLast;
            OnTimeDay = pOnTimeDay;
            CD = pCD;
            OnTimeWeek = pOnTimeWeek;
            CW = pCW;
            OnTimeMonth = pOnTimeMonth;
            CM = pCM;
            OnTimeYear = pOnTimeYear;
            CY = pCY;
            Iterations = pIterations;
            RemainingRam = pRemainingRam;
            ForcedReboots = pforcedReboots;
            BadReboots = pbadReboots;
            SendErrors = pSendErrors;
            BootReason = pBootReason;
            ForceSend = pForceSend;
            Message = pMessage;
            SendInfo = pSendInfo;
            Val_1 = pVal_1;
            Val_2 = pVal_2;
            Val_3 = pVal_3;
        }
         
        public string LastStatus { get; set;}       
        public string Status { get; set;}                
        public TimeSpan OnTimeDay { get; set;}        
        public int CD { get; set;}        
        public TimeSpan OnTimeWeek { get; set;}        
        public int CW { get; set;}        
        public TimeSpan OnTimeMonth { get; set;}        
        public int CM { get; set;}        
        public TimeSpan OnTimeYear { get; set;}        
        public int CY { get; set;}
        public UInt16 SendInfo { get; set; }
        public string Val_1 { get; set; }
        public string Val_2 { get; set; }
        public string Val_3 { get; set; }
    }
}
