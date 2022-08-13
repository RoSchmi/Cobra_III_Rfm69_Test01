using System;
using Microsoft.SPOT;

namespace Cobra_III_Rfm69_Test01
{
    public struct Counters
    {       
            public int AzureSends { get; set; }
            public int ForcedReboots { get; set; }
            public int BadReboots { get; set; }
            public int AzureSendErrors { get; set; }
            

            // Initialization with this constructor did not work ( Compiler error? )
            /*
            public Counters(int pAzureSends, int pForcedReboots, int pBadReboots, int pAzureSendErrors)
            {
                AzureSends = pAzureSends;
                ForcedReboots = pForcedReboots;
                BadReboots = pBadReboots;
                AzureSendErrors = pAzureSendErrors;               
            }
            */
    }
}
