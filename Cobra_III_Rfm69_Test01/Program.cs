// Cobra_III_Rfm69_Test01 Program Copyright RoSchmi 2022 License Apache 2.0,  Version 1.1.0 vom 13.08.2022, 
// NETMF 4.3, GHI SDK 2016 R1
// Hardware: GHI Cobra III Mainboard, Enc28 Ethernet module 
// Dieses Programm dient zur Registrierung der gemessenen Stromwerte eines Smartmeters Eastron SDM530 bzw. SDM630
// sowie der Messung von Temperaturen und der relativen Luftfeutigkeit
//
// Zur gesicherten Datenübertragung via https an Azure muss 'Azure_useHTTPS' auf true gesetzt werden.
// Außerdem muss mittels des Programms MFDeploy die ssl seed des Boards einmalig gesetzt werden




#define DebugPrint


#region Region Using directives
using System;
using System.Threading;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Ports;
using System.Security.Cryptography.X509Certificates;
using System.Collections;
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
using RoSchmi.Net.SparkPost;
using RoSchmi.Net.Divers;
//using RoSchmi.Logger
//using SparkPostEmailTemplates;
using Osre.Modbus;
using Osre.Modbus.Interface;
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

            //private static TimeSpan sendInterval = new TimeSpan(0, 10, 0); // If this time interval has expired since the last sending to azure,
            //private static TimeSpan sendInterval = new TimeSpan(0, 0, 30); // If this time interval has expired since the last sending to azure
            // the acutal entry in the SampleValueBuffer is sent to azure, otherwise it is neglected
        
            //private static TimeSpan sendInterval_Burner = new TimeSpan(0, 0, 1); // If this time interval has expired since the last sending to azure,         
            //private static TimeSpan sendInterval_Boiler = new TimeSpan(0, 0, 1);
            //private static TimeSpan sendInterval_Solar = new TimeSpan(0, 0, 1);
            private static TimeSpan sendInterval_Current = new TimeSpan(0, 0, 1);
            private static TimeSpan sendInterval_Froggit = new TimeSpan(0, 10, 0);

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

            #region Settings concerning parameters of the ALL3075v3 switchable power socket

            private static bool _switchingOfPowerSocketIsActivated = false;
            private static bool _readingPowerSocketSensorIsActivated = false;
            //*****************    Set temperatur threshold    **********************************
            private static double switchOnTemperaturCelsius = 1.00;  // When temperature is lower, then the power outlet is switched on
            // These values are limited through limits defined in the fields region

            private static double switchHysteresis = 2.00;           // When temperature is higher than switchOnTemperaturCelsius + hysteresis,
            // then the power outlet is switched off

            //************************************************************************************


            // Ip-Address or name of the ALL3075V3 power socket in the local network
            // For https it seems to work only with the IP-Address, not with the name
            //private const string _All3075V3_01_AccountName = "192.168.1.100";
            private const string _All3075V3_01_AccountName = "ALL3075v3";
            private static readonly string _All3075V3_01_User = "RoSchmi";             // User Name
            private static readonly string _All3075V3_01_PassWord = "myPassWord";      // Password
            private static readonly bool _All3075V3_01_useHTTPS = false;

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

            // if augmentPartitionKey == true, the actual year and month are added, e.g. Y_2016_07
            private static bool augmentPartitionKey = true;
            
            


            //private static string _tablePreFix_Burner = "Brenner";          // Preset, comes in the sensor eventhandler
            //private static string _tablePreFix_Boiler = "BoilerHeizung";    // Preset, comes in the sensor eventhandle
            private static string _tablePreFix_Solar = "Solar";             // Preset, comes in the sensor eventhandle
            private const string _tablePreFix_Current = "Current";         // Preset, comes in the sensor eventhandle
            private const string _tablePreFix_Froggit = "TempHum";         // Preset, comes in the sensor eventhandle
            //private const string _tablePreFix_SolarTemps = "SolarTemps";    // Preset, comes in the sensor eventhandle

      
         
            private const string _tablePreFix_Definition_3 = "SolarTemps";   // Preset, doesn't come in the sensor eventhandle



            // Preset for the partitionKey of the table entities.        
            //private const string _partitionKeyPrefix_Burner = "Y3_";   // Preset, comes in the sensor eventhandler
            //private const string _partitionKeyPrefix_Boiler = "Y3_";
            private const string _partitionKeyPrefix_Solar = "Y3_";
            private const string _partitionKey_Current = "Y2_";
            private const string _partitionKeyPrefix_Froggit = "Y3_";

        
            //private const string _location_Burner = "Heizung";              // Preset, can be replaced with a value received in the sensor eventhandler
            //private const string _location_Boiler = "Heizung";
            private const string _location_Solar = "Heizung";
            private const string _location_Current = "Keller";
            private const string _location_Froggit = "Keller";

     

            //private const string _sensorValueHeader_Burner = "OnOff";
            //private const string _sensorValueHeader_Boiler = "OnOff";
            private const string _sensorValueHeader_Solar = "OnOff";
            private const string _sensorValueHeader_Current = "T_0";
            private const string _sensorValueHeader_Froggit = "Temps";
            //private const string _sensorValueHeader_SolarTemps = "Coll";



            //private const string _socketSensorHeader_Burner = "";  // (not used in this App)
            //private const string _socketSensorHeader_Boiler = "";  // (not used in this App)
            //private const string _socketSensorHeader_Solar = "";  // (not used in this App)
            private const string _socketSensorHeader_Current = "Current";
            private const string _socketSensorHeader_Froggit = "NU";
            //private const string _socketSensorHeader_SolarTemps = "ST";
           
            
            #endregion

    static bool _useRF_433_Receiver = false;       //if true the NoName RF_433_Receiver is used as the "Priority Sensor"

    #region Setting concerning your SparkPost E-Mail Account

    private static string EmailRecipient_1_Sender = @"Dr.Roland.Schmidt@t-online.de";
    private static string EmailRecipient_1_Recipient = @"Dr-Roland-Schmidt@t-online.de";
    private static string EmailRecipient_1_Name = "Roland-Schmidt";

    private static string EmailRecipient_2_Sender = @"Dr.Roland.Schmidt@t-online.de";
    private static string EmailRecipient_2_Recipient = @"Dr.Roland.Schmidt@t-online.de";
    private static string EmailRecipient_2_Name = "Roland.Schmidt";


    private static bool SendingEmailsViaSparkPostIsActivated = true;
    private static double UpperValueThreshold = 32.0;
    private static double LowerValueThreshold = -1.0;
    private static double ThresholdHysteresis = 0.5;    // Only if the Threshold + Hysteresis is exceeded an e-mail is sent for new exceeding of threshold

    private static TimeSpan EmailSuppressTimePeriod = new TimeSpan(0, 20, 0);   // When an e-mail was sent, new e-mails are suppressed for this time


    private static int heartBeatTimePeriod_Days = 7;        // In this period e-mails are sent to show that the program is working (e.g. once a week)
    private static int heartBeatDayOfWeek = 4;              // Day of the week, where the first heartBeat e-mail is sent: Sunday = 0, Monday = 1, ...... Saturday = 6
    private static double heartBeatHourOfDay = 0.0;        // Hour of the day where the first heartbeat e-mail is sent: 0.0 - 23.0
    // The date and time of the following messages depends on the value of heartBeatTimePeriod_Days


    private const string SparkPostAPIKey = "801eb37f0f11b7b9a090af719875e6f063799194";
    private const string SparkPostBaseURI = "https://api.sparkpost.com/api/v1/";

    private const string SparkPostAcount = "api.sparkpost.com/api/v1/";

    // choose wheter http or https shall be used (with SparkPost only https works, http is only for tests)
    private const bool SparkPost_useHTTPS = true;

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

            static Timer _sensorPollingTimer;
            static TimeSpan _sensorPollingTimerInterval = new TimeSpan(0, 0, 30);  // 30 seconds
            // This timer writes to the SampleValueBuffer if new values were polled from sensors

            static TimeSpan makeInvalidTimeSpan = new TimeSpan(0, 3, 0);  // When this timespan has elapsed, old sensor values are set to invalid

            static bool _handleDiscordantValues = false;  // If true: discordant temperature values are eliminated/attenuated for some tim (see code)
            static bool _randomIdSnapping = true;         // if true: only sensor values from the device with the selected RandomId are taken, 0 means all are taken
            static bool _AutoIdSnapping = false;          // if true: the first RandomId is snapped and reliesed when it is missed for 3 periods (yet not working)

           
            #endregion

        #endregion




            #region Fields


            private static string[] ChRandomAutoId = new string[8] { "0", "0", "0", "0", "0", "0", "0", "0" };
            private static int[] ChAutoIdCount = new int[8] { 3, 3, 3, 3, 3, 3, 3, 3 };

            private static string[] ChRandomId = new string[8] { "0", "0", "0", "0", "0", "0", "0", "0" };

            private static string Ch01RandomId = "0";
            private static string Ch02RandomId = "0";

            //Absolute limits for e-mail alert
            private const double UpperValueLimit = 68.0;
            private const double LowerValueLimit = -38.0;
            private const double ThresholdHysteresisLimitLow = 0.1;
            private const double ThresholdHysteresisLimitHigh = 1.0;

            //Absolute limits for switching
            private const double switchOnTemperaturCelsiusLimitLow = -1.0;
            private const double switchOnTemperaturCelsiusLimitHigh = 5.0;
            private const double switchHysteresisLow = 0.1;
            private const double switchHysteresisHigh = 5.0;

            static AutoResetEvent waitForCurrentCallback = new AutoResetEvent(false);
            static AutoResetEvent waitForTempHumCallback = new AutoResetEvent(false);


            public static SDCard SD;
            private static bool _fs_ready = false;

            private static GHI.Networking.EthernetENC28J60 netif;
            private static bool _hasAddress;
            private static bool _available;

            private static readonly object LockProgram = new object();

            private static bool watchDogIsAcitvated = false;// Don't change, choosing is done in the workWithWatchDog variable

            static Thread WatchDogCounterResetThread;

            private static bool _sensorPollingOccured = false;

            private static int _iteration = 0;



            private static Counters _counters = new Counters();

            private static int _azureSends = 1;
            private static int _forcedReboots = 0;
            private static int _badReboots = 0;
            private static int _azureSendErrors = 0;

            private static bool _willRebootAfterNextEvent = false;

            private static DateTime _timeOfLastSend = DateTime.Now;
            private static DateTime _timeOfLastArduinoSensorEvent = DateTime.Now;

            private static string _lastResetCause;

            private static DateTime _heartBeatTime;

            private static ArrayList EmailRecipientList;

            private const double InValidValue = 999.9;

            private static double _dayMin = 1000.00;   //don't change
            private static double _dayMax = -1000.00;  //don't change
            private static double _lastValue = InValidValue;


            private static double[] _lastTemperature = new double[8] { InValidValue, InValidValue, InValidValue, InValidValue, InValidValue, InValidValue, InValidValue, InValidValue };


            // RoSchmi
            //static TimeSpan makeInvalidTimeSpan = new TimeSpan(2, 15, 0);  // When this timespan has elapsed, old sensor values are set to invalid


            private static readonly object MainThreadLock = new object();


            // Regex ^: Begin at start of line; [a-zA-Z0-9]: these chars are allowed; [^<>]: these chars ar not allowd; +$: test for every char in string until end of line
            // Is used to exclude some not allowed characters in the strings for the name of the Azure table and the message entity property
            static Regex _tableRegex = new Regex(@"^[a-zA-Z0-9]+$");
            static Regex _columnRegex = new Regex(@"^[a-zA-Z0-9_]+$");
            static Regex _stringRegex = new Regex(@"^[^<>]+$");

            private static int _azureSendThreads = 0;
            private static int _azureSendThreadResetCounter = 0;
            //private static int _azureSendErrors = 0;

            private static int _azureGetParamsThreads = 0;
            private static bool _couldReadParams = false;



            // Certificate of Azure, included as a Resource
            static byte[] caAzure = Resources.GetBytes(Resources.BinaryResources.DigiCert_Baltimore_Root);
            static byte[] caAzureNeu = Resources.GetBytes(Resources.BinaryResources.DigiCertGlobalRootG2);

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

            private static AzureSendManager_Froggit myAzureSendManager_Froggit;
            private static AzureSendManager myAzureSendManager;
            //private static AzureSendManager_SolarTemps myAzureSendManager_SolarTemps;

            private static AzureParamManager myAzureParamManager;

            private static ModbusRtuInterface _modbusRtuInterface;
            private static ModbusMaster _modbusMaster;

            private static SerialPort _comPort;


            static SensorValue[] _sensorValueArr = new SensorValue[8];
            static SensorValue[] _sensorValueArr_last_1 = new SensorValue[8];
            static SensorValue[] _sensorValueArr_last_2 = new SensorValue[8];

            static SensorValue[] _sensorValueArr_Out = new SensorValue[8];
       




        #endregion
        public static void Main()
        {
           // Debug.Print(Resources.GetString(Resources.StringResources.String1));

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

            #region Instantiate serial port for Modbus Communication

            _comPort = new SerialPort("COM1", 9600, Parity.Even, 8, StopBits.One);
            //_comPort = new SerialPort("COM1", 19200, Parity.Even, 8, StopBits.One);  // Baudrates higher than 19200 did not work


            _comPort.Open();
            _modbusRtuInterface = new ModbusRtuInterface(_comPort);
            _modbusMaster = new ModbusMaster(_modbusRtuInterface);

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
          //caCerts = new X509Certificate[] { new X509Certificate(caAzure) };
          //caCerts = new X509Certificate[] { new X509Certificate(caAzureNeu) };

           caCerts = new X509Certificate[] { new X509Certificate(caAzure), new X509Certificate(caAzureNeu)};

            int timeOutMs = 100000;    // Wait for Timeserver Response
            long startTicks = Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;
            long timeOutTicks = timeOutMs * TimeSpan.TicksPerMillisecond + startTicks;
            while ((!timeIsSet) && (Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks < timeOutTicks))
            {
                Thread.Sleep(100);
            }
            if (!timeIsSet)           // for the case that there was no AddressChanged Event, try to set time
            {
                SetTime(timeZoneOffset, TimeServer_1, TimeServer_2);
            }
            long endTicks = Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;
            if (!timeIsSet)
            {
                Debug.Print("Going to reboot in 20 sec. Have waited for " + (endTicks - startTicks) / TimeSpan.TicksPerMillisecond + " ms.\r\n");
                Thread.Sleep(20000);
                Microsoft.SPOT.Hardware.PowerState.RebootDevice(true, 3000);
                while (true)
                {
                    Thread.Sleep(100);
                }
            }
            else
            {
                Debug.Print("Program continues. Waited for time for " + (endTicks - startTicks) / TimeSpan.TicksPerMillisecond + " ms.\r\n");
                Debug.Print("Got Time from " + (timeServiceIsRunning ? "Internet" : "Hardware RealTimeClock"));
            }


            #region Set some Presets for Azure Table and others

            myCloudStorageAccount = new CloudStorageAccount(myAzureAccount, myAzureKey, useHttps: Azure_useHTTPS);


            //myAzureSendManager = new AzureSendManager(myCloudStorageAccount, timeZoneOffset, dstStart, dstEnd, dstOffset, _tablePreFix_Current, _sensorValueHeader_Current, _socketSensorHeader_Current, caCerts, DateTime.Now, sendInterval_Current, _azureSends, _AzureDebugMode, _AzureDebugLevel, IPAddress.Parse(fiddlerIPAddress), pAttachFiddler: attachFiddler, pFiddlerPort: fiddlerPort, pUseHttps: Azure_useHTTPS);

            //myAzureSendManager = new AzureSendManager(myCloudStorageAccount, timeZoneOffset, dstStart, dstEnd, dstOffset, _tablePreFix, _sensorValueHeader_Current, _socketSensorHeader_Current, caCerts, DateTime.Now, sendInterval_Current, _azureSends, _AzureDebugMode, _AzureDebugLevel, IPAddress.Parse(fiddlerIPAddress), pAttachFiddler: attachFiddler, pFiddlerPort: fiddlerPort, pUseHttps: Azure_useHTTPS);
            myAzureSendManager = new AzureSendManager(myCloudStorageAccount, timeZoneOffset, dstStart, dstEnd, dstOffset, _tablePreFix_Current, _sensorValueHeader_Current, _socketSensorHeader_Current, caCerts, DateTime.Now, sendInterval_Current, _azureSends, _AzureDebugMode, _AzureDebugLevel, IPAddress.Parse(fiddlerIPAddress), pAttachFiddler: attachFiddler, pFiddlerPort: fiddlerPort, pUseHttps: Azure_useHTTPS);
            AzureSendManager.sampleTimeOfLastSent = DateTime.Now.AddDays(-10.0);    // Date in the past
            AzureSendManager.InitializeQueue();
            
            
            myAzureSendManager_Froggit = new AzureSendManager_Froggit(myCloudStorageAccount, timeZoneOffset, dstStart, dstEnd, dstOffset, _tablePreFix_Froggit, _sensorValueHeader_Froggit, _socketSensorHeader_Froggit, caCerts, DateTime.Now, sendInterval_Froggit, _azureSends, _AzureDebugMode, _AzureDebugLevel, IPAddress.Parse(fiddlerIPAddress), pAttachFiddler: attachFiddler, pFiddlerPort: fiddlerPort, pUseHttps: Azure_useHTTPS);
            AzureSendManager_Froggit.sampleTimeOfLastSent = DateTime.Now.AddDays(-10.0);    // Date in the past
            AzureSendManager_Froggit.InitializeQueue();

            /*********************************************/

            //Initialize sensorValueArray (for values coming from connected arduino via modbus)
            for (int i = 0; i < 8; i++)
            {
                _sensorValueArr[i] = new SensorValue(_timeOfLastArduinoSensorEvent, 0, 0, 0, 0, InValidValue, 999, 0x00, false);
            }

            //_sensorPollingTimer = new Timer(new TimerCallback(_sensorPollingTimer_Tick), cls, _sensorPollingTimerInterval, _sensorPollingTimerInterval);
            _sensorPollingTimer = new Timer(new TimerCallback(_sensorPollingTimer_Tick), null, new TimeSpan(0, 0, 2), _sensorPollingTimerInterval);

            /*
            if (DebugMode == AllnetHttpWebRequestHelper.DebugMode.NoDebug)
            {
#if DebugPrint
                Debug.Print("\r\n\r\nTo see more Debug.Print Messages showing the sequence of the programm?\r\n" +
                            "Set the variable DebugMode to .StandardDebug\r\n");
#endif
            }
            */

            /*
            if (_useRF_433_Receiver)
            {
                rf_433_Receiver.BlankTime = new TimeSpan(0, 0, 40);        // interrupt disabled for this time in seconds after valid signal
                // to prevent the interrupt to be driven by noise spikes
            }
            */

#if SD_Card_Logging
            source = new LogContent() { logOrigin = "Method Main", logReason = "Rebooted", logPosition = "End of the Main Method", logNumber = 3 };
            SdLoggerService.LogEventHourly("Normal", source);
#endif

            /*******************************************/

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


        //**************************************************************************************************

        #region sensorPollingTimer_Tick - Gets Temperatur samples from the Arduino/Medusa Mini Board 
        static void _sensorPollingTimer_Tick(object o)
        {
            try { GHI.Processor.Watchdog.ResetCounter(); }
            catch { };

            // Set timer interval to a very long value
            _sensorPollingTimer.Change(new TimeSpan(0, 0, 2, 0), new TimeSpan(0, 0, 2, 0));

            try
            {
                ushort[] holdingRegResult;

                try
                {
                    holdingRegResult = _modbusMaster.ReadHoldingRegisters(0x11, 0x0000, 40);
                    Debug.Print("Succeeded to read from Arduino");
                }
                catch
                {
                    _sensorPollingTimer.Change(_sensorPollingTimerInterval, _sensorPollingTimerInterval);
                    Debug.Print(" to read from Arduino");

                    return;
                }
                for (int i = 0; i < 8; i++)
                {
                    //holdingRegResult[i * 5 + 1] = 0x8000;
                    UInt32 newSampleTime = (UInt32)(holdingRegResult[i * 5 + 1] << 16) + holdingRegResult[i * 5 + 2];
                    bool batteryIsLow = (newSampleTime & 0x80000000) != 0;
                    newSampleTime = newSampleTime & 0x7FFFFFF;

                    if (_sensorValueArr[i].SampleTime != newSampleTime)
                    {
                        //_sensorValueArr[i] = new SensorValue(DateTime.Now, (byte)(holdingRegResult[i * 5] >> 8), (byte)(holdingRegResult[i * 5] & 0x00FF), newSampleTime, holdingRegResult[i * 5 + 3], holdingRegResult[i * 5 + 4]);                           
                        try
                        {
                            _sensorValueArr_last_2[i] = new SensorValue(_sensorValueArr_last_1[i].LastNetmfTime, _sensorValueArr_last_1[i].Channel, _sensorValueArr_last_1[i].SensorId, _sensorValueArr_last_1[i].SampleTime, _sensorValueArr_last_1[i].Temp, _sensorValueArr_last_1[i].TempDouble, _sensorValueArr_last_1[i].Hum, _sensorValueArr_last_1[i].RandomId, _sensorValueArr[i].BatteryIsLow);
                        }
                        catch { };
                        try
                        {
                            _sensorValueArr_last_1[i] = new SensorValue(_sensorValueArr[i].LastNetmfTime, _sensorValueArr[i].Channel, _sensorValueArr[i].SensorId, _sensorValueArr[i].SampleTime, _sensorValueArr[i].Temp, _sensorValueArr[i].TempDouble, _sensorValueArr[i].Hum, _sensorValueArr[i].RandomId, _sensorValueArr[i].BatteryIsLow);
                        }
                        catch { };

                        //_sensorValueArr[i] = new SensorValue(DateTime.Now, (byte)(holdingRegResult[i * 5] >> 8), (byte)(holdingRegResult[i * 5] & 0x00FF), newSampleTime, holdingRegResult[i * 5 + 3], (System.Math.Round((holdingRegResult[i * 5 + 3] - 720) * 0.556)) / 10, holdingRegResult[i * 5 + 4]);
                        //_sensorValueArr[i] = new SensorValue(DateTime.Now, (byte)(holdingRegResult[i * 5] >> 8), (byte)(holdingRegResult[i * 5] & 0x00FF), newSampleTime, holdingRegResult[i * 5 + 3], (System.Math.Round((holdingRegResult[i * 5 + 3] - 720) * 0.556)) / 10, (ushort)(holdingRegResult[i * 5 + 4] & 0x00FF), (byte)(holdingRegResult[i * 5 + 4] >> 8));


                        _sensorValueArr[i] = new SensorValue(DateTime.Now, (byte)(holdingRegResult[i * 5] >> 8), (byte)(holdingRegResult[i * 5] & 0x00FF), newSampleTime, holdingRegResult[i * 5 + 3], (System.Math.Round((holdingRegResult[i * 5 + 3] - 720) * 0.556)) / 10, (ushort)(holdingRegResult[i * 5 + 4] & 0x00FF), (byte)(holdingRegResult[i * 5 + 4] >> 8), batteryIsLow);

                    }

                    if (_randomIdSnapping)
                    {
                        if (ChRandomId[i] != "0" && (_sensorValueArr[i].RandomId.ToString() != ChRandomId[i]))   // retrieve last values
                        {
                            _sensorValueArr[i] = new SensorValue(_sensorValueArr_last_1[i].LastNetmfTime, _sensorValueArr_last_1[i].Channel, _sensorValueArr_last_1[i].SensorId, _sensorValueArr_last_1[i].SampleTime, _sensorValueArr_last_1[i].Temp, _sensorValueArr_last_1[i].TempDouble, _sensorValueArr_last_1[i].Hum, _sensorValueArr_last_1[i].RandomId, _sensorValueArr[i].BatteryIsLow);
                        }
                    }

                    // Make a copy of the last values
                    try
                    {
                        _sensorValueArr_Out[i] = new SensorValue(_sensorValueArr[i].LastNetmfTime, _sensorValueArr[i].Channel, _sensorValueArr[i].SensorId, _sensorValueArr[i].SampleTime, _sensorValueArr[i].Temp, _sensorValueArr[i].TempDouble, _sensorValueArr[i].Hum, _sensorValueArr[i].RandomId, _sensorValueArr[i].BatteryIsLow);
                    }
                    catch
                    {
                        _sensorValueArr_Out[i] = new SensorValue(DateTime.Now, 0, 0, 0, 2518, InValidValue, 999, 0, false);
                    }
#if DebugPrint
                        //Debug.Print("Humidity is: " + _sensorValueArr_Out[i].Hum.ToString() + "  Random-Id is: " + _sensorValueArr_Out[i].RandomId.ToString());
                        //Debug.Print("Battery Status is low: " + _sensorValueArr_Out[i].BatteryIsLow.ToString());
#endif

                    #region _handleDiscordant values
                    /*
                        // To eliminate discordant values. When there is a deviation of more than 3 °C from the mean of the both last values
                        // the last value is hold as the result until the last three values are close together
                        if (_handleDiscordantValues)
                        {
                            if ((31 < _sensorValueArr[i].Temp && _sensorValueArr[i].Temp < 1939) && (31 < _sensorValueArr_last_1[i].Temp && _sensorValueArr_last_1[i].Temp < 1939) && (31 < _sensorValueArr_last_2[i].Temp && _sensorValueArr_last_2[i].Temp < 1939))  // Only if all values are in the allowed range
                            {
                                //int TempLast_2 = (int)_sensorValueArr_last_2[i].Temp;
                                //int TempLast_1 = (int)_sensorValueArr_last_1[i].Temp;
                                //int TempAct = (int)_sensorValueArr[i].Temp;
                                //int AbsDiff = System.Math.Abs(((TempLast_2 + TempLast_1) / 2) - TempAct);

                                //if (AbsDiff > 60)
                                if (System.Math.Abs((((int)_sensorValueArr_last_2[i].Temp + (int)_sensorValueArr_last_1[i].Temp) / 2) - (int)_sensorValueArr[i].Temp) > 60)  // Difference may be not more than 60 ( = 3 °C)
                                {
                                    _sensorValueArr_Out[i].Temp = _samplHoldValues[i].Temp;
                                    _sensorValueArr_Out[i].Hum = _samplHoldValues[i].Humid;
                                }
                                else
                                {
                                    _samplHoldValues[i] = new SampleHoldValue(_sensorValueArr[i].Temp, _sensorValueArr[i].Hum);
                                }
                            }
                            else
                            {
                                _samplHoldValues[i] = new SampleHoldValue(_sensorValueArr[i].Temp, _sensorValueArr[i].Hum);
                            }
                        }
                        */
                    #endregion


                    // If there was no acutalization in a certain timespan, the sensor values are set to invalid
                    //if (DateTime.Now - _sensorValueArr[i].LastNetmfTime > makeInvalidTimeSpan)
                    // RoSchmi
                    //if (DateTime.Now - _sensorValueArr[i].LastNetmfTime > (makeInvalidTimeSpan < sendInterval ? makeInvalidTimeSpan : sendInterval))
                    if (DateTime.Now - _sensorValueArr[i].LastNetmfTime > (makeInvalidTimeSpan < sendInterval_Froggit ? makeInvalidTimeSpan : sendInterval_Froggit))
                    {

                        _sensorValueArr[i].Temp = 2518;
                        _sensorValueArr[i].TempDouble = InValidValue;
                        _sensorValueArr[i].Hum = 999;
                        //_sensorValueArr[i] = new SensorValue(  , _sensorValueArr[i].Channel, _sensorValueArr[i].SensorId, _sensorValueArr[i].SampleTime, 2518, 999.9, 999, _sensorValueArr_Out[i].RandomId, batteryIsLow);
                        _sensorValueArr_Out[i] = new SensorValue(_sensorValueArr[i].LastNetmfTime, _sensorValueArr[i].Channel, _sensorValueArr[i].SensorId, _sensorValueArr_Out[i].SampleTime, _sensorValueArr[i].Temp, _sensorValueArr[i].TempDouble, _sensorValueArr[i].Hum, _sensorValueArr[i].RandomId, _sensorValueArr[i].BatteryIsLow);
                    }
                }     // End of the 'for (int i = 0; i < 8; i++)' loop
            }
            catch (Exception ex)
            {

                Debug.Print("Exception: " + ex.Message);
            }


            double theRoundedDecTemp = 700.0; // Presset with a value out of the allowed range
            try
            {
                //theRoundedDecTemp = System.Math.Round((_sensorValueArr[(Ch_1_Sel < 1 ? 1 : Ch_1_Sel > 8 ? 8 : Ch_1_Sel) - 1].Temp - 720) * 0.556);
                theRoundedDecTemp = System.Math.Round((_sensorValueArr_Out[(Ch_1_Sel < 1 ? 1 : Ch_1_Sel > 8 ? 8 : Ch_1_Sel) - 1].Temp - 720) * 0.556);

            }
            catch { }

            //theRoundedDecTemp = 700.0;    // RoSchmi:  must be removed - only for tests

            string theDecTempString = null;

            // faked arbitrary value (must be 29 chars, otherwise exception in the following code)
            char[] theFakeNews = Encoding.UTF8.GetChars(System.Text.Encoding.UTF8.GetBytes("1001001011010000110101001010*"));

            if (!_randomIdSnapping || (ChRandomId[Ch_1_Sel - 1] == "0" || _sensorValueArr[Ch_1_Sel - 1].RandomId.ToString() == ChRandomId[Ch_1_Sel - 1]))
            {
                if ((theRoundedDecTemp < 690.0) && (theRoundedDecTemp > -390))  // Only values in the allowed range are transferred
                {
                    // This is the measured temperature as string with one decimal place 
                    theDecTempString = (theRoundedDecTemp / 10).ToString("f1");

                    // This is the temperature transformed to the value as ecpected by the tempSensor_SignalReceived event
                    int theIntTempValueTimesTwo = ((int)theRoundedDecTemp * 2) & 8191;


                    //var theResult = new SignalReceivedEventArgs(new char[1] { 'C' }, true, theIntTempValueTimesTwo, DateTime.Now.AddMinutes(DayLihtSavingTime.DayLightTimeOffset(dstStart, dstEnd, dstOffset, DateTime.Now, true)), 1, 1, 1, "Y2_", _location, "Temperature", _tablePreFix_Froggit, "000", 0);

                    // _sensorValueHeader_Froggit

                    var theResult = new SignalReceivedEventArgs(theFakeNews, true, theIntTempValueTimesTwo, DateTime.Now.AddMinutes(DayLihtSavingTime.DayLightTimeOffset(dstStart, dstEnd, dstOffset, DateTime.Now, true)), 1, 1, 1, _partitionKeyPrefix_Froggit, _location_Froggit, _sensorValueHeader_Froggit, _tablePreFix_Froggit, (Ch_1_Sel - 1).ToString(), 0);

                    if (!_useRF_433_Receiver)   // if the RF_433_sensor is used, signals ar not sent
                    {
                        tempSensor_SignalReceived(theResult);
                    }

                }
                else
                {
#if SD_Card_Logging
                        var source = new LogContent() { logOrigin = "sensorPollingTimer_Tick", logReason = "n", logPosition = "Calling AsyncGetParamsFromAzure", logNumber = 1 };
                        SdLoggerService.LogEventHourly("Normal", source);
#endif

                    AsyncGetParamsFromAzure(8000, 1);
                }
            }
            else
            {
                if (DateTime.Now - _sensorValueArr[Ch_1_Sel - 1].LastNetmfTime > makeInvalidTimeSpan)
                {
                    _sensorValueArr[Ch_1_Sel - 1].LastNetmfTime = DateTime.Now;
                    // This is the temperature transformed to the value as ecpected by the tempSensor_SignalReceived event
                    int theIntTempValueTimesTwo = ((int)theRoundedDecTemp * 2) & 8191;
                    var theResult = new SignalReceivedEventArgs(theFakeNews, true, theIntTempValueTimesTwo, DateTime.Now.AddMinutes(DayLihtSavingTime.DayLightTimeOffset(dstStart, dstEnd, dstOffset, DateTime.Now, true)), 1, 1, 1, "Y2_", _location_Froggit, "Temperature", _tablePreFix_Froggit, "000", 0);
                    if (!_useRF_433_Receiver)   // if the RF_433_sensor is used, signals ar not sent
                    {
                        tempSensor_SignalReceived(theResult);
                    }
                }
                else
                {
#if SD_Card_Logging
                        var source = new LogContent() { logOrigin = "sensorPollingTimer_Tick", logReason = "n", logPosition = "Calling AsyncGetParamsFromAzure", logNumber = 2 };
                        SdLoggerService.LogEventHourly("Normal", source);
#endif
                    AsyncGetParamsFromAzure(8000, 2);
                }
            }

            if (theDecTempString != null)
            {
                //Debug.Print("Temperature is: " + theDecTempString);
            }
            else
            {
                //Debug.Print("Temperature is: ???");
            }
            _sensorPollingOccured = true;  //Singals that this event was fired at least one time
            // When everthing is done set timer interval to the original value
            _sensorPollingTimer.Change(_sensorPollingTimerInterval, _sensorPollingTimerInterval);
        }
        #endregion


        #region AsyncGetParamsThread
        private static void AsyncGetParamsFromAzure(int pTimeOutMs, int pSenderId)
        {
#if SD_Card_Logging
            var source = new LogContent() { logOrigin = "AsyncGetParamsFromAzure", logReason = "n", logPosition = "Start of method. Count of Threads = " + _azureGetParamsThreads.ToString(), logNumber = pSenderId };
            SdLoggerService.LogEventHourly("Normal", source);
#endif

            lock (LockProgram)
            {
                _azureGetParamsThreads++;
                _couldReadParams = false;
            }



            int timeOutMs = pTimeOutMs;
            long startTicks = Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;
            long timeOutTicks = timeOutMs * TimeSpan.TicksPerMillisecond + startTicks;
            bool ReadingOfParametersFinished = false;

            // Start new thread to read parameters
            Thread getParamsThread = new Thread(new ThreadStart(runGetParamsThread));
            getParamsThread.Start();

            do
            {
                Thread.Sleep(20);
                lock (LockProgram)
                {
                    if (_couldReadParams)
                    { ReadingOfParametersFinished = true; }
                }
            } while (!ReadingOfParametersFinished && (Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks < timeOutTicks));
            long endTicks = Microsoft.SPOT.Hardware.Utility.GetMachineTime().Ticks;

            if (endTicks > timeOutTicks)
            {
#if SD_Card_Logging
                    source = new LogContent() { logOrigin = "Thread runGetParamsFromAzure", logReason = "Timeout exceeded", logPosition = "Timeout exceeded", logNumber = pSenderId };
                    SdLoggerService.LogEventHourly("Error", source);
#endif

                //Debug.Print("Without success waited for " + (endTicks - startTicks) / TimeSpan.TicksPerMillisecond + " ms.\r\n");

                lock (LockProgram)
                {
                    if (_azureGetParamsThreads > 3)
                    {
#if SD_Card_Logging
                            source = new LogContent() { logOrigin = "Thread runGetParamsFromAzure", logReason = "n", logPosition = "More than 3 threads --> Reboot", logNumber = pSenderId};
                            SdLoggerService.LogEventHourly("Error", source);
#endif

                        Microsoft.SPOT.Hardware.PowerState.RebootDevice(true, 3000);
                        while (true)
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
            }
            else
            {
                //Debug.Print("Program goes on. Waited for  " + (endTicks - startTicks) / TimeSpan.TicksPerMillisecond + " ms.\r\n");
            }
        }


        private static void runGetParamsThread()
        {

#if SD_Card_Logging
                var source = new LogContent() { logOrigin = "Thread runGetParams", logReason = "n", logPosition = "Start of thread", logNumber = 1 };
                SdLoggerService.LogEventHourly("Normal", source);
#endif

            HttpStatusCode createTableReturnCode = HttpStatusCode.Ambiguous;
            HttpStatusCode queryEntityReturnCode = HttpStatusCode.Ambiguous;
            HttpStatusCode insertEntityReturnCode = HttpStatusCode.Ambiguous;
            ArrayList queryArrayList = new ArrayList();
            AzureParamManager myAzureParamManager = new AzureParamManager(myCloudStorageAccount, _tablePreFix_Froggit, caCerts, _AzureDebugMode, _AzureDebugLevel, IPAddress.Parse(fiddlerIPAddress), pAttachFiddler: attachFiddler, pFiddlerPort: fiddlerPort, pUseHttps: Azure_useHTTPS);
            try { GHI.Processor.Watchdog.ResetCounter(); }
            catch { };
            for (int i = 0; i < 3; i++)
            {

                queryEntityReturnCode = myAzureParamManager.queryTableEntities("$top=1", out queryArrayList);

                if ((queryEntityReturnCode != HttpStatusCode.OK) || (queryArrayList.Count == 0))
                {
                    for (int c = 0; c < 3; c++)
                    {

                        createTableReturnCode = myAzureParamManager.createTable(myCloudStorageAccount, _tablePreFix_Froggit + "Params");

                        //if ((createTableReturnCode == HttpStatusCode.NoContent) || (createTableReturnCode == HttpStatusCode.Conflict))
                        if (createTableReturnCode == HttpStatusCode.NoContent)
                        {
                            #region Create an ArrayList  to hold the properties of the entity

                            // Now we create an Arraylist to hold the properties of a Params table Row,
                            // write these items to an entity
                            // and send this entity to the Cloud

                            ArrayList propertiesAL = new System.Collections.ArrayList();
                            TableEntityProperty property;

                            //Add properties to ArrayList (Name, Value, Type)
                            property = new TableEntityProperty("AlertLevelLow", LowerValueThreshold.ToString("f1"), "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("AlertLevelHigh", UpperValueThreshold.ToString("f1"), "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("AlertHysteresis", ThresholdHysteresis.ToString("f1"), "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("Channel01", Ch_1_Sel.ToString("D1"), "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("Ch01RandomId", Ch01RandomId, "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("Channel02", Ch_2_Sel.ToString("D1"), "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("Ch02RandomId", Ch02RandomId, "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            //property = new TableEntityProperty("Interval", (sendInterval.Days * 1440 + sendInterval.Hours * 60 + sendInterval.Minutes).ToString(), "Edm.String");
                            property = new TableEntityProperty("Interval", (sendInterval_Froggit.Days * 1440 + sendInterval_Froggit.Hours * 60 + sendInterval_Froggit.Minutes).ToString(), "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("SwitchLevelLow", switchOnTemperaturCelsius.ToString("f1"), "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("SwitchHysteresis", switchHysteresis.ToString("f1"), "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("Email_01_Sender", EmailRecipient_1_Sender, "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("Email_01_Recipient", EmailRecipient_1_Recipient, "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("Email_01_Name", EmailRecipient_1_Name, "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("Email_02_Sender", EmailRecipient_2_Sender, "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("Email_02_Recipient", EmailRecipient_2_Recipient, "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            property = new TableEntityProperty("Email_02_Name", EmailRecipient_2_Name, "Edm.String");
                            propertiesAL.Add(makePropertyArray.result(property));
                            //Thread.Sleep(1100);
                            #endregion
                            DateTime actDate = DateTime.Now;

                            //calculate reverse Date, so the last entity can be retrieved with the Azure $top1 query
                            string reverseDate = (10000 - actDate.Year).ToString("D4") + (12 - actDate.Month).ToString("D2") + (31 - actDate.Day).ToString("D2")
                                + (23 - actDate.Hour).ToString("D2") + (59 - actDate.Minute).ToString("D2") + (59 - actDate.Second).ToString("D2");

                            ParamEntity myParamEntity = new ParamEntity(_tablePreFix_Froggit + "Params", reverseDate, propertiesAL);
                            string insertEtag = null;

                            insertEntityReturnCode = myAzureParamManager.insertTableEntity(myCloudStorageAccount, _tablePreFix_Froggit + "Params", myParamEntity, out insertEtag);
#if DebugPrint
                                Debug.Print("Standard Parameters were written to Azure");
#endif

                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            if ((queryArrayList != null) && (queryArrayList.Count != 0))
            {

                var entityHashtable = queryArrayList[0] as Hashtable;
                if (entityHashtable["PartitionKey"].ToString() == _tablePreFix_Froggit + "Params")
                {
                    lock (LockProgram)
                    {
                        _couldReadParams = true;
                        try
                        {
                            var result = double.Parse(entityHashtable["AlertLevelLow"].ToString());
                            if ((result >= LowerValueLimit) && (result <= UpperValueLimit - 1.0))
                            {
                                LowerValueThreshold = result;
                            }
                        }
                        catch { }
                        try
                        {
                            var result = System.Math.Round(double.Parse(entityHashtable["AlertLevelHigh"].ToString()) * 10) / 10; ;
                            if ((result <= UpperValueLimit) && (result > LowerValueThreshold))
                            {
                                UpperValueThreshold = result;
                            }
                            else
                            {
                                UpperValueThreshold = LowerValueThreshold + 0.1;
                            }
                        }
                        catch
                        {
                            UpperValueThreshold = LowerValueThreshold + 0.1;
                        }
                        try
                        {
                            var result = System.Math.Round(double.Parse(entityHashtable["AlertHysteresis"].ToString()) * 10) / 10;
                            if ((result >= ThresholdHysteresisLimitLow) && (result <= ThresholdHysteresisLimitHigh))
                            {
                                ThresholdHysteresis = result;
                            }
                            else
                            {
                                ThresholdHysteresis = 0.2;
                            }
                        }
                        catch
                        {
                            ThresholdHysteresis = 0.2;
                        }
                        try
                        {
                            var result = System.Math.Round(double.Parse(entityHashtable["SwitchLevelLow"].ToString()) * 10) / 10;
                            switchOnTemperaturCelsius = (result < switchOnTemperaturCelsiusLimitLow) ? switchOnTemperaturCelsiusLimitLow : (result > switchOnTemperaturCelsiusLimitHigh) ? switchOnTemperaturCelsiusLimitHigh : result;
                        }
                        catch { }
                        try
                        {
                            var result = System.Math.Round(double.Parse(entityHashtable["SwitchHysteresis"].ToString()) * 10) / 10;
                            switchHysteresis = (result < switchHysteresisLow) ? switchHysteresisLow : (result > switchHysteresisHigh) ? switchHysteresisHigh : result;
                        }
                        catch
                        {
                            switchHysteresis = switchHysteresisLow;
                        }
                        try
                        {
                            var result = int.Parse(entityHashtable["Channel01"].ToString());
                            Ch_1_Sel = (result < 1) ? 1 : (result > 8) ? 8 : result;
                        }
                        catch
                        {
                            Ch_1_Sel = 1;
                        }
                        try
                        {
                            var result = int.Parse(entityHashtable["Channel02"].ToString());
                            Ch_2_Sel = (result < 1) ? 1 : (result > 8) ? 8 : result;
                        }
                        catch
                        {
                            Ch_2_Sel = 2;
                        }
                        try
                        {

                            var result = int.Parse(entityHashtable["Ch01RandomId"].ToString());
                            Ch01RandomId = (result < 0) ? "0" : (result > 255) ? "0" : result.ToString();
                            ChRandomId[Ch_1_Sel - 1] = Ch01RandomId;

                        }
                        catch
                        {
                            Ch01RandomId = "0";
                            ChRandomId[Ch_1_Sel - 1] = Ch01RandomId;
                        }
                        try
                        {
                            var result = int.Parse(entityHashtable["Ch02RandomId"].ToString());
                            Ch02RandomId = (result < 0) ? "0" : (result > 255) ? "0" : result.ToString();
                            ChRandomId[Ch_2_Sel - 1] = Ch02RandomId;
                        }
                        catch
                        {
                            Ch02RandomId = "0";
                            ChRandomId[Ch_1_Sel - 1] = Ch02RandomId;
                        }
                        try
                        {
                            var result = int.Parse(entityHashtable["Interval"].ToString());
                            var result_in_Limits = (result < 2) ? 2 : (result > 10000) ? 10000 : result;
                            var days = result_in_Limits / 1440;
                            var hours = (result_in_Limits - days * 1440) / 60;
                            var minutes = (result_in_Limits - days * 1440) % 60;
                            //sendInterval = new TimeSpan(days, hours, minutes, 0);
                            sendInterval_Froggit = new TimeSpan(days, hours, minutes, 0);
                        }
                        catch
                        {
                            //sendInterval = new TimeSpan(00, 10, 00);
                            sendInterval_Froggit = new TimeSpan(00, 10, 00);

                        }
                        try
                        {
                            EmailRecipient_1_Sender = entityHashtable["Email_01_Sender"].ToString();
                            EmailRecipient_1_Recipient = entityHashtable["Email_01_Recipient"].ToString();
                            EmailRecipient_1_Name = entityHashtable["Email_01_Name"].ToString();
                            EmailRecipient_2_Sender = entityHashtable["Email_02_Sender"].ToString();
                            EmailRecipient_2_Recipient = entityHashtable["Email_02_Recipient"].ToString();
                            EmailRecipient_2_Name = entityHashtable["Email_02_Name"].ToString();
                        }
                        catch { }
                    }
#if DebugPrint
                        Debug.Print("Success: Reading Parameter from Azure");
#endif
                }
                else
                {
#if DebugPrint
                        Debug.Print("Reading PartitionKey from Azure failed");
#endif
#if SD_Card_Logging
                            source = new LogContent() { logOrigin = "Thread runGetParams", logReason = "n", logPosition = "Reading parameter failed", logNumber = 1 };
                            SdLoggerService.LogEventHourly("Failed", source);
#endif
                }

            }
            else
            {
#if DebugPrint
                    Debug.Print("Reading Params from Azure failed");
#endif
#if SD_Card_Logging
                    source = new LogContent() { logOrigin = "Thread runGetParamsFromAzure", logReason = "n", logPosition = "Reading parameter failed", logNumber = 3 };
                    SdLoggerService.LogEventHourly("Failed", source);
#endif
            }
            lock (LockProgram)
            {
                if (_azureGetParamsThreads > 0)
                {
                    _azureGetParamsThreads--;
                }
            }

#if DebugPrint
                Debug.Print("Number of AzureGetParmsThreads = " + _azureGetParamsThreads.ToString());
#endif
#if SD_Card_Logging
                source = new LogContent() { logOrigin = "Thread runGetParams", logReason = "n", logPosition = "Decrement ThreadCounter, threads: " + _azureGetParamsThreads.ToString(), logNumber = 4 };
                SdLoggerService.LogEventHourly("Normal", source);
#endif
        }


        #endregion

        #region Event tempSensor_SignalReceived
        static void tempSensor_SignalReceived(SignalReceivedEventArgs e)
        {
            Debug.Print("Froggit Signal received");

            //RoSchmi
            //return;

            #region basic rf_433_Receiver_SignalReceived eventhandler

            if (!_sensorPollingOccured)
            {
                Debug.Print("Sensor event befor Data read from Arduino, wait for next event");
                return;
            }

            string outString = string.Empty;
           
            string tablePreFix = e.DestinationTable;


            // get minimal and maximal Values
            double dayMaxBefore = AzureSendManager_Froggit._dayMax < 0 ? 0.00 : AzureSendManager_Froggit._dayMax;
            double dayMinBefore = AzureSendManager_Froggit._dayMin > 70 ? 0.00 : AzureSendManager_Froggit._dayMin;



            bool degreeCelsiusSign = true;  // positive value
            double decimalValue = 0;
            string degreeCelsiusString = "???";
            double fahrenheitValue = 0;
            string degreeFahrenheitString = "";
            int measuredValuePlus50 = 0;
            double decimalValuePlus50 = 0; ;
            string bitString = new string(e.receivedData);

#if SD_Card_Logging
                var source = new LogContent() { logOrigin = "Event: RF 433 Signal received", logReason = "n", logPosition = "RF 433 event Start", logNumber = 1 };
                SdLoggerService.LogEventHourly("Normal", source);
#endif

            if (e.signalIsValid == false)       // With the actual driver only valid signals are sent 
            {                                   // but with moifications of the driver it may be useful
                outString = "Corrupted Data: ";
            }

            // Some calculations to transform the measured values which are valid for
            // the range from -50°C to +70°C in a form that can be easily displayed
            // I'm sure there are more elegant methods to do this task

            int measuredValue = e.measuredValue / 2;   // remove last digit, then one bit = 0.1 degree Celcius

            measuredValue = measuredValue & 4095;      // new remove leading 1 s
            //if ((3595 < measuredValue) && (measuredValue < 4096))  // Negative readings are valid from -0.1 to -50 degree Celsius
            if ((3700 < measuredValue) && (measuredValue < 4096))  // Negative readings are valid from -0.1 to -39 degree Celsius
            {
                measuredValue = 4096 - measuredValue;
                degreeCelsiusString = "-";
                degreeCelsiusSign = false;     // sign is "-"
                measuredValuePlus50 = 500 - measuredValue; // // to have only positive values we add 50 degree Celsius
            }                                                 // then -50°C = 0, 0°C = 50, 70°C = 120
            else                                              // now it is easy to compare with certain thresholds
            {
                if ((measuredValue >= 0) && (measuredValue < 701)) // Positive readings are valid from 0 to +70 degree Celsius
                {
                    degreeCelsiusString = "+";
                    degreeCelsiusSign = true;   // sign is "+"
                    measuredValuePlus50 = measuredValue + 500;  // add eqiv. 50 °C
                }
                else
                {
                    degreeCelsiusString = "???";
                }
            }

            if (degreeCelsiusString != "???")
            {
                // calculate celsius value
                //decimalValue = ((double)measuredValue / 10);

                decimalValue = degreeCelsiusSign ? (double)measuredValue / 10 : -(double)measuredValue / 10;

                decimalValuePlus50 = ((double)measuredValuePlus50 / 10);  // by adding 50 degree Celsius the valid range -50 - + 70
                // is now 0 - 120

                degreeCelsiusString = decimalValue.ToString("f1") + " °C";

                // calculate fahrenheit from celsius
                fahrenheitValue = decimalValue * 1.8;
                if (degreeCelsiusSign == false)
                { fahrenheitValue = fahrenheitValue * -1; }
                fahrenheitValue += 32;
                if (fahrenheitValue >= 0)
                {
                    degreeFahrenheitString = "+" + fahrenheitValue.ToString("f1") + " °F";
                }
                else
                {
                    degreeFahrenheitString = fahrenheitValue.ToString("f1") + " °F";
                }
            }
            else
            {
                decimalValue = InValidValue;
            }

#if DebugPrint
            //Debug.Print("Rfm_Froggit event, Data: " + decimalValue.ToString("f2") + " Amps " + t4_decimal_value.ToString("f2") + " Watt " + t5_decimal_value.ToString("f2") + " KWh");
            Debug.Print("Rfm_Froggit event, Data: " + decimalValue.ToString("f2") + " °C ");
#endif


            outString = outString + bitString.Substring(0, 9) + " " + bitString.Substring(9, 15)
                        + " " + bitString.Substring(24, 5) + "  Measured Value: "
                        + degreeCelsiusString + " (+50 = " + decimalValuePlus50.ToString("f1") + ") "
                        + degreeFahrenheitString + "  " + "  Time: "
                        + e.ReadTime.Hour + ":" + e.ReadTime.Minute + ":" + e.ReadTime.Second
                        + "  Repetitions needed: " + e.repCount
                        + "  Failed Bit-Count: " + e.failedBitsCount
                        + "  Eliminated Noise Spikes: " + e.eliminatedSpikesCount;
            _Print_Debug(outString + "\r\n");
            //Debug.Print(outString + "\r\n");

            // ********************    End of the basic rf_433_Receiver_SignalReceived eventhandler   *******************************
            #endregion
#if DebugPrint
            Debug.Print("\r\nReceived reading from sensor: " + degreeCelsiusString + "  " + decimalValue.ToString("f1"));
#endif


            #region get local copy of parameters to avoid issues through access by different threads

            double localLowerValueThreshold = 0.0;
            double localUpperValueThreshold = 0.0;
            double localThresholdHysteresis = 0.0;
            double localswitchOnTemperaturCelsius = 0.0;
            double localswitchHysteresis = 0.0;
            TimeSpan localSendInterval;
            int localCh_1_Sel = 0;
            int localCh_2_Sel = 0;
            string localCh01RandomId = string.Empty;
            string localCh02RandomId = string.Empty;
            string localEmailRecipient_1_Sender = null;
            string localEmailRecipient_1_Recipient = null;
            string localEmailRecipient_1_Name = null;
            string localEmailRecipient_2_Sender = null;
            string localEmailRecipient_2_Recipient = null;
            string localEmailRecipient_2_Name = null;

            lock (LockProgram)
            {
                localLowerValueThreshold = LowerValueThreshold;
                localUpperValueThreshold = UpperValueThreshold;
                localThresholdHysteresis = ThresholdHysteresis;
                localswitchOnTemperaturCelsius = switchOnTemperaturCelsius;
                localswitchHysteresis = switchHysteresis;
                //localSendInterval = new TimeSpan(sendInterval.Days, sendInterval.Hours, sendInterval.Seconds, 0);
                localSendInterval = new TimeSpan(sendInterval_Froggit.Days, sendInterval_Froggit.Hours, sendInterval_Froggit.Seconds, 0);
                localCh_1_Sel = Ch_1_Sel;
                localCh_2_Sel = Ch_2_Sel;
                localCh01RandomId = Ch01RandomId;
                localCh02RandomId = Ch02RandomId;
                localEmailRecipient_1_Sender = EmailRecipient_1_Sender;
                localEmailRecipient_1_Recipient = EmailRecipient_1_Recipient;
                localEmailRecipient_1_Name = EmailRecipient_1_Name;
                localEmailRecipient_2_Sender = EmailRecipient_2_Sender;
                localEmailRecipient_2_Recipient = EmailRecipient_2_Recipient;
                localEmailRecipient_2_Name = EmailRecipient_2_Name;
            }
            #endregion

            EmailRecipientList = new ArrayList();
            EmailRecipientList.Add(new EmailRecipientProperties(localEmailRecipient_1_Sender, localEmailRecipient_1_Recipient, localEmailRecipient_1_Name));
            if ((localEmailRecipient_2_Recipient != null) && (localEmailRecipient_2_Recipient.Length > 2))
            {
                EmailRecipientList.Add(new EmailRecipientProperties(localEmailRecipient_2_Sender, localEmailRecipient_2_Recipient, localEmailRecipient_2_Name));
            }

            // activateWatchdogIfAllowedAndNotYetRunning();


            AsyncGetParamsFromAzure(8000, 3);                      // Get Parameters from Azure (are used on the next send)

            #region Preset some table parameters like Row Headers
            // Here we set some table parameters which where transmitted in the eventhandler and were set in the constructor of the RF_433_Receiver Class

            // RoSchmi
            //string _tablePreFix_Froggit = e.DestinationTable;
           
            //string _partitionKey_Froggit = e.SensorLabel;
            string _location_Froggit = e.SensorLocation;
            string _sensorValueHeader_Froggit = e.MeasuredQuantity;
            string _socketSensorHeader_Froggit = Program._socketSensorHeader_Froggit;


            #endregion



            #region toggles the power socket switch. The code is commented out, used only for tests
            // this toggles the power socket switch (only for tests)
            /*
            if (_iteration % 2 == 0)
            {
                decimalValuePlus50 = 50;
            }
            else
            {
                decimalValuePlus50 = 55;
            }
            */
            #endregion

            DateTime timeOfThisEvent = DateTime.Now;
            AzureSendManager_Froggit._timeOfLastSensorEvent = timeOfThisEvent;


            // Reset _sensorControlTimer, if the timer is not reset, the board will be rebooted
            _sensorControlTimer = new Timer(new TimerCallback(_sensorControlTimer_Tick), null, _sensorControlTimerInterval, _sensorControlTimerInterval);
            // when no sensor events occure in a certain timespan

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


            //_timeOfLastSensorEvent = timeOfThisEvent;    // Refresh the time of the last sensor event so that the _sensorControlTimer will be enabled to react
            // when no sensor events occure in a certain timespan

            #region If allowed: Switch the socket according to meusured temperature and thresholds
            string switchResult = null;
            string switchMessage = "Switch Message Preset";
            string switchState = "???";

            //ALL3075V3_SwitchResponse mySwitchResponse = new ALL3075V3_SwitchResponse();
            if (_switchingOfPowerSocketIsActivated)
            {
                /*
                _Print_Debug("\r\nGoing to switch Power Socket (if needed)");
                lock (LockProgram)
                {
                    mySwitchResponse = switchALL3075V3(myAll3075V3_01_Account, degreeCelsiusString == "???" ? "off" : "TempDependent", _SocketState, decimalValue, localswitchOnTemperaturCelsius, localswitchHysteresis);
                }
                if (mySwitchResponse.Success)
                {
                    switchResult = mySwitchResponse.Result;
                    switchMessage = mySwitchResponse.Message;
                    switchState = mySwitchResponse.State;
                    _SocketState = switchState;
                }
                 */
            }

            #endregion

            // if the power socket was switched to a new state or if an error occured, we force to send the row to Azure
            bool forceSend = ((switchResult != null)) && (switchResult != "nothing todo");

            #region if allowed: Send an E-mail through SparkPost

            double theMeasuredValue = decimalValue;

            /*
            if (decisionToSendEmail(theMeasuredValue, localUpperValueThreshold, localLowerValueThreshold, SendingEmailsViaSparkPostIsActivated,
                                       LastValueExceededThreshold, timeOfLastEmail_LowLevel,
                                       timeOfLastEmail_HighLevel, EmailSuppressTimePeriod))
            {
               
                LastValueExceededThreshold = true;
                string StatusColour = string.Empty;
                if (theMeasuredValue < localLowerValueThreshold)
                {
                    timeOfLastEmail_LowLevel = DateTime.Now;
                    StatusColour = "Blue";
                }
                else
                {
                    timeOfLastEmail_HighLevel = DateTime.Now;
                    StatusColour = "Red";
                }

                SparkPostHttpWebClient mySparkPostWebClient = new SparkPostHttpWebClient(mySparkPostAccount, caCerts, SparkPostHttpWebRequestHelper.DebugMode.StandardDebug, SparkPostHttpWebRequestHelper.DebugLevel.DebugAll);
                mySparkPostWebClient.SparkPostCommandSent += mySparkPostWebClient_SparkPostCommandSent;

                if (attachFiddler)
                {
                    mySparkPostWebClient.attachFiddler(true, IPAddress.Parse(fiddlerIPAddress), fiddlerPort);
                }
                string message = e.SensorLocation + ": " + "Temperature is out of limits";

                //temp_alert myTempAlert = new temp_alert("Temperature is out of limits", StatusColour, theMeasuredValue.ToString("f1"), localLowerValueThreshold.ToString("f1"), localUpperValueThreshold.ToString("f1"));
                temp_alert myTempAlert = new temp_alert("temp-alert", true, "temp-informer", @"Dr.Roland.Schmidt@t-online.de", EmailRecipientList, message, StatusColour, theMeasuredValue.ToString("f1"), localLowerValueThreshold.ToString("f1"), localUpperValueThreshold.ToString("f1"));
                var postData = myTempAlert.ToString();

                if (postData != null)
                {
                    // Since e-mail over Sparkpost doesn't work after the depricated TLS 1.0 E-mail over the power socket is used
                    ALL3075V3_SwitchResponse LEDSwitchResponse = new ALL3075V3_SwitchResponse();
                    LEDSwitchResponse = LEDswitchALL3075V3(myAll3075V3_01_Account, "On");
                    Thread.Sleep(1000);
                    LEDSwitchResponse = LEDswitchALL3075V3(myAll3075V3_01_Account, "Off");
                    Thread.Sleep(100);

                    // mySparkPostWebClient.sendEmail(postData);
                }
                else
                {
#if DebugPrint
                        Debug.Print("\r\nError: No E-mail recipients were specified\r\n");
#endif
                }
            }
            else
            {
                if ((theMeasuredValue < localLowerValueThreshold) || (theMeasuredValue > localUpperValueThreshold))
                {
                    LastValueExceededThreshold = true;
                }
                else
                {
                    if ((theMeasuredValue > localLowerValueThreshold + localThresholdHysteresis) || (theMeasuredValue < localUpperValueThreshold - localThresholdHysteresis))
                    {
                        LastValueExceededThreshold = false;
                    }
                }
               
            }
             */

            #endregion

            #region if allowed: Read Sensor value from power Socket
            string actCurrent = "????";
            if (_readingPowerSocketSensorIsActivated)
            {
                /*
                // Read a value from a Sensor
#if DebugPrint
                    Debug.Print("\r\nGoing to read a sensor of the Power Socket\r\n");
#endif
                if (forceSend)
                { Thread.Sleep(1000); }         // If socket was switched wait for the right current value to get settled before reading

                myAll3075V3_01_Client = new All3075V3HttpWebClient(myAll3075V3_01_Account, caCerts, DebugMode, DebugLevel);
                try
                {
                    try { GHI.Processor.Watchdog.ResetCounter(); }
                    catch { };

                    if (myAll3075V3_01_Client.GetALL3075Sensor(All3075V3HttpWebClient.Sensor.Current) != HttpStatusCode.OK)
                    {
                        throw new Exception("An error occured when trying to get Allnet Infos");
                    }
                    actCurrent = myAll3075V3_01_Client.SensorResult(All3075V3HttpWebClient.Result.current);

                    try     // avoid -0.00
                    {
                        double theDouble = double.Parse(actCurrent);
                        if (theDouble > -0.01)
                        {
                            theDouble = System.Math.Abs(theDouble);
                            actCurrent = theDouble.ToString("f2");
                        }
                    }
                    catch
                    { }
                    // Print some of the values
                    _Print_Debug("Current actual value : " + myAll3075V3_01_Client.SensorResult(All3075V3HttpWebClient.Result.current));


                    _Print_Debug("Todays maximal reading  : " + myAll3075V3_01_Client.SensorResult(All3075V3HttpWebClient.Result.today_max_value)
                        + " at " + myAll3075V3_01_Client.SensorResult(All3075V3HttpWebClient.Result.today_max_date));


                    _Print_Debug("Todays minimal reading  : " + myAll3075V3_01_Client.SensorResult(All3075V3HttpWebClient.Result.today_min_value)
                        + " at " + myAll3075V3_01_Client.SensorResult(All3075V3HttpWebClient.Result.today_min_date));


                    

                }
                catch
                {
#if DebugPrint
                        Debug.Print(myAll3075V3_01_Client.GetErrorMessage());
#endif
                }
                */
            }

            #endregion

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

            RegexTest.ThrowIfNotValid(_tableRegex, new string[] { tablePreFix, _location_Froggit });
            RegexTest.ThrowIfNotValid(_columnRegex, new string[] { _sensorValueHeader_Froggit });

            #endregion

            #region After a reboot: Read the last stored entity from Azure to actualize the counters
            if (AzureSendManager_Froggit._iteration == 0)    // The system has rebooted: We read the last entity from the Cloud
            {
                _counters = myAzureSendManager_Froggit.ActualizeFromLastAzureRow(ref switchMessage);
                _azureSendErrors = _counters.AzureSendErrors > _azureSendErrors ? _counters.AzureSendErrors : _azureSendErrors;
                _azureSends = _counters.AzureSends > _azureSends ? _counters.AzureSends : _azureSends;
                _forcedReboots = _counters.ForcedReboots > _forcedReboots ? _counters.ForcedReboots : _forcedReboots;
                _badReboots = _counters.BadReboots > _badReboots ? _counters.BadReboots : _badReboots;

                /*
                _azureSendErrors = _counters.AzureSendErrors > _azureSendErrors ? _counters.AzureSendErrors : _azureSendErrors;
                _azureSends = _counters.AzureSends > _azureSends ? _counters.AzureSends : _azureSends;
                _forcedReboots = _counters.ForcedReboots > _forcedReboots ? _counters.ForcedReboots : _forcedReboots;
                _badReboots = _counters.BadReboots > _badReboots ? _counters.BadReboots : _badReboots;
                */

                forceSend = true;
                // actualize to consider the timedelay caused by reading from the cloud
                timeOfThisEvent = DateTime.Now;
                AzureSendManager_Froggit._timeOfLastSensorEvent = timeOfThisEvent;

                /*
                QueryLastRow(ref switchMessage);
                forceSend = true;
                Thread.Sleep(3000);
                */
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

            //DateTime copyTimeOfLastSend = AzureSendManager._timeOfLastSend;

            DateTime copyTimeOfLastSend = AzureSendManager_Froggit._timeOfLastSend;

            TimeSpan timeFromLastSend = timeOfThisEvent - copyTimeOfLastSend;

            double daylightCorrectOffset = DayLihtSavingTime.DayLightTimeOffset(dstStart, dstEnd, dstOffset, DateTime.Now, true);


            // TimeSpan timeFromLastSend = timeOfThisEvent - _timeOfLastSend;

            // double daylightCorrectOffset = DayLihtSavingTime.DayLightTimeOffset(dstStart, dstEnd, dstOffset, DateTime.Now, true);

            //TimeSpan timeFromLastSend = timeOfThisEvent.AddMinutes(DayLihtSavingTime.DayLightTimeOffset(dstStart, dstEnd, dstOffset, DateTime.Now, true)) - _timeOfLastSend;

            #region Set the partitionKey
            
            string partitionKey = e.SensorLabel;

            // Set Partition Key for Azure storage table
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
            if ((timeFromLastSend > AzureSendManager_Froggit._sendInterval) || forceSend)
            {
                #region actualize the values of minumum and maximum measurements of the day

                if (AzureSendManager_Froggit._timeOfLastSend.AddMinutes(daylightCorrectOffset).Day == timeOfThisEvent.AddMinutes(daylightCorrectOffset).Day)
                {
                    // same day as event before
                    // RoSchmi
                    AzureSendManager_Froggit._dayMaxWorkBefore = AzureSendManager_Froggit._dayMaxWork;
                    AzureSendManager_Froggit._dayMinWorkBefore = AzureSendManager_Froggit._dayMinWork;
                    AzureSendManager_Froggit._dayMaxSolarWorkBefore = AzureSendManager_Froggit._dayMaxSolarWork;
                    AzureSendManager_Froggit._dayMinSolarWorkBefore = AzureSendManager_Froggit._dayMinSolarWork;

                    Debug.Print(AzureSendManager_Froggit._dayMaxWork.ToString("F4"));
                    Debug.Print(AzureSendManager_Froggit._dayMaxWorkBefore.ToString("F4"));


                    //AzureSendManager_Froggit._dayMaxWork = t5_decimal_value;     // measuredWork
                    AzureSendManager_Froggit._dayMaxWork = 55.5; 

                    if (AzureSendManager_Froggit._dayMinWork < 0.1)
                    {
                        AzureSendManager_Froggit._dayMinWorkBefore = AzureSendManager._dayMinWork;
                        //AzureSendManager_Froggit._dayMinWork = t5_decimal_value;  // measuredWork
                        AzureSendManager_Froggit._dayMinWork = 55.6;  // measuredWork
                    }

                    /*
                    AzureSendManager._dayMaxSolarWork = solarEnergy_decimal_value;     // measuredSolarWork
                    if (AzureSendManager._dayMinSolarWork < 0.1)
                    {
                        AzureSendManager._dayMinSolarWork = solarEnergy_decimal_value;  // measuredSolarWork
                    }
                    */
                    AzureSendManager_Froggit._dayMaxSolarWork = 0.0;
                    AzureSendManager_Froggit._dayMinSolarWork = 0.0;
                }
                else   // not the same day as event before
                {

                    if ((decimalValue > AzureSendManager_Froggit._dayMax) && (decimalValue < 70.0))
                    {
                        AzureSendManager_Froggit._dayMax = decimalValue;
                    }
                    if ((decimalValue > -39.0) && ((decimalValue < AzureSendManager_Froggit._dayMin)) || AzureSendManager_Froggit._dayMin < 0.001)
                    {
                        AzureSendManager_Froggit._dayMin = decimalValue;
                    }
                }

                    /*
                    if (_timeOfLastSend.AddMinutes(daylightCorrectOffset).Day == timeOfThisEvent.AddMinutes(daylightCorrectOffset).Day)
                    {
                        if ((decimalValue > _dayMax) && (decimalValue < 70.0))
                        {
                            _dayMax = decimalValue;
                        }
                        if ((decimalValue > -39.0) && (decimalValue < _dayMin))
                        {
                            _dayMin = decimalValue;
                        }
                    }
                    else
                    {
                        if ((decimalValue > -39.0) && (decimalValue < 70.0))
                            _dayMax = decimalValue;
                        _dayMin = decimalValue;
                    }
                    */
                #endregion

                    _lastValue = decimalValue;

                    //tablePreFix + DateTime.Now.Year

                    AzureSendManager_Froggit._iteration++;

                    SampleValue theRow = new SampleValue(tablePreFix + DateTime.Now.Year, partitionKey, e.ReadTime, timeZoneOffset + (int)daylightCorrectOffset, decimalValue, AzureSendManager_Froggit._dayMin, AzureSendManager_Froggit._dayMax,
                        _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_1_Sel - 1].RandomId, _sensorValueArr_Out[Ch_1_Sel - 1].Hum, _sensorValueArr_Out[Ch_1_Sel - 1].BatteryIsLow,
                        _sensorValueArr_Out[Ch_2_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_2_Sel - 1].RandomId, _sensorValueArr_Out[Ch_2_Sel - 1].Hum, _sensorValueArr_Out[Ch_2_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_3_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_3_Sel - 1].RandomId, _sensorValueArr_Out[Ch_3_Sel - 1].Hum, _sensorValueArr_Out[Ch_3_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_4_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_4_Sel - 1].RandomId, _sensorValueArr_Out[Ch_4_Sel - 1].Hum, _sensorValueArr_Out[Ch_4_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_5_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_5_Sel - 1].RandomId, _sensorValueArr_Out[Ch_5_Sel - 1].Hum, _sensorValueArr_Out[Ch_5_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_6_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_6_Sel - 1].RandomId, _sensorValueArr_Out[Ch_6_Sel - 1].Hum, _sensorValueArr_Out[Ch_6_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_7_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_7_Sel - 1].RandomId, _sensorValueArr_Out[Ch_7_Sel - 1].Hum, _sensorValueArr_Out[Ch_7_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_8_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_8_Sel - 1].RandomId, _sensorValueArr_Out[Ch_8_Sel - 1].Hum, _sensorValueArr_Out[Ch_8_Sel - 1].BatteryIsLow,
                       actCurrent, switchState, _location_Froggit, timeFromLastSend, 0, e.RSSI, AzureSendManager_Froggit._iteration, remainingRam, _forcedReboots, _badReboots, _azureSendErrors, willReboot ? 'X' : '.', forceSend, forceSend ? switchMessage : "");
                    
                    /*
                    SampleValue theRow = new SampleValue(tablePreFix + DateTime.Now.Year, partitionKey, e.ReadTime, timeZoneOffset + (int)daylightCorrectOffset, decimalValue, _dayMin, _dayMax, 
                        _sensorValueArr_Out[Ch_2_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_2_Sel - 1].RandomId, _sensorValueArr_Out[Ch_2_Sel - 1].Hum, _sensorValueArr_Out[Ch_2_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_3_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_3_Sel - 1].RandomId, _sensorValueArr_Out[Ch_3_Sel - 1].Hum, _sensorValueArr_Out[Ch_3_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_4_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_4_Sel - 1].RandomId, _sensorValueArr_Out[Ch_4_Sel - 1].Hum, _sensorValueArr_Out[Ch_4_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_5_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_5_Sel - 1].RandomId, _sensorValueArr_Out[Ch_5_Sel - 1].Hum, _sensorValueArr_Out[Ch_5_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_6_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_6_Sel - 1].RandomId, _sensorValueArr_Out[Ch_6_Sel - 1].Hum, _sensorValueArr_Out[Ch_6_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_7_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_7_Sel - 1].RandomId, _sensorValueArr_Out[Ch_7_Sel - 1].Hum, _sensorValueArr_Out[Ch_7_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_8_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_8_Sel - 1].RandomId, _sensorValueArr_Out[Ch_8_Sel - 1].Hum, _sensorValueArr_Out[Ch_8_Sel - 1].BatteryIsLow,
                       actCurrent, switchState, _location_Froggit, timeFromLastSend, 0, e.RSSI, AzureSendManager_Froggit._iteration, remainingRam, _forcedReboots, _badReboots, _azureSendErrors, willReboot ? 'X' : '.', forceSend, forceSend ? switchMessage : "");
                     */

                    /*
                    SampleValue theRow = new SampleValue(partitionKey, e.ReadTime, timeZoneOffset + (int)daylightCorrectOffset, decimalValue, _dayMin, _dayMax,
                       _sensorValueArr_Out[Ch_1_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_1_Sel - 1].RandomId, _sensorValueArr_Out[Ch_1_Sel - 1].Hum, _sensorValueArr_Out[Ch_1_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_2_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_2_Sel - 1].RandomId, _sensorValueArr_Out[Ch_2_Sel - 1].Hum, _sensorValueArr_Out[Ch_2_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_3_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_3_Sel - 1].RandomId, _sensorValueArr_Out[Ch_3_Sel - 1].Hum, _sensorValueArr_Out[Ch_3_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_4_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_4_Sel - 1].RandomId, _sensorValueArr_Out[Ch_4_Sel - 1].Hum, _sensorValueArr_Out[Ch_4_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_5_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_5_Sel - 1].RandomId, _sensorValueArr_Out[Ch_5_Sel - 1].Hum, _sensorValueArr_Out[Ch_5_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_6_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_6_Sel - 1].RandomId, _sensorValueArr_Out[Ch_6_Sel - 1].Hum, _sensorValueArr_Out[Ch_6_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_7_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_7_Sel - 1].RandomId, _sensorValueArr_Out[Ch_7_Sel - 1].Hum, _sensorValueArr_Out[Ch_7_Sel - 1].BatteryIsLow,
                       _sensorValueArr_Out[Ch_8_Sel - 1].TempDouble, _sensorValueArr_Out[Ch_8_Sel - 1].RandomId, _sensorValueArr_Out[Ch_8_Sel - 1].Hum, _sensorValueArr_Out[Ch_8_Sel - 1].BatteryIsLow,
                       actCurrent, switchState, _location, timeFromLastSend, e.RSSI, _iteration, remainingRam, _forcedReboots, _badReboots, _azureSendErrors, willReboot ? 'X' : '.', forceSend, forceSend ? switchMessage : "");
                    */
                    if (AzureSendManager_Froggit._iteration == 1)
                    {
                        //if (timeFromLastSend < (makeInvalidTimeSpan < sendInterval ? makeInvalidTimeSpan : sendInterval))   // after reboot for the first time take values which were read back from the Cloud
                        if (timeFromLastSend < (makeInvalidTimeSpan < sendInterval_Froggit ? makeInvalidTimeSpan : sendInterval_Froggit))   // after reboot for the first time take values which were read back from the Cloud
                        {
                            //theRow.T_0 = _lastTemperature[Ch_1_Sel - 1];
                            theRow.T_1 = _lastTemperature[Ch_2_Sel - 1];
                            theRow.T_2 = _lastTemperature[Ch_3_Sel - 1];
                            theRow.T_3 = _lastTemperature[Ch_4_Sel - 1];
                            theRow.T_4 = _lastTemperature[Ch_5_Sel - 1];
                            theRow.T_5 = _lastTemperature[Ch_6_Sel - 1];
                            theRow.T_6 = _lastTemperature[Ch_7_Sel - 1];
                            theRow.T_7 = _lastTemperature[Ch_8_Sel - 1];
                        }
                        else
                        {
                            //theRow.T_0 = InValidValue;
                            theRow.T_1 = InValidValue;
                            theRow.T_2 = InValidValue;
                            theRow.T_3 = InValidValue;
                            theRow.T_4 = InValidValue;
                            theRow.T_5 = InValidValue;
                            theRow.T_6 = InValidValue;
                            theRow.T_7 = InValidValue;
                        }
                    }

                    //waitForCurrentCallback.Reset();
                    waitForTempHumCallback.Reset();
                    //waitForCurrentCallback.WaitOne(50000, true);
                    waitForTempHumCallback.WaitOne(50000, true);
                    waitForTempHumCallback.WaitOne(5000, true);

                    //Thread.Sleep(5000); // Wait additional 5 sec for last thread AzureSendManager_Froggit Thread to finish
                    AzureSendManager.EnqueueSampleValue(theRow);

                    if (AzureSendManager_Froggit.hasFreePlaces())
                    {
                        AzureSendManager_Froggit.EnqueueSampleValue(theRow);
                        _timeOfLastSend = timeOfThisEvent;
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
                    if (_azureSendThreads == 0)
                    {

                        _azureSendThreads++;

                        myAzureSendManager_Froggit = new AzureSendManager_Froggit(myCloudStorageAccount, timeZoneOffset, dstStart, dstEnd, dstOffset, _tablePreFix_Froggit, _sensorValueHeader_Froggit, _socketSensorHeader_Froggit, caCerts, DateTime.Now, sendInterval_Froggit, _azureSends, _AzureDebugMode, _AzureDebugLevel, IPAddress.Parse(fiddlerIPAddress), pAttachFiddler: attachFiddler, pFiddlerPort: fiddlerPort, pUseHttps: Azure_useHTTPS);
                        //AzureSendManager_Froggit.sampleTimeOfLastSent = DateTime.Now.AddDays(-10.0);    // Date in the past
                       
                       
                        myAzureSendManager_Froggit.AzureCommandSend += myAzureSendManager_Froggit_AzureCommandSend;

                        try { GHI.Processor.Watchdog.ResetCounter(); }
                        catch { };
                        _Print_Debug("\r\nRow was sent on its way to Azure");
                        myAzureSendManager_Froggit.Start();

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
        


        static void myAzureSendManager_Froggit_AzureCommandSend(AzureSendManager_Froggit sender, AzureSendManager_Froggit.AzureSendEventArgs e)
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
                    //waitForCurrentCallback.Set();

                    waitForTempHumCallback.Set();
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

            Debug.Print("AsyncCallback from Rfm69 Froggit send Thread: " + e.Message);

#if SD_Card_Logging
                var source = new LogContent() { logOrigin = "Event: Azure command sent", logReason = "n", logPosition = "End of method. Count of Threads = " + _azureSendThreads, logNumber = 2 };
                SdLoggerService.LogEventHourly("Normal", source);
#endif

        }
        #endregion


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


        #region Event SolarPumpSolarTempsDataSensor_SignalReceived   (not used in this App)
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
            
            // RoSchmi
            return;

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

            string _partitionKey_Current = e.SensorLabel;
            string _location_Current = e.SensorLocation;
            string _sensorValueHeader_Current = e.MeasuredQuantity;
            
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

                        //Thread.Sleep(5000); // Wait additional 5 sec for last thread AzureSendManager Thread to finish
                        AzureSendManager.EnqueueSampleValue(theRow);

                       
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

        #region Method switchALL3075V3  Supports the commands >On<, >Off< and >TempDependent< (actual temperature is compared with thresholds)
        /// <summary>
        /// Switches the power socket according to the command pSwitchToState that can be "On", "Off" or "TempDependent"
        /// </summary>
        /// <param name="pSwitchToState">The Command: Can be "Off", "On" or "TempDependent". For "Off" or "On" the actual temperature is ignored</param>
        /// <param name="pActTemperatureCelsius">The actually measured temperature in degrees Celsius".</param>
        /// <param name="pSwitchOnTemperaturCelsius">If the acutally measured temperatur is below this value the soccket is switched on".</param>
        /// <param name="pSwitchHysteresis">If the acutally measured temperatur is above (pSwitchOnTemperatureCelsius + pSwitchHysteresis) the socket is switched off".</param>
        
        
        /*
        static ALL3075V3_SwitchResponse switchALL3075V3(AllnetAccount pAccount, string pSwitchToState, string pSocketState, double pActTemperatureCelsius, double pSwitchOnTemperaturCelsius, double pSwitchHysteresis)
        {
            All3075V3HttpWebClient localAll3075V3HttpWebClient;
            double decimalValuePlus50 = pActTemperatureCelsius + 50;
            string switchMessage = string.Empty;
            string switchResult = null;
            string switchState = string.Empty;
            bool success = false;
            bool commandWasNotAllowed = false;

            string switchCommand = pSocketState;  // preset = switch to the state it had before

            switch (pSwitchToState.ToLower())
            {
                case "on":
                    {
                        switchCommand = "1";
                        //decimalValuePlus50 = (switchOnTemperaturCelsius + 50.00 - 1.00); // So it switches always to "On"
                    }
                    break;
                case "off":
                    {
                        switchCommand = "0";
                        //decimalValuePlus50 = switchOnTemperaturCelsius + 50.00 + pSwitchHysteresis + 1.00;  // So it switches always to "Off"
                    }
                    break;
                case "tempdependent":
                    {
                        if (decimalValuePlus50 < (pSwitchOnTemperaturCelsius + 50.00))    // Below lower threshold  --> switch on
                        {
                            switchCommand = "1";
                        }
                        if (decimalValuePlus50 > (pSwitchOnTemperaturCelsius + 50.00 + pSwitchHysteresis))        // Above upper threshold  --> switch off
                        {
                            switchCommand = "0";
                        }
                    }
                    break;
                default:
                    {
                        // For a not allowed command we switch off
                        commandWasNotAllowed = true;
                        switchCommand = "0"; // So it switches always to "Off"
                    }
                    break;
            }

            if (switchCommand == "1")   // ---> Switch on
            {
                localAll3075V3HttpWebClient = new All3075V3HttpWebClient(pAccount, caCerts, DebugMode, DebugLevel);
                try
                {
                    try { GHI.Processor.Watchdog.ResetCounter(); }
                    catch { };
                    if (localAll3075V3HttpWebClient.ALL3075_Switch_On() != HttpStatusCode.OK)
                    {
                        //switchMessage = "An error occured when trying to switch the socket to on";
                        throw new Exception("An error occured when trying to switch the power outlet on");
                    }
                    // Be careful to have no unallowed charcters in the string
                    switchMessage = "Response on Command switch ON: " + localAll3075V3HttpWebClient.SwitchResult("name") + " Result: " + localAll3075V3HttpWebClient.SwitchResult("result_text");
                    switchResult = localAll3075V3HttpWebClient.SwitchResult("result_text");
                    switchState = localAll3075V3HttpWebClient.SwitchResult("result");
                    success = true;
                    _Print_Debug("\r\n" + switchMessage);
                }
                catch
                {
                    switchMessage = "An error occured when trying to switch the socket to on";
                    switchState = "?";
                    success = false;
#if DebugPrint
                        Debug.Print(localAll3075V3HttpWebClient.GetErrorMessage());
#endif
                }
            }
            else                        //--> switch off
            {
                localAll3075V3HttpWebClient = new All3075V3HttpWebClient(pAccount, caCerts, DebugMode, DebugLevel);
                try
                {
                    try { GHI.Processor.Watchdog.ResetCounter(); }
                    catch { };
                    if (localAll3075V3HttpWebClient.ALL3075_Switch_Off() != HttpStatusCode.OK)
                    {
                        //switchResult = "An error occured when trying to switch the socket to off";
                        throw new Exception("An error occured when trying to switch the power outlet off");
                    }
                    // Be careful to have no unallowed charcters in the string
                    switchMessage = "Response on Command switch OFF: " + localAll3075V3HttpWebClient.SwitchResult("name") + " Result: " + localAll3075V3HttpWebClient.SwitchResult("result_text");
                    switchResult = localAll3075V3HttpWebClient.SwitchResult("result_text");
                    switchState = localAll3075V3HttpWebClient.SwitchResult("result");
                    success = true;
                    _Print_Debug("\r\n" + switchMessage);
                }
                catch
                {
                    //switchResult = "An error occured when trying to switch the socket to off";
                    switchMessage = "An error occured when trying to switch the socket to off";
                    switchState = "?";
                    success = false;
#if DebugPrint
                           Debug.Print(localAll3075V3HttpWebClient.GetErrorMessage());
#endif
                }
            }

            if (commandWasNotAllowed)
            {
                switchMessage = "Not allowed Switch Command: " + switchMessage;
            }

            return new ALL3075V3_SwitchResponse() { Result = switchResult, Message = switchMessage, State = switchState, Success = success };
        }
        */
        #endregion

        #region Method QueryLastRow()
        /*
        private static void QueryLastRow(ref string pSwitchMessage)
        {
#if DebugPrint
                    Debug.Print("\r\nGoing to query for Entities");
#endif
            // Now we query for the last row of the table as selected by the query string "$top=1"
            // (OLS means Of the Last Send)
            string readTimeOLS = DateTime.Now.ToString();  // shall hold send time of the last entity on Azure
            ArrayList queryArrayList = new ArrayList();
            //RoSchmi
            //myAzureSendManager = new AzureSendManager(myCloudStorageAccount, _tablePreFix, _sensorValueHeader, _socketSensorHeader, caCerts, _timeOfLastSend, sendInterval, _azureSends, _AzureDebugMode, _AzureDebugLevel, IPAddress.Parse(fiddlerIPAddress), pAttachFiddler: attachFiddler, pFiddlerPort: fiddlerPort, pUseHttps: Azure_useHTTPS);

            myAzureSendManager_Froggit = new AzureSendManager_Froggit(myCloudStorageAccount, timeZoneOffset, dstStart, dstEnd, dstOffset, _tablePreFix_Froggit, _sensorValueHeader_Froggit, _socketSensorHeader_Froggit, caCerts, DateTime.Now, sendInterval_Current, _azureSends, _AzureDebugMode, _AzureDebugLevel, IPAddress.Parse(fiddlerIPAddress), pAttachFiddler: attachFiddler, pFiddlerPort: fiddlerPort, pUseHttps: Azure_useHTTPS);
            //AzureSendManager.sampleTimeOfLastSent = DateTime.Now.AddDays(-10.0);    // Date in the past
            //AzureSendManager.InitializeQueue();


            try { GHI.Processor.Watchdog.ResetCounter(); }
            catch { };
            HttpStatusCode queryEntityReturnCode = myAzureSendManager.queryTableEntities("$top=1", out queryArrayList);

            if (queryEntityReturnCode == HttpStatusCode.OK)
            {
#if DebugPrint
                        Debug.Print("Query for entities completed. HttpStatusCode: " + queryEntityReturnCode.ToString());
#endif
                try { GHI.Processor.Watchdog.ResetCounter(); }
                catch { };
                if (queryArrayList.Count != 0)
                {
                    var entityHashtable = queryArrayList[0] as Hashtable;
                    string lastBootReason = entityHashtable["bR"].ToString();
                    if (lastBootReason == "X")     // reboot was forced by the program (not enougth free ram)
                    {
                        _lastResetCause = "ForcedReboot";
                        try
                        {
                            _forcedReboots = int.Parse(entityHashtable["forcedReboots"].ToString()) + 1;
                            _badReboots = int.Parse(entityHashtable["badReboots"].ToString());
                            _azureSends = int.Parse(entityHashtable["Sends"].ToString()) + 1;
                            _azureSendErrors = int.Parse(entityHashtable["sendErrors"].ToString());
                            _dayMin = double.Parse(entityHashtable["min"].ToString());
                            _dayMax = double.Parse(entityHashtable["max"].ToString());
                            _lastTemperature[Ch_1_Sel - 1] = double.Parse(entityHashtable["T_1"].ToString());
                            _lastTemperature[Ch_2_Sel - 1] = double.Parse(entityHashtable["T_2"].ToString());
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            _forcedReboots = int.Parse(entityHashtable["forcedReboots"].ToString());
                            _badReboots = int.Parse(entityHashtable["badReboots"].ToString()) + 1;
                            _azureSends = int.Parse(entityHashtable["Sends"].ToString()) + 1;
                            _azureSendErrors = int.Parse(entityHashtable["sendErrors"].ToString());
                            _dayMin = double.Parse(entityHashtable["min"].ToString());
                            _dayMax = double.Parse(entityHashtable["max"].ToString());
                            _lastTemperature[Ch_1_Sel - 1] = double.Parse(entityHashtable["T_1"].ToString());
                            _lastTemperature[Ch_2_Sel - 1] = double.Parse(entityHashtable["T_2"].ToString());
                        }
                        catch { }
                    }
                    readTimeOLS = entityHashtable["SampleTime"].ToString();
                }
            }
            else
            {
                try { GHI.Processor.Watchdog.ResetCounter(); }
                catch { };
#if DebugPrint
                        Debug.Print("Failed to query Entities. HttpStatusCode: " + queryEntityReturnCode.ToString());
#endif
#if SD_Card_Logging
                        var source = new LogContent() { logOrigin = "Query last row", logReason = "n", logPosition = "Query failed", logNumber = 1 };
                        SdLoggerService.LogEventHourly("Error", source);
#endif
            }
            try
            {
                _timeOfLastSend = new DateTime(int.Parse(readTimeOLS.Substring(6, 4)), int.Parse(readTimeOLS.Substring(0, 2)),
                                               int.Parse(readTimeOLS.Substring(3, 2)), int.Parse(readTimeOLS.Substring(11, 2)),
                                               int.Parse(readTimeOLS.Substring(14, 2)), int.Parse(readTimeOLS.Substring(17, 2)));

                // calculate back to the time without dayLightSavingTime offset
                _timeOfLastSend = _timeOfLastSend.AddMinutes(-DayLihtSavingTime.DayLightTimeOffset(dstStart, dstEnd, dstOffset, _timeOfLastSend, true));
            }
            catch
            {
                _timeOfLastSend = DateTime.Now.AddHours(-1.0);  // if something goes wrong, take DateTime.Now minus 1 hour;
            }
            //forceSend = true;                  // after reboot the row is sent independent of sendinterval expired
            pSwitchMessage += _lastResetCause;
        }
        */
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
