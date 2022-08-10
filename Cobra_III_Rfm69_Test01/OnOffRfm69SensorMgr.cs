#define DebugPrint

using System;
using Microsoft.SPOT;
using GHI.Pins;
using System.Text;
using RoSchmi.RFM69_NETMF;


//namespace HeatingSurvey
namespace  Cobra_III_Rfm69_Test01
{
    class OnOffRfm69SensorMgr : OnOffBaseSensorMgr
    {
        #region Settings concerning Rfm69 receiver
        
        //private const string ENCRYPTKEY = "sampleEncryptKey"; //exactly the same 16 characters/bytes on all nodes!
        private const string ENCRYPTKEY = "heatingsurvey234"; //exactly the same 16 characters/bytes on all nodes!
       
        private const bool IS_RFM69HCW = true; // set to 'true' if you are using an RFM69HCW module

        //Match frequency to the hardware version of the RFM69 radio
        //private static RFM69_NETMF.Frequency FREQUENCY = RFM69_NETMF.Frequency.RF69_868MHZ;
        private static RFM69_NETMF.Frequency FREQUENCY = RFM69_NETMF.Frequency.RF69_433MHZ;
        // RoSchmi
        private static byte NETWORKID = 100;  // The same on all nodes that talk to each other
        //private static byte NETWORKID = 101;  // The same on all nodes that talk to each other
        // private static byte NODEID = 3;       // The ID of this node (the node to make tests)
        private static byte NODEID = 1;          // The ID of this node (the node in the heating room)

        private const short LowBorderRSSI = -65;       // In automatic transmission control sending power is reduced to this border
        
        private const byte PowerLevel = 28;            // Sending-power starts with this Power Level (0 - 31)
        private const byte MaxPowerLevel = 28;         // Sending-power does not exceed this Power Level

        private const UInt16 firstAfterBootMagicNumber = 999;

        private static int Rfm69EventsCounter = 0;

        #endregion

        int lastPacketNum = -1;
        int lastPacketNum_3 = -1;
        int actPacketNum = 0;

        string DestinationTableContinuous;
        string MeasuredQuantityContinuous;

        public static RFM69_NETMF radio;


        private static readonly object Rfm69SensorLock = new object();

        public OnOffRfm69SensorMgr(GHI.Processor.DeviceType deviceType, int socketNumber, int pDstOffset, string pDstStart, string pDstEnd, string pSensorLabel = "undef", string pSensorLocation = "undef", string pMeasuredQuantity = "undef", string pMeasuredQuantityContinuous = "undef", string pDestinationTableOnOff = "undef", string pDestinationTableContinuous = "undef", string pChannel = "000")
        {
            // RoSchmi
            if ((deviceType != GHI.Processor.DeviceType.EMX) && (deviceType != GHI.Processor.DeviceType.G120E) && (deviceType != GHI.Processor.DeviceType.G120))
            {
                throw new NotSupportedException("Mainboard is not supported");
            }

            dstOffset = pDstOffset;
            dstStart = pDstStart;
            dstEnd = pDstEnd;

            SensorLabel = pSensorLabel;
            SensorLocation = pSensorLocation;
            MeasuredQuantity = pMeasuredQuantity;
            MeasuredQuantityContinuous = pMeasuredQuantityContinuous;
            DestinationTable = pDestinationTableOnOff;
            DestinationTableContinuous = pDestinationTableContinuous;
            Channel = pChannel;

            _stopped = true;

            switch (deviceType)
            {
                // For e.g. Copra III
                case GHI.Processor.DeviceType.G120:
                    {
                        radio = new RFM69_NETMF(Microsoft.SPOT.Hardware.SPI.SPI_module.SPI1, G120.P1_9, G120.P0_1, GHI.Pins.G120.P1_8, true, pSensorLabel, pSensorLocation, pMeasuredQuantity, pDestinationTableOnOff);
                        break;
                    }


                case GHI.Processor.DeviceType.EMX:
                case GHI.Processor.DeviceType.G120E:
                    {
                        //For Spider on Gadgeteer Socket 6
                        if (socketNumber == 6)
                        {
                            radio = new RFM69_NETMF(Microsoft.SPOT.Hardware.SPI.SPI_module.SPI2, GHI.Pins.EMX.IO10, EMX.IO18, EMX.IO20, true, pSensorLabel, pSensorLocation, pMeasuredQuantity, pDestinationTableOnOff);
                        }
                        else
                        {
                            throw new NotSupportedException("The selcted socket is not allowed");
                        }

                        break;
                    }


            }
            /*
            //For Spider on Gadgeteer Socket 6
            if (socketNumber == 6)
            {
                radio = new RFM69_NETMF(Microsoft.SPOT.Hardware.SPI.SPI_module.SPI2, GHI.Pins.EMX.IO10, EMX.IO18, EMX.IO20, true, pSensorLabel, pSensorLocation, pMeasuredQuantity, pDestinationTableOnOff);
            }
            else
            {
                throw new NotSupportedException("The selcted socket is not allowed");
            }
            */

            radio.ACKReturned += radio_ACKReturned;
            radio.MessageReceived += radio_MessageReceived;          
            radio.ReceiveLoopIteration += radio_ReceiveLoopIteration;
            radio.hardReset();
            if (!radio.initialize(FREQUENCY, NODEID, NETWORKID))   // NODEID = 1, NETWORKID = 100
            {
                Debug.Print("Error: Rfm69 Initialization failed");
            }
            else
            {
                Debug.Print("Rfm69 Initialization finished");
            }
            if (IS_RFM69HCW)
            {
                radio.setHighPower(true);
            }
            radio.setPowerLevel(PowerLevel, MaxPowerLevel); // power output ranges from 0 (5dBm) to 31 (20dBm), will not exceed MaxPowerLevel

            radio.encrypt(ENCRYPTKEY);
            //radio.encrypt(null);

            //leave the following line away if autoPower is not wanted
            //radio.enableAutoPower(LowBorderRSSI);     // enable automatic transmission control, transmission power is stepwise reduced to the RSSI level passed as parameter
 
            Debug.Print("\nListening at "
                + (FREQUENCY == RFM69_NETMF.Frequency.RF69_433MHZ ? "433" : FREQUENCY == RFM69_NETMF.Frequency.RF69_868MHZ ? "868" : "915")
                + " MHz");          
        }

        public void Start()
        {         
            _stopped = false;
        }

        public void Stop()
        {
            _stopped = true;
        }

        void radio_ReceiveLoopIteration(RFM69_NETMF sender, RFM69_NETMF.ReceiveLoopEventArgs e)
        {
            /*
           if (e.EventID == 1)
           {
               //multicolorLED.BlinkOnce(GT.Color.Green);
           }
           if (e.Iteration % 5 == 0)  // toggle every fifth iteration
           {
               myButton.ToggleLED();
           }
           */
        }

        void radio_MessageReceived(RFM69_NETMF sender, RFM69_NETMF.MessageReceivedEventArgs e)
        {
            
#if DebugPrint
            Debug.Print("Rfm69 received: " + new string(Encoding.UTF8.GetChars(e.receivedData)));
#endif
            if (!_stopped)
            {
                // RoSchmi
                Rfm69EventsCounter++;
                if (Rfm69EventsCounter > 1)
                {
                    Debug.Print("More than one Rfm69 event processed at a time");
                    //throw new NotSupportedException("More than one Rfm69 event processed at a time");
                }

                actPacketNum = int.Parse(new string(Encoding.UTF8.GetChars(e.receivedData, 0, 3)));
                UInt16 sendInfo = UInt16.Parse(new string(Encoding.UTF8.GetChars(e.receivedData, 8, 4)));
                UInt16 repeatSend = sendInfo;
                
                

                
                if ((sendInfo / 10) == firstAfterBootMagicNumber)     // 999 = magic number for first send after boot of sending device
                {
                    repeatSend = (UInt16)(9000 + sendInfo % 10);  // sets the first digit of info to 9 to signal first send after boot 
                }



                Char cmdChar = Encoding.UTF8.GetChars(e.receivedData, 4, 1)[0];
                int selectedLastPacketNum = (cmdChar == '3') ? lastPacketNum_3 : lastPacketNum;
                if (actPacketNum != selectedLastPacketNum)
                {
                    if (cmdChar == '3')
                    {
                        lastPacketNum_3 = actPacketNum;
                    }
                    else
                    {
                        lastPacketNum = actPacketNum;
                    }
                    
                    Char oldChar = Encoding.UTF8.GetChars(e.receivedData, 6, 1)[0];

                    

                    UInt32 current = (UInt32)((UInt32)e.receivedData[16] | (UInt32)e.receivedData[15] << 8 | (UInt32)e.receivedData[14] << 16 | (UInt32)e.receivedData[13] << 24);

                    UInt32 power = (UInt32)((UInt32)e.receivedData[21] | (UInt32)e.receivedData[20] << 8 | (UInt32)e.receivedData[19] << 16 | (UInt32)e.receivedData[18] << 24);

                    UInt32 work = (UInt32)((UInt32)e.receivedData[26] | (UInt32)e.receivedData[25] << 8 | (UInt32)e.receivedData[24] << 16 | (UInt32)e.receivedData[23] << 24);
                  
                                     
                    if (cmdChar == '0' || cmdChar == '1')      // Comes from Pump on/off sensor
                    {
                        actState = cmdChar == '1' ? InputSensorState.High : InputSensorState.Low;
                        oldState = oldChar == '1' ? InputSensorState.High : InputSensorState.Low;
                       
                        OnRfm69OnOffSensorSend(this, new OnOffSensorEventArgs(actState, oldState, repeatSend, DateTime.Now.AddMinutes(RoSchmi.DayLihtSavingTime.DayLihtSavingTime.DayLightTimeOffset(dstStart, dstEnd, dstOffset, DateTime.Now, true)), SensorLabel, SensorLocation, MeasuredQuantity, DestinationTable, Channel, false, current, power, work));
                    }
                    if (cmdChar == '2')                          // Comes from current sensor    
                    {
                        
                        OnRfm69DataSensorSend(this, new DataSensorEventArgs(DateTime.Now.AddMinutes(RoSchmi.DayLihtSavingTime.          DayLihtSavingTime.DayLightTimeOffset(dstStart, dstEnd, dstOffset, DateTime.Now, true)), repeatSend, current, power, work, SensorLabel, SensorLocation, MeasuredQuantityContinuous, DestinationTableContinuous, Channel, false));
                    }
                    if (cmdChar == '3')                          // Comes from solar temperature sensor (Collector, Storage, Water)
                    {
                        OnRfm69SolarTempsDataSensorSend(this, new DataSensorEventArgs(DateTime.Now.AddMinutes(RoSchmi.DayLihtSavingTime.DayLihtSavingTime.DayLightTimeOffset(dstStart, dstEnd, dstOffset, DateTime.Now, true)), repeatSend, current, power, work, SensorLabel, SensorLocation, MeasuredQuantityContinuous, "EscapeTableLocation_03", Channel, false));
                        // Destination Table is changed to "EscapeTableLocation_03" (Magic String)
                       
                    }
                }

                Rfm69EventsCounter--;
            }      // End if not stopped
        }

        void radio_ACKReturned(RFM69_NETMF sender, RFM69_NETMF.ACK_EventArgs e)
        {
            if (e.ACK_Received)
            {
                try
                {
                    // multicolorLED.BlinkOnce(GT.Color.Cyan);
#if DebugPrint
                        Debug.Print("ACK received from Node: " + e.senderOfTheACK + "  ~ms " + (DateTime.Now - e.sendTime).Milliseconds + " after asyncSendWithRetry");
#endif
                }
                catch { };
            }
            else
            {
                try
                {
#if DebugPrint
                        Debug.Print("No ACK was returned for message: " + new string(Encoding.UTF8.GetChars(e.sentData)));
#endif
                }
                catch { };
            }
        }

        #region DataSensorEventArgs
        /// <summary>
        /// Event arguments for the AzureSend event
        /// </summary>
        public class DataSensorEventArgs : EventArgs
        {
            
            /// <summary>
            /// Timestamp
            /// </summary>
            /// 
            public DateTime Timestamp
            { get; private set; }


            /// <summary>
            /// Val_1
            /// </summary>
            /// 
            public UInt32 Val_1
            { get; private set; }

            /// <summary>
            /// Val_2
            /// </summary>
            /// 
            public UInt32 Val_2
            { get; private set; }


            /// <summary>
            /// Val_3
            /// </summary>
            /// 
            public UInt32 Val_3
            { get; private set; }

            /// <summary>
            /// RepaeatCount to succeed
            /// </summary>
            /// 
            public UInt16 RepeatSend
            { get; private set; }


            /// <summary>
            /// SensorLabel
            /// </summary>
            /// 
            public string SensorLabel
            { get; private set; }

            /// <summary>
            /// SensorLocation
            /// </summary>
            /// 
            public string SensorLocation
            { get; private set; }

            /// <summary>
            /// MeasuredQuantity
            /// </summary>
            /// 
            public string MeasuredQuantity
            { get; private set; }

            /// <summary>
            /// DestinationTable
            /// </summary>
            /// 
            public string DestinationTable
            { get; private set; }

            /// <summary>
            /// Channel
            /// </summary>
            /// 
            public string Channel
            { get; private set; }

            /// <summary>
            /// LastOfDay
            /// </summary>
            /// 
            public bool LastOfDay
            { get; private set; }

            /// <summary>
            /// The RSSI (Signal Strength)
            /// </summary>
            /// 
            public int RSSI
            { get; private set; }

            internal DataSensorEventArgs(DateTime pTimeStamp, UInt16 pRepeatSend, UInt32 pVal_1, UInt32 pVal_2, UInt32 pVal3, string pSensorLabel, string pSensorLocation, string pMeasuredQuantity, string pDestinationTable, string pChannel, bool pLastOfDay)
            {                
                this.Timestamp = pTimeStamp;
                this.Val_1 = pVal_1;
                this.Val_2 = pVal_2;
                this.Val_3 = pVal3;
                this.RepeatSend = pRepeatSend;
                this.DestinationTable = pDestinationTable;
                this.MeasuredQuantity = pMeasuredQuantity;
                this.SensorLabel = pSensorLabel;
                this.SensorLocation = pSensorLocation;
                this.Channel = pChannel;
                this.LastOfDay = pLastOfDay;
                this.RSSI = 0;
            }
        }
        #endregion


        #region Delegates


        /// <summary>
        /// The delegate that is used to handle the data message.
        /// </summary>
        /// <param name="sender">The <see cref="Rfm69"/> object that raised the event.</param>
        /// <param name="e">The event arguments.</param>     
        public delegate void rfm69SolarTempsDataSensorEventhandler(OnOffRfm69SensorMgr sender, DataSensorEventArgs e);

        /// <summary>
        /// Raised when a message with Current data is received
        /// </summary>
        public event rfm69SolarTempsDataSensorEventhandler rfm69SolarTempsDataSensorSend;

        private rfm69SolarTempsDataSensorEventhandler onRfm69SolarTempsDataSensorSend;

        private void OnRfm69SolarTempsDataSensorSend(OnOffRfm69SensorMgr sender, DataSensorEventArgs e)
        {
            if (this.onRfm69SolarTempsDataSensorSend == null)
            {
                this.onRfm69SolarTempsDataSensorSend = this.OnRfm69SolarTempsDataSensorSend;
            }
            this.rfm69SolarTempsDataSensorSend(sender, e);
        }




        /// <summary>
        /// The delegate that is used to handle the data message.
        /// </summary>
        /// <param name="sender">The <see cref="Rfm69"/> object that raised the event.</param>
        /// <param name="e">The event arguments.</param>     
        public delegate void rfm69DataSensorEventhandler(OnOffRfm69SensorMgr sender, DataSensorEventArgs e);

        /// <summary>
        /// Raised when a message with Current data is received
        /// </summary>
        public event rfm69DataSensorEventhandler rfm69DataSensorSend;

        private rfm69DataSensorEventhandler onRfm69DataSensorSend;

        private void OnRfm69DataSensorSend(OnOffRfm69SensorMgr sender, DataSensorEventArgs e)
        {
            if (this.onRfm69DataSensorSend == null)
            {
                this.onRfm69DataSensorSend = this.OnRfm69DataSensorSend;
            }
            this.rfm69DataSensorSend(sender, e);
        }




        /// <summary>
        /// The delegate that is used to handle the data message.
        /// </summary>
        /// <param name="sender">The <see cref="Rfm69"/> object that raised the event.</param>
        /// <param name="e">The event arguments.</param>     
        public delegate void rfm69OnOffSensorEventhandler(OnOffRfm69SensorMgr sender, OnOffSensorEventArgs e);

        /// <summary>
        /// Raised when a message from the PRU is received
        /// </summary>
        public event rfm69OnOffSensorEventhandler rfm69OnOffSensorSend;
        
        private rfm69OnOffSensorEventhandler onRfm69OnOffSensorSend;
        
        private void OnRfm69OnOffSensorSend(OnOffRfm69SensorMgr sender, OnOffSensorEventArgs e)
        {
            if (this.onRfm69OnOffSensorSend == null)
            {
                this.onRfm69OnOffSensorSend = this.OnRfm69OnOffSensorSend;
            }
            this.rfm69OnOffSensorSend(sender, e);
        }
        #endregion

    }
}
