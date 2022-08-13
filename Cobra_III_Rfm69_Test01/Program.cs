// Cobra_III_Rfm69_Test01 Program Copyright RoSchmi 2022 License Apache 2.0,  Version 1.1.0 vom 13.08.2022, 
// NETMF 4.3, GHI SDK 2016 R1
// Hardware: GHI Cobra III Mainboard, Enc28 Ethernet module 
// Dieses Programm dient zur Registrierung der gemessenen Stromwerte eines Smartmeters
// sowie der Messung von Temperaturen und der relativen Luftfeutigkeit




//#define DebugPrint


#region Region Using directives
using System;
using System.Threading;
using System.Net;
//using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Ports;
using System.Security.Cryptography.X509Certificates;
//using System.Collections;
using System.Xml;
using System.Ext.Xml;
using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;
using GHI.Networking;
using GHI.Processor;
using GHI.Pins;
using GHI.IO.Storage;
using Microsoft.SPOT.Time;
using RoSchmi.DayLihtSavingTime;
using RoSchmi.RFM69_NETMF;
using RoSchmi.Utilities;
//using RoSchmi.RF_433_Receiver;

// RoSchmi.RS232;
//using RoSchmi.ButtonNETMF;
//using RoSchmi.Net.ALLNET;
using RoSchmi.Net.Azure.Storage;
//using RoSchmi.Net.SparkPost;
using RoSchmi.Net.Divers;
//using RoSchmi.Logger
//using SparkPostEmailTemplates;
//using Osre.Modbus;
//using Osre.Modbus.Interface;
using PervasiveDigital;
using PervasiveDigital.Utilities;

#endregion



namespace Cobra_III_Rfm69_Test01
{
    public class Program
    {
        #region Settings that have to be set by the user
            //************ Settings: These parameters have to be set by the user  *******************

            #region Common Settings (time offset to GMT, Interval of Azure Sends, Debugging parameters and things concerning fiddler attachment
            // Common Settings

            // Select Mainboard (here FEZ Spider)
            private const GHI.Processor.DeviceType _deviceType = GHI.Processor.DeviceType.G120;

            // Select if Static IP or DHCP is used (can be overwritten by parameters in an XML-File on the SD-Card (file Temp_Survey.xml)
                                                     // When there is no such file on the SD-Card it is created with the parameters in the program
            private static bool useDHCP = true;
        
            // Static Network IP-Addresses
            private static string DeviceIpAddress = "192.168.1.66";
            //private static string GateWayIpAddress = "192.168.1.1";
            private static string GateWayIpAddress = "192.168.1.65";
            private static string SubnetMask = "255.255.255.0";
            //private static string DnsServerIpAddress = "192.168.1.1";
            private static string DnsServerIpAddress = "192.168.1.65";

            private static string TimeServer_1 = "time1.google.com";
            private static string TimeServer_2 = "1.pool.ntp.org";

            //private static string TimeServer_1 = "fritz.box";

            //private static int timeZoneOffset = -720;
            //private static int timeZoneOffset = -715;
            //private static int timeZoneOffset = -500;
            //private static int timeZoneOffset = -300;     // New York offest in minutes of your timezone to Greenwich Mean Time (GMT)
            //private static int timeZoneOffset = -60;
            //private static int timeZoneOffset = 0;       // Lissabon offest in minutes of your timezone to Greenwich Mean Time (GMT)
            private static int timeZoneOffset = 60;             // offest in minutes of your timezone to Greenwich Mean Time (GMT)
            //private static int timeZoneOffset = 120;
            //private static int timeZoneOffset = 180;     // Moskau offest in minutes of your timezone to Greenwich Mean Time (GMT) 
            //private static int timeZoneOffset = 240;
            // private static int timeZoneOffset = 243;
            //private static int timeZoneOffset = 680;
            //private static int timeZoneOffset = 720;

            // Europe                                           //DayLightSavingTimeSettings
            private static int dstOffset = 60; // 1 hour (Europe 2016)
            private static string dstStart = "Mar lastSun @2";
            private static string dstEnd = "Oct lastSun @3"; 
            /*  USA
            private static int dstOffset = 60; // 1 hour (US 2013)
            private static string dstStart = "Mar Sun>=8"; // 2nd Sunday March (US 2013)
            private static string dstEnd = "Nov Sun>=1"; // 1st Sunday Nov (US 2013)
            */


            // if time has elapsed, the acutal entry in the SampleValueBuffer is sent to azure, otherwise it is neglected (here: 1 sec, so it is always sended)
            //private static TimeSpan sendInterval_Burner = new TimeSpan(0, 0, 1); // If this time interval has expired since the last sending to azure,         
            //private static TimeSpan sendInterval_Boiler = new TimeSpan(0, 0, 1);
            //private static TimeSpan sendInterval_Solar = new TimeSpan(0, 0, 1);
            private static TimeSpan sendInterval_Current = new TimeSpan(0, 0, 1);
            //private static TimeSpan sendInterval_SolarTemps = new TimeSpan(0, 0, 1);

            // RoSchmi
            //private static bool workWithWatchDog = true;    // Choose whether the App runs with WatchDog, should normally be set to true
            private static bool workWithWatchDog = false;
            private static int watchDogTimeOut = 50;        // WatchDog timeout in sec: Max Value for G400 15 sec, G120 134 sec, EMX 4.294 sec
            // = 50 sec, don't change without need, may not be below 30 sec 

            static Timer _sensorControlTimer;
            static TimeSpan _sensorControlTimerInterval = new TimeSpan(0, 35, 0);  // 35 minutes
            // The event handler of this  timer checks if there was an event of the solarPumpCurrentSensor
            // in the selected time (here 35 min). This means that the program is running but receiving data via RF69 hangs 
            // in this very rarely occuring case we reset the board



            // If the free ram of the mainboard is below this level it will reboot (because of https memory leak)
            private static int freeRamThreshold = 4300000;
            //private static int freeRamThreshold = 3300000;

            // You can select what kind of Debug.Print messages are sent

            //public static AzureStorageHelper.DebugMode _AzureDebugMode = AzureStorageHelper.DebugMode.StandardDebug;
            public static AzureStorageHelper.DebugMode _AzureDebugMode = AzureStorageHelper.DebugMode.NoDebug;
            public static AzureStorageHelper.DebugLevel _AzureDebugLevel = AzureStorageHelper.DebugLevel.DebugAll;

            // To use Fiddler as WebProxy set attachFiddler = true and set the proper IPAddress and port
            // Use the local IP-Address of the PC where Fiddler is running
            // see: -http://blog.devmobile.co.nz/2013/01/09/netmf-http-debugging-with-fiddler
            private static bool attachFiddler = false;
            private const string fiddlerIPAddress = "192.168.1.21"; // Set to the IP-Adress of your PC
            private const int fiddlerPort = 8888;                   // Standard port of fiddler
        #endregion


            #region Setting concerning your Azure Table Storage Account

            // Set your Azure Storage Account Credentials here or store them in the Resources      
            static string myAzureAccount = Resources.GetString(Resources.StringResources.AzureAccountName);
            //static string myAzureAccount = "your Accountname";


            static string myAzureKey = Resources.GetString(Resources.StringResources.AzureAccountKey);
            //static string myAzureKey = "your key";


            // choose whether http or https shall be used
            //private const bool Azure_useHTTPS = true;
            private const bool Azure_useHTTPS = false;


            // Preset for the Name of the Azure storage table 
            // To build the table name, the table prefix is augmented with the actual year
            // So data from one year can be easily deleted
            // A second table is generated, the name of this table is augmented with the suffix "Days" and the actual year (e.g. TestDays2018)
            //

            
          

            //private static string _tablePreFix_Burner = "Brenner";          // Preset, comes in the sensor eventhandler
            //private static string _tablePreFix_Boiler = "BoilerHeizung";    // Preset, comes in the sensor eventhandle
            private static string _tablePreFix_Solar = "Solar";             // Preset, comes in the sensor eventhandle
            private const string _tablePreFix_Current = "Current";         // Preset, comes in the sensor eventhandle
            //private const string _tablePreFix_SolarTemps = "SolarTemps";    // Preset, comes in the sensor eventhandle


         
          private const string _tablePreFix_Definition_3 = "SolarTemps";   // Preset, doesn't come in the sensor eventhandle



          // Preset for the partitionKey of the table entities.        
          //private static string _partitionKeyPrefix_Burner = "Y3_";   // Preset, comes in the sensor eventhandler
          //private static string _partitionKeyPrefix_Boiler = "Y3_";
          private static string _partitionKeyPrefix_Solar = "Y3_";
          private static string _partitionKey_Current = "Y2_";


          // if augmentPartitionKey == true, the actual year and month are added, e.g. Y_2016_07
          private static bool augmentPartitionKey = true;

       

       //private static string _location_Burner = "Heizung";              // Preset, can be replaced with a value received in the sensor eventhandler
       //private static string _location_Boiler = "Heizung";
       private static string _location_Solar = "Heizung";
       private static string _location_Current = "Keller";

     

    //private static string _sensorValueHeader_Burner = "OnOff";
    //private static string _sensorValueHeader_Boiler = "OnOff";
    private static string _sensorValueHeader_Solar = "OnOff";
    private static string _sensorValueHeader_Current = "T_0";
    //private static string _sensorValueHeader_SolarTemps = "Coll";



    //private static string _socketSensorHeader_Burner = "";  // (not used in this App)
    //private static string _socketSensorHeader_Boiler = "";  // (not used in this App)
    //private static string _socketSensorHeader_Solar = "";  // (not used in this App)
    private static string _socketSensorHeader_Current = "Current";
    private static string _socketSensorHeader_SolarTemps = "ST";
            


            #endregion


            #region Settings concerning Rfm69 receiver

            // Settings for Rfm69 (like Node IDs of sender and recipient) must be set in Class OnOffRfm69SensorMgr.cs

            static int Ch_1_Sel = 1;   // The Channel of the temp/humidity sensor (Values from 1 to 8 are allowed)
            static int Ch_2_Sel = 2;
            static int Ch_3_Sel = 3;
            static int Ch_4_Sel = 4;
            static int Ch_5_Sel = 5;
            static int Ch_6_Sel = 6;
            static int Ch_7_Sel = 7;
            static int Ch_8_Sel = 8;


            #endregion

        #endregion




            #region Fields

            static AutoResetEvent waitForCurrentCallback = new AutoResetEvent(false);


            public static SDCard SD;
            private static bool _fs_ready = false;

            private static GHI.Networking.EthernetENC28J60 netif;
            private static bool _hasAddress;
            private static bool _available;

            private static bool watchDogIsAcitvated = false;// Don't change, choosing is done in the workWithWatchDog variable

            static Thread WatchDogCounterResetThread;

            private static Counters _counters = new Counters();

            private static int _azureSends = 1;
            private static int _forcedReboots = 0;
            private static int _badReboots = 0;
            private static int _azureSendErrors = 0;

            private static bool _willRebootAfterNextEvent = false;

            private const double InValidValue = 999.9;

            // RoSchmi
            static TimeSpan makeInvalidTimeSpan = new TimeSpan(2, 15, 0);  // When this timespan has elapsed, old sensor values are set to invalid


            private static readonly object MainThreadLock = new object();


            // Regex ^: Begin at start of line; [a-zA-Z0-9]: these chars are allowed; [^<>]: these chars ar not allowd; +$: test for every char in string until end of line
            // Is used to exclude some not allowed characters in the strings for the name of the Azure table and the message entity property
            static Regex _tableRegex = new Regex(@"^[a-zA-Z0-9]+$");
            static Regex _columnRegex = new Regex(@"^[a-zA-Z0-9_]+$");
            static Regex _stringRegex = new Regex(@"^[^<>]+$");

            private static int _azureSendThreads = 0;
            private static int _azureSendThreadResetCounter = 0;


            // Certificate of Azure, included as a Resource
            static byte[] caAzure = Resources.GetBytes(Resources.BinaryResources.DigiCert_Baltimore_Root);

            // See -https://blog.devmobile.co.nz/2013/03/01/https-with-netmf-http-client-managing-certificates/ how to include a certificate

            private static X509Certificate[] caCerts;


            private static TimeServiceSettings timeSettings;
            private static bool timeServiceIsRunning = false;
            private static bool timeIsSet = false;

            //private static OnOffDigitalSensorMgr myBurnerSensor;
            //private static OnOffAnalogSensorMgr myStoragePumpSensor;
            private static OnOffRfm69SensorMgr mySolarPumpCurrentSensor;
            private static CloudStorageAccount myCloudStorageAccount;
            //private static AzureSendManager_Burner myAzureSendManager_Burner;
            //private static AzureSendManager_Boiler myAzureSendManager_Boiler;
            //private static AzureSendManager_Solar myAzureSendManager_Solar;
            private static AzureSendManager myAzureSendManager;
            //private static AzureSendManager_SolarTemps myAzureSendManager_SolarTemps;


            static SensorValue[] _sensorValueArr = new SensorValue[8];
            static SensorValue[] _sensorValueArr_last_1 = new SensorValue[8];
            static SensorValue[] _sensorValueArr_last_2 = new SensorValue[8];

            static SensorValue[] _sensorValueArr_Out = new SensorValue[8];
       




        #endregion
        public static void Main()
        {
            Debug.Print(Resources.GetString(Resources.StringResources.String1));

            //OurClass cls = new OurClass();

            #region Try to open SD-Card, if there is no SD-Card, doesn't matter
            try
            {
                SD = new SDCard();
                AutoResetEvent waitForSD = new AutoResetEvent(false);
                SD.Mount();
                RemovableMedia.Insert += (a, b) =>
                {
                    var theA = a;
                    var theB = b;
                    _fs_ready = true;
                    waitForSD.Reset();
                };

                waitForSD.WaitOne(1000, true);
            }
            catch (Exception ex1)
            {
                Debug.Print("\r\nSD-Card not mounted! " + ex1.Message);
            }
            #endregion

            #region Try to get Network parameters from SD-Card, if SD-Card or no file Temp_Survey.xml --> take Program values
            
            if (_fs_ready)
            {
                try
                {
                    if (VolumeInfo.GetVolumes()[0].IsFormatted)
                    {
                        string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;

                        #region If file Temp_Survey.xml does not exist, write XML file to SD-Card
                        if (!File.Exists(rootDirectory + @"\Temp_Survey.xml"))
                        {
                            using (FileStream FileHandleWrite = new FileStream(rootDirectory + @"\Temp_Survey.xml", FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                using (XmlWriter xmlwrite = XmlWriter.Create(FileHandleWrite))
                                {
                                    xmlwrite.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\"");
                                    xmlwrite.WriteRaw("\r\n");
                                    xmlwrite.WriteComment("Contents of this XML file defines the network settings of the device");
                                    xmlwrite.WriteRaw("\r\n");
                                    xmlwrite.WriteStartElement("networksettings"); //root element
                                    xmlwrite.WriteRaw("\r\n");
                                    xmlwrite.WriteElementString("dhcp", "false");
                                    xmlwrite.WriteRaw("\r\n");
                                    xmlwrite.WriteElementString("ipaddress", "192.168.1.66");
                                    xmlwrite.WriteRaw("\r\n");
                                    xmlwrite.WriteElementString("gateway", "192.168.1.1");
                                    xmlwrite.WriteRaw("\r\n");
                                    xmlwrite.WriteElementString("dnsserver", "192.168.1.1");
                                    xmlwrite.WriteRaw("\r\n");
                                    xmlwrite.WriteEndElement();
                                    xmlwrite.WriteRaw("\r\n");
                                    xmlwrite.WriteComment("End");
                                    xmlwrite.Flush();
                                    xmlwrite.Close();
                                }
                            }
                            FinalizeVolumes();
                        }
                        #endregion

                        #region Read contents of file Temp_Survey.xml from SD-Card
                        FileStream FileHandleRead = new FileStream(rootDirectory + @"\Temp_Survey.xml", FileMode.Open);
                        XmlReaderSettings ss = new XmlReaderSettings();
                        ss.IgnoreWhitespace = true;
                        ss.IgnoreComments = false;
                        XmlReader xmlr = XmlReader.Create(FileHandleRead, ss);
                        string actElement = string.Empty;
                        while (!xmlr.EOF)
                        {
                            xmlr.Read();
                            switch (xmlr.NodeType)
                            {
                                case XmlNodeType.Element:
                                    //Debug.Print("element: " + xmlr.Name);
                                    actElement = xmlr.Name;
                                    break;
                                case XmlNodeType.Text:
                                    //Debug.Print("text: " + xmlr.Value);
                                    switch (actElement)
                                    {
                                        case "dhcp":
                                            if (xmlr.Value == "true")
                                            {
                                                useDHCP = true;
                                            }
                                            else
                                            {
                                                useDHCP = false;
                                            }
                                            break;
                                        case "ipaddress":
                                            DeviceIpAddress = xmlr.Value;
                                            break;
                                        case "gateway":
                                            GateWayIpAddress = xmlr.Value;
                                            break;
                                        case "dnsserver":
                                            GateWayIpAddress = xmlr.Value;
                                            break;
                                        default:
                                            break;
                                    }
                                    break;
                               
                                default:
                                    //Debug.Print(xmlr.NodeType.ToString());
                                    break;
                            }
                        }
                        #endregion

                        SD.Unmount();
                    }
                    else
                    {
                        Debug.Print("Storage is not formatted. " + "Format on PC with FAT32/FAT16 first!");
                    }
                    try
                    {
                        SD.Unmount();
                    }
                    catch { };
                }
                catch (Exception ex)
                {
                    Debug.Print("SD-Card not opened! " + ex.Message);
                }
            }
            #endregion

            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;

            //For Cobra III and ethernet ENC28 module on GXP Gadgeteer Bridge Socket 1 (SU)
           
            netif = new GHI.Networking.EthernetENC28J60(Microsoft.SPOT.Hardware.SPI.SPI_module.SPI2, G120.P0_5, G120.P0_4, G120.P4_28);

            //netif = new GHI.Networking.EthernetENC28J60(Microsoft.SPOT.Hardware.SPI.SPI_module.SPI3, G120.P0_5, G120.P0_4, G120.P4_28);

            // Not needed for Buildin Ethernet, is used to get a valid MAC Address vor Ethernet ENC28 Module
             var myMac = GenerateUniqueMacAddr.GenerateUniqueMacAddress(myAzureAccount);

            netif.Open();

            if (useDHCP)
            {
                // for DHCP 
                netif.EnableDhcp();
                netif.EnableDynamicDns();
                while (netif.IPAddress == "0.0.0.0")
                {
                    Debug.Print("Wait DHCP");
                    Thread.Sleep(300);
                }
                _hasAddress = true;
                Debug.Print("IP is: " + netif.IPAddress);
            }
            else
            {
                // for static IP
                netif.EnableStaticIP(DeviceIpAddress, SubnetMask, GateWayIpAddress);
                netif.EnableStaticDns(new[] { DnsServerIpAddress });
                while (!_hasAddress || !_available)
                {
                    Debug.Print("Wait static IP");
                    Thread.Sleep(100);
                }
            }

          //  caCerts = new X509Certificate[] { new X509Certificate(caAllnet), new X509Certificate(caAzure), new X509Certificate(caSparkPost) };
            caCerts = new X509Certificate[] { new X509Certificate(caAzure) };

            #region Set some Presets for Azure Table and others

            myCloudStorageAccount = new CloudStorageAccount(myAzureAccount, myAzureKey, useHttps: Azure_useHTTPS);


            myAzureSendManager = new AzureSendManager(myCloudStorageAccount, timeZoneOffset, dstStart, dstEnd, dstOffset, _tablePreFix_Current, _sensorValueHeader_Current, _socketSensorHeader_Current, caCerts, DateTime.Now, sendInterval_Current, _azureSends, _AzureDebugMode, _AzureDebugLevel, IPAddress.Parse(fiddlerIPAddress), pAttachFiddler: attachFiddler, pFiddlerPort: fiddlerPort, pUseHttps: Azure_useHTTPS);
            AzureSendManager.sampleTimeOfLastSent = DateTime.Now.AddDays(-10.0);    // Date in the past
            AzureSendManager.InitializeQueue();


            //mySolarPumpCurrentSensor = new OnOffRfm69SensorMgr(DeviceType.EMX, 6, dstOffset, dstStart, dstEnd, _partitionKeyPrefix_Solar, _location_Solar, _sensorValueHeader_Solar, _sensorValueHeader_Current, _tablePreFix_Solar, _tablePreFix_Current, "0");
            mySolarPumpCurrentSensor = new OnOffRfm69SensorMgr(DeviceType.G120, 6, dstOffset, dstStart, dstEnd, _partitionKeyPrefix_Solar, _location_Solar, _sensorValueHeader_Solar, _sensorValueHeader_Current, _tablePreFix_Solar, _tablePreFix_Current, "0");


            mySolarPumpCurrentSensor.rfm69OnOffSensorSend += mySolarPumpCurrentSensor_rfm69OnOffSensorSend;
            mySolarPumpCurrentSensor.rfm69DataSensorSend += mySolarPumpCurrentSensor_rfm69DataSensorSend;
            mySolarPumpCurrentSensor.rfm69SolarTempsDataSensorSend += mySolarPumpCurrentSensor_rfm69SolarTempsDataSensorSend;
            #endregion

            mySolarPumpCurrentSensor.Start();

            // finally: blinking a LED, just for fun
            //_Led = new OutputPort(_deviceType == GHI.Processor.DeviceType.EMX ? GHI.Pins.FEZSpider.DebugLed : GHI.Pins.FEZSpiderII.DebugLed, true);
            while (true)
            {
                //_Led.Write(true);
                Thread.Sleep(200);
                //_Led.Write(false);
                Thread.Sleep(200);
            }    

        }

        #region NetworkAddressChanged
        static void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            Debug.Print("The network address has changed.");
            _hasAddress = netif.IPAddress != "0.0.0.0";
            Debug.Print("IP is: " + netif.IPAddress);

            if (!timeIsSet)
            {
                if (DateTime.Now < new DateTime(2016, 7, 1))
                {
                    Debug.Print("Going to set the time in NetworkAddressChanged event");
                    SetTime(timeZoneOffset, TimeServer_1, TimeServer_2);
                    Thread.Sleep(200);
                }
            }
            Thread.Sleep(20);

            Debug.Print("Time is: " + DateTime.Now.AddMinutes(RoSchmi.DayLihtSavingTime.DayLihtSavingTime.DayLightTimeOffset(dstStart, dstEnd, dstOffset, DateTime.Now, true)).ToString());
        }
        #endregion


        #region NetworkAvailabilityChanged
        static void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            //Debug.Print("Network available: " + e.IsAvailable.ToString());

            _available = e.IsAvailable;
        }
        #endregion 0


        #region Event SensorControlTimer_SignalReceived
        static void _sensorControlTimer_Tick(object o)
        {
            Microsoft.SPOT.Hardware.PowerState.RebootDevice(true, 3000);
        }

        #endregion


        #region Event SolarPumpSolarTempsDataSensor_SignalReceived
        static void mySolarPumpCurrentSensor_rfm69SolarTempsDataSensorSend(OnOffRfm69SensorMgr sender, OnOffRfm69SensorMgr.DataSensorEventArgs e)
        {
            Debug.Print("SolarTemps Signal received");
        }
        #endregion




        #region Event SolarPumpCurrentDataSensor_SignalReceived
        // This eventmanager is for the case when Continuous Sensordata from the Smartmeter (and Fritz!Dect) were sent
        static void mySolarPumpCurrentSensor_rfm69DataSensorSend(OnOffRfm69SensorMgr sender, OnOffRfm69SensorMgr.DataSensorEventArgs e)
        {
            Debug.Print("Current Signal received");
            
           

            string outString = string.Empty;
            bool forceSend = false;
            // The magic word EscapeTableLocation_03 makes, that not the tabelname from e is used, but from the const _tablePreFix_Definition_3
            string tablePreFix = (e.DestinationTable == "EscapeTableLocation_03") ? _tablePreFix_Definition_3 : e.DestinationTable;


            // get minimal and maximal gained Power by the the solar panel
            double dayMaxBefore = AzureSendManager._dayMax < 0 ? 0.00 : AzureSendManager._dayMax;
            double dayMinBefore = AzureSendManager._dayMin > 70 ? 0.00 : AzureSendManager._dayMin;

            
            // Get Power from Fritzbox (not needed here)
            // string solarPower = fritz.getSwitchPower(FRITZ_DEVICE_AIN_01);
            // double decimalValue = solarPower != null ? double.Parse(solarPower) / 10000 : InValidValue;
            //double logCurrent = ((decimalValue > 170) || (decimalValue < -40)) ? InValidValue : (decimalValue > 160) ? 160.0 : decimalValue;

            double decimalValue = InValidValue;
            double logCurrent = InValidValue; 


            // Get Energy from Fritzbox (not needed here)
            //string solarEnergy = fritz.getSwitchEnergy(FRITZ_DEVICE_AIN_01);
            //double solarEnergy_decimal_value = solarEnergy != null ? double.Parse(solarEnergy) / 100 : InValidValue;

            double solarEnergy_decimal_value = InValidValue;



            double t4_decimal_value = (double)e.Val_2 / 100;      // measuredPower
            double t2_decimal_value = (t4_decimal_value > 6000) ? 60 : (t4_decimal_value / 100);

            double t5_decimal_value = (double)Reform_uint16_2_float32.Convert((UInt16)(e.Val_3 >> 16), (UInt16)(e.Val_3 & 0x0000FFFF));  // measuredWork 

#if SD_Card_Logging
                var source = new LogContent() { logOrigin = "Event: RF 433 Signal received", logReason = "n", logPosition = "RF 433 event Start", logNumber = 1 };
                SdLoggerService.LogEventHourly("Normal", source);
#endif

#if DebugPrint
            Debug.Print("Rfm69 event, Data: " + decimalValue.ToString("f2") + " Amps " + t4_decimal_value.ToString("f2") + " Watt " + t5_decimal_value.ToString("f2") + " KWh");
#endif
            // RoSchmi
            //activateWatchdogIfAllowedAndNotYetRunning();

            #region Preset some table parameters like Row Headers
            // Here we set some table parameters which where transmitted in the eventhandler and were set in the constructor of the RF_433_Receiver Class

            //_tablePreFix_Current = e.DestinationTable;

            _partitionKey_Current = e.SensorLabel;
            _location_Current = e.SensorLocation;
            _sensorValueHeader_Current = e.MeasuredQuantity;
            _socketSensorHeader_Current = "NU";
            #endregion


            DateTime timeOfThisEvent = DateTime.Now;
            AzureSendManager._timeOfLastSensorEvent = timeOfThisEvent;

            // Reset _sensorControlTimer, if the timer is not reset, the board will be rebooted
            _sensorControlTimer = new Timer(new TimerCallback(_sensorControlTimer_Tick), null, _sensorControlTimerInterval, _sensorControlTimerInterval);
            // when no sensor events occure in a certain timespan

            string switchMessage = "Switch Message Preset";
            string switchState = "???";
            string actCurrent = "???";

            #region Test if timeService is running. If not, try to initialize
            if (!timeServiceIsRunning)
            {
                if (DateTime.Now < new DateTime(2016, 7, 1))
                {
#if DebugPrint
                        Debug.Print("Going to set the time in rf_433_Receiver_SignalReceived event");
#endif
                    try { GHI.Processor.Watchdog.ResetCounter(); }
                    catch { };
                    SetTime(timeZoneOffset, TimeServer_1, TimeServer_2);
                    Thread.Sleep(200);
                }
                else
                {
                    timeServiceIsRunning = true;
                }
                if (!timeServiceIsRunning)
                {
#if DebugPrint
                        Debug.Print("Sending aborted since timeservice is not running");
#endif
                    return;
                }
            }
            #endregion

            #region Do some tests with RegEx to assure that proper content is transmitted to the Azure table

            RegexTest.ThrowIfNotValid(_tableRegex, new string[] { tablePreFix, _location_Current });
            RegexTest.ThrowIfNotValid(_columnRegex, new string[] { _sensorValueHeader_Current });

            #endregion


            #region After a reboot: Read the last stored entity from Azure to actualize the counters
            if (AzureSendManager._iteration == 0)    // The system has rebooted: We read the last entity from the Cloud
            {
                _counters = myAzureSendManager.ActualizeFromLastAzureRow(ref switchMessage);

                _azureSendErrors = _counters.AzureSendErrors > _azureSendErrors ? _counters.AzureSendErrors : _azureSendErrors;
                _azureSends = _counters.AzureSends > _azureSends ? _counters.AzureSends : _azureSends;
                _forcedReboots = _counters.ForcedReboots > _forcedReboots ? _counters.ForcedReboots : _forcedReboots;
                _badReboots = _counters.BadReboots > _badReboots ? _counters.BadReboots : _badReboots;


                forceSend = true;
                // actualize to consider the timedelay caused by reading from the cloud
                timeOfThisEvent = DateTime.Now;
                AzureSendManager._timeOfLastSensorEvent = timeOfThisEvent;
            }
            #endregion

            // when every sample value shall be sent to Azure, remove the outcomment of the next two lines
            //forceSend = true;                 
            //switchMessage = "Sending was forced by Program";              

            #region Check if we still have enough free ram (memory leak in https) and evtl. prepare for resetting the mainboard
            uint remainingRam = Debug.GC(false);            // Get remaining Ram because of the memory leak in https
            bool willReboot = (remainingRam < freeRamThreshold);     // If the ram is below this value, the Mainboard will reboot
            if (willReboot)
            {
                forceSend = true;
                switchMessage = "Going to reboot the Mainboard due to not enough free RAM";
            }
            #endregion

            DateTime copyTimeOfLastSend = AzureSendManager._timeOfLastSend;

            TimeSpan timeFromLastSend = timeOfThisEvent - copyTimeOfLastSend;

            double daylightCorrectOffset = DayLihtSavingTime.DayLightTimeOffset(dstStart, dstEnd, dstOffset, DateTime.Now, true);

            #region Set the partitionKey
            //string partitionKey = _partitionKey_Current;                    // Set Partition Key for Azure storage table
            string partitionKey = e.SensorLabel;                    // Set Partition Key for Azure storage table
            if (augmentPartitionKey == true)                        // if wanted, augment with year and month (12 - month for right order)
            //{ partitionKey = partitionKey + DateTime.Now.ToString("yyyy") + "-" + X_Stellig.Zahl((12 - DateTime.Now.Month).ToString(), 2); }
            { partitionKey = partitionKey + DateTime.Now.AddMinutes(daylightCorrectOffset).ToString("yyyy") + "-" + X_Stellig.Zahl((12 - DateTime.Now.AddMinutes(daylightCorrectOffset).Month).ToString(), 2); }
            #endregion

            #region Regex test for proper content of the Message property in the Azure table
            // The regex test can be outcommented if the string is valid
            if (!_stringRegex.IsMatch(switchMessage))
            { throw new NotSupportedException("Some charcters [<>] may not be used in this string"); }
            #endregion


            #region If sendInterval has expired, write new sample value row to the buffer and start writing to Azure
            if ((timeFromLastSend > AzureSendManager._sendInterval) || forceSend)
            {
                #region actualize the values of minumum and maximum measurements of the day

                if (AzureSendManager._timeOfLastSend.AddMinutes(daylightCorrectOffset).Day == timeOfThisEvent.AddMinutes(daylightCorrectOffset).Day)
                {
                    // same day as event before
                    // RoSchmi
                    AzureSendManager._dayMaxWorkBefore = AzureSendManager._dayMaxWork;
                    AzureSendManager._dayMinWorkBefore = AzureSendManager._dayMinWork;
                    AzureSendManager._dayMaxSolarWorkBefore = AzureSendManager._dayMaxSolarWork;
                    AzureSendManager._dayMinSolarWorkBefore = AzureSendManager._dayMinSolarWork;

                    Debug.Print(AzureSendManager._dayMaxWork.ToString("F4"));
                    Debug.Print(AzureSendManager._dayMaxWorkBefore.ToString("F4"));


                    AzureSendManager._dayMaxWork = t5_decimal_value;     // measuredWork
                    if (AzureSendManager._dayMinWork < 0.1)
                    {
                        AzureSendManager._dayMinWorkBefore = AzureSendManager._dayMinWork;
                        AzureSendManager._dayMinWork = t5_decimal_value;  // measuredWork
                    }

                    /*
                    AzureSendManager._dayMaxSolarWork = solarEnergy_decimal_value;     // measuredSolarWork
                    if (AzureSendManager._dayMinSolarWork < 0.1)
                    {
                        AzureSendManager._dayMinSolarWork = solarEnergy_decimal_value;  // measuredSolarWork
                    }
                    */
                    AzureSendManager._dayMaxSolarWork = 0.0;
                    AzureSendManager._dayMinSolarWork = 0.0;



                    if ((decimalValue > AzureSendManager._dayMax) && (decimalValue < 70.0))
                    {
                        AzureSendManager._dayMax = decimalValue;
                    }
                    if ((decimalValue > -39.0) && ((decimalValue < AzureSendManager._dayMin)) || AzureSendManager._dayMin < 0.001)
                    {
                        AzureSendManager._dayMin = decimalValue;
                    }



                }
                else
                {
                    // first event of a new day

                    // decimalValue is actually gained power of solar panel
                    if ((decimalValue > -39.0) && (decimalValue < 70.0))
                    {
                        AzureSendManager._dayMax = decimalValue;
                        AzureSendManager._dayMin = decimalValue;
                    }
                    AzureSendManager._dayMinWorkBefore = AzureSendManager._dayMinWork;
                    AzureSendManager._dayMinWork = t5_decimal_value;                        // measuredWork
                    AzureSendManager._dayMaxWorkBefore = AzureSendManager._dayMaxWork;
                    AzureSendManager._dayMaxWork = t5_decimal_value;                        // measuredWork

                    AzureSendManager._dayMinSolarWorkBefore = AzureSendManager._dayMinSolarWork;
                    AzureSendManager._dayMinSolarWork = solarEnergy_decimal_value;                        // measuredWork
                    AzureSendManager._dayMaxSolarWorkBefore = AzureSendManager._dayMaxSolarWork;
                    AzureSendManager._dayMaxSolarWork = solarEnergy_decimal_value;                        // measuredWork



                }
                #endregion

                AzureSendManager._lastValue = decimalValue;

                for (int i = 0; i < 8; i++)
                {
                    _sensorValueArr_Out[i] = new SensorValue(AzureSendManager._timeOfLastSensorEvent, 0, 0, 0, 0, InValidValue, 999, 0x00, false);
                }


                _sensorValueArr_Out[Ch_2_Sel - 1].TempDouble = t2_decimal_value;                 // T_2 : Power, limited to a max. Value

                //RoSchmi
                // T_1 : Solar Work of this day
                _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble = ((AzureSendManager._dayMaxSolarWork - AzureSendManager._dayMinSolarWork) <= 0) ? 0.00 : AzureSendManager._dayMaxSolarWork - AzureSendManager._dayMinSolarWork;
                // _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble = _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble <  9 ? _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble * 10 : 90.00;    // set limit to 90 and change scale
                _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble = _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble < 9 ? _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble * 0.5 : 90.00;    // set limit to 90 and change scale

                // RoSchmi
                // T_3 : Work of this day
                _sensorValueArr_Out[Ch_3_Sel - 1].TempDouble = ((AzureSendManager._dayMaxWork - AzureSendManager._dayMinWork) <= 0) ? 0.00 : AzureSendManager._dayMaxWork - AzureSendManager._dayMinWork;
                _sensorValueArr_Out[Ch_3_Sel - 1].TempDouble = _sensorValueArr_Out[Ch_3_Sel - 1].TempDouble < 18 ? _sensorValueArr_Out[Ch_3_Sel - 1].TempDouble * 5 : 90.00;    // set limit to 90 and change scale


                _sensorValueArr_Out[Ch_4_Sel - 1].TempDouble = t4_decimal_value;                // T_4 : Power
                _sensorValueArr_Out[Ch_5_Sel - 1].TempDouble = t5_decimal_value;                // T_5 : Work
                _sensorValueArr_Out[Ch_6_Sel - 1].TempDouble = AzureSendManager._dayMinWork;    // T_6 : Work at start of day

                double energyConsumptionLastDay = AzureSendManager._dayMinWorkBefore < 0.01 ? 0 : (AzureSendManager._dayMaxWorkBefore - AzureSendManager._dayMinWorkBefore) / 5.0;

                AzureSendManager._iteration++;

                SampleValue theRow = new SampleValue(tablePreFix + DateTime.Now.Year, partitionKey, e.Timestamp, timeZoneOffset + (int)daylightCorrectOffset, logCurrent, AzureSendManager._dayMin, AzureSendManager._dayMax,
                   _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_1_Sel - 1].RandomId, _sensorValueArr_Out[Ch_1_Sel - 1].Hum, _sensorValueArr_Out[Ch_1_Sel - 1].BatteryIsLow,
                   _sensorValueArr_Out[Ch_2_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_2_Sel - 1].RandomId, _sensorValueArr_Out[Ch_2_Sel - 1].Hum, _sensorValueArr_Out[Ch_2_Sel - 1].BatteryIsLow,
                   _sensorValueArr_Out[Ch_3_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_3_Sel - 1].RandomId, _sensorValueArr_Out[Ch_3_Sel - 1].Hum, _sensorValueArr_Out[Ch_3_Sel - 1].BatteryIsLow,
                   _sensorValueArr_Out[Ch_4_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_4_Sel - 1].RandomId, _sensorValueArr_Out[Ch_4_Sel - 1].Hum, _sensorValueArr_Out[Ch_4_Sel - 1].BatteryIsLow,
                   _sensorValueArr_Out[Ch_5_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_5_Sel - 1].RandomId, _sensorValueArr_Out[Ch_5_Sel - 1].Hum, _sensorValueArr_Out[Ch_5_Sel - 1].BatteryIsLow,
                   _sensorValueArr_Out[Ch_6_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_6_Sel - 1].RandomId, _sensorValueArr_Out[Ch_6_Sel - 1].Hum, _sensorValueArr_Out[Ch_6_Sel - 1].BatteryIsLow,
                   _sensorValueArr_Out[Ch_7_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_7_Sel - 1].RandomId, _sensorValueArr_Out[Ch_7_Sel - 1].Hum, _sensorValueArr_Out[Ch_7_Sel - 1].BatteryIsLow,
                   _sensorValueArr_Out[Ch_8_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_8_Sel - 1].RandomId, _sensorValueArr_Out[Ch_8_Sel - 1].Hum, _sensorValueArr_Out[Ch_8_Sel - 1].BatteryIsLow,
                   actCurrent, switchState, _location_Current, timeFromLastSend, e.RepeatSend, e.RSSI, AzureSendManager._iteration, remainingRam, _forcedReboots, _badReboots, _azureSendErrors, willReboot ? 'X' : '.', forceSend, forceSend ? switchMessage : "");


                if (AzureSendManager._iteration == 1)
                {
                    if (timeFromLastSend < makeInvalidTimeSpan)   // after reboot for the first time take values which were read back from the Cloud
                    {
                        //theRow.T_0 = AzureSendManager._lastContent[Ch_1_Sel - 1];
                        //theRow.T_1 = AzureSendManager._lastContent[Ch_2_Sel - 1];
                        theRow.T_2 = AzureSendManager._lastContent[Ch_3_Sel - 1];
                        //theRow.T_3 = AzureSendManager._lastContent[Ch_4_Sel - 1];
                        //theRow.T_4 = AzureSendManager._lastContent[Ch_5_Sel - 1];
                        theRow.T_5 = AzureSendManager._lastContent[Ch_6_Sel - 1];
                        //theRow.T_6 = AzureSendManager._lastContent[Ch_7_Sel - 1];
                        //theRow.T_7 = AzureSendManager._lastContent[Ch_8_Sel - 1];
                    }
                    else
                    {
                        //theRow.T_0 = InValidValue;
                        //theRow.T_1 = InValidValue;
                        theRow.T_2 = InValidValue;
                        //theRow.T_3 = InValidValue;
                        //theRow.T_4 = InValidValue;
                        theRow.T_5 = InValidValue;
                        //theRow.T_6 = InValidValue;
                        //theRow.T_7 = InValidValue;
                    }
                }


                if (AzureSendManager.hasFreePlaces())
                {
                    AzureSendManager.EnqueueSampleValue(theRow);

                    // RoSchmi
                    //copyTimeOfLastSend = timeOfThisEvent;
                    AzureSendManager._timeOfLastSend = timeOfThisEvent;

                    //Debug.Print("\r\nRow was writen to the Buffer. Number of rows in the buffer = " + AzureSendManager.Count + ", still " + (AzureSendManager.capacity - AzureSendManager.Count).ToString() + " places free");
                }
                // optionally send message to Debug.Print  
                //SampleValue theReturn = AzureSendManager.PreViewNextSampleValue();
                //DateTime thatTime = theReturn.TimeOfSample;
                //double thatDouble = theReturn.TheSampleValue;
                //Debug.Print("The Temperature: " + thatDouble.ToString() + "  at: " + thatTime.ToString());

            #endregion


                #region ligth a multicolour led asynchronously to indicate action
#if MulticolorLed
                myMulticolorLedAsync.light("green", 3000);
#endif
                #endregion

                #region If sendInterval has expired, send contents of the buffer to Azure
                //if (_azureSendThreads == 0)
                if (_azureSendThreads == 0)
                {
                    lock (MainThreadLock)
                    {
                        _azureSendThreads++;
                    }
                    // RoSchmi
                    myAzureSendManager = new AzureSendManager(myCloudStorageAccount, timeZoneOffset, dstStart, dstEnd, dstOffset, tablePreFix, _sensorValueHeader_Current, _socketSensorHeader_Current, caCerts, timeOfThisEvent, sendInterval_Current, _azureSends, _AzureDebugMode, _AzureDebugLevel, IPAddress.Parse(fiddlerIPAddress), pAttachFiddler: attachFiddler, pFiddlerPort: fiddlerPort, pUseHttps: Azure_useHTTPS);
                    myAzureSendManager.AzureCommandSend += myAzureSendManager_AzureCommandSend;
                    try { GHI.Processor.Watchdog.ResetCounter(); }
                    catch { };
                    _Print_Debug("\r\nRow was sent on its way to Azure");
                    myAzureSendManager.Start();

                    //RoSchmi
                    // if last send was yesterday: write 
                    var last = copyTimeOfLastSend.AddMinutes(daylightCorrectOffset).AddDays(1).Day;

                    var theAct = timeOfThisEvent.AddMinutes(daylightCorrectOffset).Day;

                    // if we have the first upload of a new day
                    if (copyTimeOfLastSend.AddMinutes(daylightCorrectOffset).AddDays(1).Day == timeOfThisEvent.AddMinutes(daylightCorrectOffset).Day)

                    //if (true)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            _sensorValueArr_Out[i] = new SensorValue(copyTimeOfLastSend, 0, 0, 0, 0, InValidValue, 999, 0x00, false);

                        }

                        //RoSchmi
                        // T_1 : Solar Work of this day
                        //_sensorValueArr_Out[Ch_1_Sel - 1].TempDouble = ((AzureSendManager._dayMaxSolarWork - AzureSendManager._dayMinSolarWork) <= 0) ? 0.00 : AzureSendManager._dayMaxSolarWork - AzureSendManager._dayMinSolarWork;

                        _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble = ((AzureSendManager._dayMaxSolarWorkBefore - AzureSendManager._dayMinSolarWorkBefore) <= 0) ? 0.00 : AzureSendManager._dayMaxSolarWorkBefore - AzureSendManager._dayMinSolarWorkBefore;
                        // _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble = _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble < 9 ? _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble * 10 : 90.00;    // set limit to 90 and change scale
                        _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble = _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble < 9 ? _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble * 0.5 : 90.00;    // set limit to 90 and change scale




                        forceSend = true;


                        theRow = new SampleValue(tablePreFix + "Days" + DateTime.Now.Year, partitionKey, copyTimeOfLastSend.AddMinutes(RoSchmi.DayLihtSavingTime.DayLihtSavingTime.DayLightTimeOffset(dstStart, dstEnd, dstOffset,
                            copyTimeOfLastSend, true)), timeZoneOffset + (int)daylightCorrectOffset, energyConsumptionLastDay, dayMinBefore, dayMaxBefore,
                           _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_1_Sel - 1].RandomId, _sensorValueArr_Out[Ch_1_Sel - 1].Hum, _sensorValueArr_Out[Ch_1_Sel - 1].BatteryIsLow,
                           _sensorValueArr_Out[Ch_2_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_2_Sel - 1].RandomId, _sensorValueArr_Out[Ch_2_Sel - 1].Hum, _sensorValueArr_Out[Ch_2_Sel - 1].BatteryIsLow,
                           _sensorValueArr_Out[Ch_3_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_3_Sel - 1].RandomId, _sensorValueArr_Out[Ch_3_Sel - 1].Hum, _sensorValueArr_Out[Ch_3_Sel - 1].BatteryIsLow,
                           _sensorValueArr_Out[Ch_4_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_4_Sel - 1].RandomId, _sensorValueArr_Out[Ch_4_Sel - 1].Hum, _sensorValueArr_Out[Ch_4_Sel - 1].BatteryIsLow,
                           _sensorValueArr_Out[Ch_5_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_5_Sel - 1].RandomId, _sensorValueArr_Out[Ch_5_Sel - 1].Hum, _sensorValueArr_Out[Ch_5_Sel - 1].BatteryIsLow,
                           _sensorValueArr_Out[Ch_6_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_6_Sel - 1].RandomId, _sensorValueArr_Out[Ch_6_Sel - 1].Hum, _sensorValueArr_Out[Ch_6_Sel - 1].BatteryIsLow,
                           _sensorValueArr_Out[Ch_7_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_7_Sel - 1].RandomId, _sensorValueArr_Out[Ch_7_Sel - 1].Hum, _sensorValueArr_Out[Ch_7_Sel - 1].BatteryIsLow,
                           _sensorValueArr_Out[Ch_8_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_8_Sel - 1].RandomId, _sensorValueArr_Out[Ch_8_Sel - 1].Hum, _sensorValueArr_Out[Ch_8_Sel - 1].BatteryIsLow,
                           " ", " ", _location_Current, new TimeSpan(0), e.RepeatSend, e.RSSI, AzureSendManager._iteration, remainingRam, _forcedReboots, _badReboots, _azureSendErrors, willReboot ? 'X' : '.', forceSend, forceSend ? switchMessage : "");


                        waitForCurrentCallback.Reset();
                        waitForCurrentCallback.WaitOne(50000, true);

                        Thread.Sleep(5000); // Wait additional 5 sec for last thread AzureSendManager Thread to finish
                        AzureSendManager.EnqueueSampleValue(theRow);

                        // RoSchmi
                        //myAzureSendManager = new AzureSendManager(myCloudStorageAccount, timeZoneOffset, dstStart, dstEnd, dstOffset, e.DestinationTable + "Days", "Work", _socketSensorHeader_Current, caCerts, timeOfThisEvent, sendInterval_Current, _azureSends, _AzureDebugMode, _AzureDebugLevel, IPAddress.Parse(fiddlerIPAddress), pAttachFiddler: attachFiddler, pFiddlerPort: fiddlerPort, pUseHttps: Azure_useHTTPS);
                        myAzureSendManager = new AzureSendManager(myCloudStorageAccount, timeZoneOffset, dstStart, dstEnd, dstOffset, e.DestinationTable + "Days", _sensorValueHeader_Current, _socketSensorHeader_Current, caCerts, timeOfThisEvent, sendInterval_Current, _azureSends, _AzureDebugMode, _AzureDebugLevel, IPAddress.Parse(fiddlerIPAddress), pAttachFiddler: attachFiddler, pFiddlerPort: fiddlerPort, pUseHttps: Azure_useHTTPS);


                        myAzureSendManager.AzureCommandSend += myAzureSendManager_AzureCommandSend;
                        try { GHI.Processor.Watchdog.ResetCounter(); }
                        catch { };
                        //_Print_Debug("\r\nRow was sent on its way to Azure");
#if DebugPrint
                            Debug.Print("\r\nLast Row of day was sent on its way to Azure");
#endif

                        myAzureSendManager.Start();


                    }
                }
                else
                {
                    _azureSendThreadResetCounter++;
#if DebugPrint
                        Debug.Print("_azureSendThreadResetCounter = " + _azureSendThreadResetCounter.ToString());
#endif
                    if (_azureSendThreadResetCounter > 5)   // when _azureSendThread != 0 we write the next 5 rows coming through sensor events only to the buffer
                    // this should give outstanding requests time to finish
                    // then we reset the counters
                    {
                        _azureSendThreadResetCounter = 0;
                        _azureSendThreads = 0;
                    }
                }
            }
            else
            {
#if MulticolorLed
                myMulticolorLedAsync.light("red", 200);
#endif
#if DebugPrint
                    Debug.Print("\r\nRow was discarded, sendInterval was not expired ");
#endif
            }
#if DebugPrint
                Debug.Print("\r\nRemaining Ram:" + remainingRam.ToString() + "\r\n");
#endif
                #endregion

            #region Prepare rebooting of the mainboard e.g. if not enough ram due to memory leak
            if (_willRebootAfterNextEvent)
            {
#if DebugPrint
                    Debug.Print("Board is going to reboot in 3000 ms\r\n");
#endif
                Microsoft.SPOT.Hardware.PowerState.RebootDevice(true, 3000);
            }
            if (willReboot)
            { _willRebootAfterNextEvent = true; }
            #endregion
#if DisplayN18
            displayN18.Clear();
            displayN18.Orientation = GTM.Module.DisplayModule.DisplayOrientation.Clockwise90Degrees;
            if (lastOutString != string.Empty)
            { displayN18.SimpleGraphics.DisplayText(lastOutString, RenderingFont, Gadgeteer.Color.Black, 1, 1); }
            lastOutString = outString.Substring(48, 8) + " " + outString.Substring(88, 8);
            displayN18.SimpleGraphics.DisplayText(lastOutString, RenderingFont, Gadgeteer.Color.Orange, 1, 1);
#endif
#if SD_Card_Logging
                source = new LogContent() { logOrigin = "Event: RF 433 Signal received", logReason = "n", logPosition = "End of method", logNumber = 1 };
                SdLoggerService.LogEventHourly("Normal", source);
#endif
        
        }

        #endregion




        #region Event SolarPumpOnOffSensor Signal received

        static void mySolarPumpCurrentSensor_rfm69OnOffSensorSend(OnOffRfm69SensorMgr sender, OnOffBaseSensorMgr.OnOffSensorEventArgs e)
        {

            Debug.Print("SolarPump Signal received");
        }

        #endregion
      

        #region Method Finalize Volumes
        public static void FinalizeVolumes()
        {
            VolumeInfo[] vi = VolumeInfo.GetVolumes();
            for (int i = 0; i < vi.Length; i++)
                vi[i].FlushAll();
        }
        #endregion


        #region Method SetTime()
        public static void SetTime(int pTimeZoneOffset, string pTimeServer_1, string pTimeServer_2)
        {
           // activateWatchdogIfAllowedAndNotYetRunning();

            timeSettings = new Microsoft.SPOT.Time.TimeServiceSettings()
            {
                RefreshTime = 21600,                         // every 6 hours (60 x 60 x 6) default: 300000 sec                    
                AutoDayLightSavings = false,                 // We use our own timeshift calculation
                ForceSyncAtWakeUp = true,
                Tolerance = 30000                            // deviation may be up to 30 sec
            };

            int loopCounter = 1;
            while (loopCounter < 3)
            {
                try { GHI.Processor.Watchdog.ResetCounter(); }
                catch { };
                IPAddress[] address = null;
                IPAddress[] address_2 = null;

                try
                {
                    address = System.Net.Dns.GetHostEntry(pTimeServer_1).AddressList;
                }
                catch { };
                try { GHI.Processor.Watchdog.ResetCounter(); }
                catch { };
                try
                {
                    address_2 = System.Net.Dns.GetHostEntry(pTimeServer_2).AddressList;
                }
                catch { };
                try { GHI.Processor.Watchdog.ResetCounter(); }
                catch { };

                try
                {
                    timeSettings.PrimaryServer = address[0].GetAddressBytes();
                }
                catch { };
                try
                {
                    timeSettings.AlternateServer = address_2[0].GetAddressBytes();
                }
                catch { };

                FixedTimeService.Settings = timeSettings;
                FixedTimeService.SetTimeZoneOffset(pTimeZoneOffset);

                Debug.Print("Starting Timeservice");
                FixedTimeService.Start();
                Debug.Print("Returned from Starting Timeservice");
                Thread.Sleep(100);
                if (DateTime.Now > new DateTime(2016, 7, 1))
                {
                    timeServiceIsRunning = true;
                    Debug.Print("Timeserver intialized on try: " + loopCounter);
                    Debug.Print("Synchronization Interval = " + timeSettings.RefreshTime);
                    break;
                }
                else
                {
                    timeServiceIsRunning = false;
                    Debug.Print("Timeserver could not be intialized on try: " + loopCounter);
                }
                loopCounter++;
            }

            if (timeServiceIsRunning)
            {
                RealTimeClock.SetDateTime(DateTime.Now); //This will set the hardware Real-time Clock
            }
            else
            {
                Debug.Print("No success to get time over internet");
                Utility.SetLocalTime(RealTimeClock.GetDateTime()); // Set System Time to RealTimeClock Time
            }

            //Utility.SetLocalTime(new DateTime(2000, 1, 1, 1, 1, 1));  //For tests, to see what happens when wrong date

            if (DateTime.Now < new DateTime(2016, 7, 1))
            {
                timeIsSet = false;
                Microsoft.SPOT.Hardware.PowerState.RebootDevice(false);  // Reboot the Mainboard
            }
            else
            {
                Debug.Print("Could get Time from Internet or RealTime Clock");
                timeIsSet = true;
            }
        }
        #endregion

        #region TimeService Events
        static void FixedTimeService_SystemTimeChanged(object sender, SystemTimeChangedEventArgs e)
        {
#if DebugPrint
                    Debug.Print("\r\nSystem Time was set");
#endif

#if SD_Card_Logging
                    if (SdLoggerService != null)
                    {
                        var source = new LogContent() { logOrigin = "n", logReason = "System Time was set", logPosition = "n", logNumber = 1 };
                        SdLoggerService.LogEventHourly("Event", source);
                    }
#endif


        }

        static void FixedTimeService_SystemTimeChecked(object sender, SystemTimeChangedEventArgs e)
        {
#if DebugPrint
                Debug.Print("\r\nSystem Time was checked");
#endif

#if SD_Card_Logging
                if (SdLoggerService != null)
                {
                    var source = new LogContent() { logOrigin = "n", logReason = "System Time was checked", logPosition = "n", logNumber = 1 };
                    SdLoggerService.LogEventHourly("Event", source);
                }
#endif

        }
        #endregion

        #region _Print_Debug
        private static void _Print_Debug(string message)
        {
            //lock (theLock1)
            //{
            switch (_AzureDebugMode)
            {
                //Do nothing             
                case AzureStorageHelper.DebugMode.NoDebug:
                    break;

                //Output Debugging info to the serial port
                case AzureStorageHelper.DebugMode.SerialDebug:

                    //Convert the message to bytes
                    /*
                    byte[] message_buffer = System.Text.Encoding.UTF8.GetBytes(message);
                    _debug_port.Write(message_buffer,0,message_buffer.Length);
                    */
                    break;

                //Print message to the standard debug output
                case AzureStorageHelper.DebugMode.StandardDebug:
#if DebugPrint
                        Debug.Print(message);
#endif
                    break;
            }
            //}
        }
        #endregion

        #region Event myAzureSendManager_AzureCommandSend (Callback indicating e.g. that the Current entity was sent
        static void myAzureSendManager_AzureCommandSend(AzureSendManager sender, AzureSendManager.AzureSendEventArgs e)
        {
            try { GHI.Processor.Watchdog.ResetCounter(); }
            catch { };

            if (e.decrementThreadCounter && (_azureSendThreads > 0))
            { _azureSendThreads--; }

            if (e.azureCommandWasSent)
            {
                _Print_Debug("Row was sent");
                _Print_Debug("Count of AzureSendThreads = " + _azureSendThreads);
                if ((e.returnCode == HttpStatusCode.Created) || (e.returnCode == HttpStatusCode.NoContent))
                {
#if SD_Card_Logging
                        var source_1 = new LogContent() { logOrigin = "Event: Azure command sent", logReason = "o.k.", logPosition = "HttpStatusCode: " + e.returnCode.ToString(), logNumber = e.Code };
                        SdLoggerService.LogEventHourly("Normal", source_1);
#endif
                    waitForCurrentCallback.Set();
                    _azureSends++;
                }
                else
                {
#if SD_Card_Logging
                        var source_2 = new LogContent() { logOrigin = "Event: Azure command sent", logReason = "n", logPosition = "Bad HttpStatusCode: " + e.returnCode.ToString(), logNumber = e.Code };
                        SdLoggerService.LogEventHourly("Error", source_2);
#endif
                    _azureSendErrors++;
                }
            }
            else
            {
#if SD_Card_Logging
                    LogContent source_3 = null;
                    switch (e.Code)
                    {
                        case 7:
                            {
                                source_3 = new LogContent() { logOrigin = "Event: Azure command sent", logReason = "No Connection", logPosition = "HttpStatusCode ambiguous: " + e.returnCode.ToString(), logNumber = e.Code };
                                break;
                            }
                        case 8:
                            {
                                source_3 = new LogContent() { logOrigin = "Event: Azure command sent", logReason = "one try failed", logPosition = "HttpStatusCode ambiguous: " + e.returnCode.ToString(), logNumber = e.Code };
                                break;
                            }
                        case 2:
                            {
                                source_3 = new LogContent() { logOrigin = "Event: Azure command sent", logReason = "Object to early", logPosition = "HttpStatusCode ambiguous: " + e.returnCode.ToString(), logNumber = e.Code };
                                break;
                            }
                        case 1:
                            {
                                var source_4 = new LogContent() { logOrigin = "Event: Azure command sent", logReason = "Buffer was empty", logPosition = "HttpStatusCode ambiguous: " + e.returnCode.ToString(), logNumber = e.Code };
                                SdLoggerService.LogEventHourly("Normal", source_4);
                                break;
                            }
                        case 9:
                            {
                                source_3 = new LogContent() { logOrigin = "Event: Azure command sent", logReason = "3 tries failed", logPosition = "HttpStatusCode ambiguous: " + e.returnCode.ToString(), logNumber = e.Code };
                                break;
                            }
                        case 5:
                            {
                                source_3 = new LogContent() { logOrigin = "Event: Azure command sent", logReason = "Failed to create Table", logPosition = "HttpStatusCode ambiguous: " + e.returnCode.ToString(), logNumber = e.Code };
                                break;
                            }
                        default:
                            { }
                            break;
                    }
                    if (source_3 != null)
                    {
                        SdLoggerService.LogEventHourly("Error", source_3);
                    }
#endif

                _Print_Debug("Count of AzureSendThreads = " + _azureSendThreads);
            }

            Debug.Print("AsyncCallback from Rfm69 Current Data send Thread: " + e.Message);

#if SD_Card_Logging
                var source = new LogContent() { logOrigin = "Event: Azure command sent", logReason = "n", logPosition = "End of method. Count of Threads = " + _azureSendThreads, logNumber = 2 };
                SdLoggerService.LogEventHourly("Normal", source);
#endif
        }
        #endregion

    }
}
