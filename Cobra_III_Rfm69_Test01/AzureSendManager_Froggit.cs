// Adapted for HeatingCurrentSurvey 10. March 2020

using System;
using Microsoft.SPOT;
using System.Collections;
using System.Threading;
using System.Net;
using RoSchmi.Net.Azure.Storage;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using RoSchmi.DayLihtSavingTime;


//namespace HeatingSurvey
 namespace   Cobra_III_Rfm69_Test01
{
    class AzureSendManager_Froggit : AzureSendManagerBase
    {
        private static readonly object theLock = new object();

        #region fields belonging to AzureSendManager
        //****************  SendManager *************************************
        static int yearOfLastSend = 2000;

        //static string prefixOfLastTable = "";
        static ArrayList LastTableNames = new ArrayList();


        public static int _forcedReboots = 0;
        public static int _badReboots = 0;
        public static int _azureSends = 0;
        public static int _azureSendErrors = 0;

        public static int dstOffset;
        public static string dstStart;
        public static string dstEnd;

        const double InValidValue = 999.9;
        public static double _dayMin = 1000.00;   //don't change
        public static double _dayMax = -1000.00;  //don't change
        public static double _lastValue = InValidValue; // InvalidValue

        static int Ch_1_Sel = 1;   // The Channel of the temp/humidity sensor (Values from 1 to 8 are allowed)
        static int Ch_2_Sel = 2;
        static int Ch_3_Sel = 3;
        static int Ch_4_Sel = 4;
        static int Ch_5_Sel = 5;
        static int Ch_6_Sel = 6;
        static int Ch_7_Sel = 7;
        static int Ch_8_Sel = 8;

        public static double[] _lastContent = new double[8] { InValidValue, InValidValue, InValidValue, InValidValue, InValidValue, InValidValue, InValidValue, InValidValue };

        public static double _dayMinWork = 0.00;   //don't change
        public static double _dayMaxWork = 0.00;  //don't change
        public static double _dayMinWorkBefore = 0.00;
        public static double _dayMaxWorkBefore = 0.00;

        public static double _dayMinSolarWork = 0.00;   //don't change
        public static double _dayMaxSolarWork = 0.00;  //don't change
        public static double _dayMinSolarWorkBefore = 0.00;
        public static double _dayMaxSolarWorkBefore = 0.00;


        public static DateTime sampleTimeOfLastSent;  // initial value is set in ProgramStarted
        public static DateTime _timeOfLastSend;
        //private DateTime _timeOfLastSend;
        public static DateTime _timeOfLastSensorEvent;
        public static string _lastResetCause = "";


        public static int _iteration = 0;

        private bool _useHttps = false;
        CloudStorageAccount _CloudStorageAccount;
        string _tablePrefix = "Y";
        string _sensorValueHeader = "Value";
        string _socketSensorHeader = "SecValue";

        public static TimeSpan _sendInterval;
        TableClient table;
        X509Certificate[] caCerts;
        private bool attachFiddler = false;
        private IPAddress fiddlerIPAddress;
        private int fiddlerPort = 8888;                   // Standard port of fiddler
        private Object lockThread = new object();

        private AzureStorageHelper.DebugMode _debug = AzureStorageHelper.DebugMode.StandardDebug;
        private AzureStorageHelper.DebugLevel _debug_level = AzureStorageHelper.DebugLevel.DebugAll;
        //*******************************************************************************
        #endregion

        #region fields belonging to Queue Buffer
        // This is the Queue to hold the measured values
        //private const int _defaultCapacity = 256;
        private const int _defaultCapacity = 20;
        private const int _defaultPreFillLevel = 1;
        private static bool _preFillLevelReached = true;

        private static SampleValue[] _buffer = new SampleValue[_defaultCapacity];

        private static int _head;
        private static int _tail;
        private static int _count;
        private static int _capacity;
        private static int _preFillLevel;
        #endregion

        #region Methods belonging to Queue Buffer
        public static void InitializeQueue()
        {
            _capacity = _defaultCapacity;
            _head = 0;
            _tail = 0;
            _count = 0;
            _preFillLevel = _defaultPreFillLevel;
            _preFillLevelReached = true;
        }

        public static int Count
        {
            get { return _count; }
        }

        public int preFillLevel
        {
            get { return _preFillLevel; }
            set { _preFillLevel = value; }
        }

        public static int capacity
        {
            get { return _capacity; }
        }

        public bool preFillLevelReached
        {
            get { return _preFillLevelReached; }
            set { _preFillLevelReached = value; }
        }

        public static void Clear()
        {
            _count = 0;
            _tail = _head;
        }


        public static bool hasFreePlaces(int value = 1)
        {
            lock (theLock)
            {
                if (_count + value <= _capacity)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static void EnqueueSampleValue(SampleValue SampleValue)
        {
            lock (theLock)
            {
                if (_count == _capacity)
                {
                    Grow();
                    //Debug.Print("New Capacity: " + _buffer.Length);
                }
                _buffer[_head] = SampleValue;
                _head = (_head + 1) % _capacity;
                _count++;
            }
        }

        public static void DiscardNextSampleValue()
        {
             lock (theLock)
             {
                 if (_count > 0)
                {  
                        _tail = (_tail + 1) % _capacity;
                        _count--;
                }              
             }
        }


        public static SampleValue PreViewNextSampleValue()
        {
            lock (theLock)
            {
                SampleValue Value = DequeueSampleValue(true);
                return Value;
            }
        }

        public static SampleValue DequeueNextSampleValue()
        {
            lock (theLock)
            {
                SampleValue Value = DequeueSampleValue(false);
                return Value;
            }
        }

        private static SampleValue DequeueSampleValue(bool PreView)
        {
            lock (theLock)
            {
                if (_count > 0)
                {
                    SampleValue value = _buffer[_tail];

                    if (!PreView)
                    {
                        _tail = (_tail + 1) % _capacity;
                        _count--;
                    }
                    return value;
                }
                else
                {
                    return null;
                }
            }
        }

        private static void Grow()
        {
            int newCapacity = _capacity << 1;
            SampleValue[] newBuffer = new SampleValue[newCapacity];

            if (_tail < _head)
            {
                Array.Copy(_buffer, _tail, newBuffer, 0, _count);
            }
            else
            {
                Array.Copy(_buffer, _tail, newBuffer, 0, _capacity - _tail);
                Array.Copy(_buffer, 0, newBuffer, _capacity - _tail, _head);
            }
            _buffer = newBuffer;
            _head = _count;
            _tail = 0;
            _capacity = newCapacity;
        }
        #endregion



        #region AzureSendManager_Froggit Constructor
        public AzureSendManager_Froggit(CloudStorageAccount pCloudStorageAccount, int pTimeZoneOffset, string pDstStart, string pDstEnd, int pDstOffset, string pTablePreFix, string pSensorValueHeader, string pSocketSensorHeader, X509Certificate[] pCaCerts, DateTime pTimeOfLastSend, TimeSpan pSendInterval, int pAzureSends, AzureStorageHelper.DebugMode pDebugMode, AzureStorageHelper.DebugLevel pDebugLevel, IPAddress pFiddlerIPAddress, bool pAttachFiddler, int pFiddlerPort, bool pUseHttps)
            : base(pCloudStorageAccount, pTimeZoneOffset, pDstStart, pDstEnd, pDstOffset, pCaCerts, pDebugMode, pDebugLevel, pFiddlerIPAddress, pAttachFiddler, pFiddlerPort, pUseHttps)
        {
            _useHttps = pUseHttps;
            _azureSends = pAzureSends;
            _tablePrefix = pTablePreFix;
            _sensorValueHeader = pSensorValueHeader;
            _socketSensorHeader = pSocketSensorHeader;
            _CloudStorageAccount = pCloudStorageAccount;
            _timeOfLastSend = pTimeOfLastSend;
            _sendInterval = pSendInterval;
            attachFiddler = pAttachFiddler;
            fiddlerIPAddress = pFiddlerIPAddress;
            fiddlerPort = pFiddlerPort;
            caCerts = pCaCerts;
            _debug = pDebugMode;
            _debug_level = pDebugLevel;

            dstOffset = pDstOffset;
            dstStart = pDstStart;
            dstEnd = pDstEnd;
        }
        #endregion

        #region public method Start sending contents of buffer
        public void Start()
        {
            Thread sendThread = new Thread(new ThreadStart(runSendThread));
            sendThread.Start();
        }
        #endregion


        #region Method ActualizeFromLastAzureRow
        public Counters ActualizeFromLastAzureRow(ref string pSwitchMessage)
        {
#if DebugPrint
                    Debug.Print("\r\nGoing to query for Entities");
#endif
            // Now we query for the last row of the table as selected by the query string "$top=1"
            // (OLS means Of the Last Send)
            string readTimeOLS = DateTime.Now.ToString();  // shall hold send time of the last entity on Azure

            ArrayList queryArrayList = new ArrayList();
            try { GHI.Processor.Watchdog.ResetCounter(); }
            catch { };
            HttpStatusCode queryEntityReturnCode = queryTableEntities("$top=1", out queryArrayList);

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
                            _lastContent[Ch_3_Sel - 1] = double.Parse(entityHashtable["T_3"].ToString());
                            // RoSchmi
                            //_lastContent[Ch_6_Sel - 1] = double.Parse(entityHashtable["T_6"].ToString());
                            _lastContent[Ch_5_Sel - 1] = double.Parse(entityHashtable["T_5"].ToString());
                            _lastContent[Ch_6_Sel - 1] = double.Parse(entityHashtable["T_6"].ToString());
                            _dayMinWorkBefore = _dayMinWork;
                            _dayMinWork = _lastContent[Ch_6_Sel - 1];

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
                            _lastContent[Ch_3_Sel - 1] = double.Parse(entityHashtable["T_3"].ToString());
                            // RoSchmi
                            //_lastContent[Ch_6_Sel - 1] = double.Parse(entityHashtable["T_6"].ToString());
                            _lastContent[Ch_5_Sel - 1] = double.Parse(entityHashtable["T_5"].ToString());
                            _lastContent[Ch_6_Sel - 1] = double.Parse(entityHashtable["T_6"].ToString());
                            _dayMinWorkBefore = _dayMinWork;
                            _dayMinWork = _lastContent[Ch_6_Sel - 1];
                        }
                        catch { }
                    }

                    readTimeOLS = entityHashtable["SampleTime"].ToString();

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


                }
                else
                {
                    _timeOfLastSend = DateTime.Now;
                }
            }
            else
            {
                try { GHI.Processor.Watchdog.ResetCounter(); }
                catch { };
                _timeOfLastSend = DateTime.Now;


#if DebugPrint
                        Debug.Print("Failed to query Entities. HttpStatusCode: " + queryEntityReturnCode.ToString());
#endif

            }

            pSwitchMessage = "Reboot: " + _lastResetCause;

            Counters returnValue = new Counters();
            returnValue.AzureSends = _azureSends;
            returnValue.AzureSendErrors = _azureSendErrors;
            returnValue.ForcedReboots = _forcedReboots;
            returnValue.BadReboots = _badReboots;


            return returnValue;
        }
        #endregion


        #region public Method queryTableEntities
        public HttpStatusCode queryTableEntities(string query, out ArrayList queryResult)
        {
            // Now we query for the last row of the table as selected by the query string "$top=1"
            ArrayList queryArrayList = new ArrayList();

            //This operation does not work with https, so the CloudStorageAccount is set to use http
            _CloudStorageAccount = new CloudStorageAccount(_CloudStorageAccount.AccountName, _CloudStorageAccount.AccountKey, useHttps: false);

            string tableName = _tablePrefix + DateTime.Now.Year;
            HttpStatusCode queryEntityReturnCode = queryTableEntities(_CloudStorageAccount, tableName, "$top=1", out queryArrayList);

            _CloudStorageAccount = new CloudStorageAccount(_CloudStorageAccount.AccountName, _CloudStorageAccount.AccountKey, useHttps: _useHttps);  // Reset Cloudstorageaccount to the original settings (http or https)
            /*
            if (queryEntityReturnCode == HttpStatusCode.OK)
            { Debug.Print("Query for entities completed. HttpStatusCode: " + queryEntityReturnCode.ToString()); }
            else
            { Debug.Print("Failed to query Entities. HttpStatusCode: " + queryEntityReturnCode.ToString()); }
            */
            queryResult = queryArrayList;
            return queryEntityReturnCode;
        }
        #endregion

        #region runSendThread
        public void runSendThread()
        {
            SampleValue nextSampleValue;
            HttpStatusCode createTableReturnCode;
            HttpStatusCode insertEntityReturnCode = HttpStatusCode.Ambiguous;
            int actYear = DateTime.Now.Year;
            string tableName = null;
            string tablePreFix = null;

            //string tableName = _tablePrefix + actYear.ToString();
            //string tablePreFix = _tablePrefix;

            bool nextSampleValueIsNull = false;
            bool nextSampleValueShallBeSent = false;

            int loopCtr = 0;
            while (loopCtr < 3)   // We try 3 times to deliver to Azure, if not accepted, we neglect
            {
                Debug.Print("Number of entities in Queue is: " + Count.ToString());

                nextSampleValue = PreViewNextSampleValue();
                nextSampleValueIsNull = (nextSampleValue == null) ? true : false;
                nextSampleValueShallBeSent = false;
                if (!nextSampleValueIsNull)
                {
                    nextSampleValueShallBeSent = (nextSampleValue.ForceSend || ((nextSampleValue.TimeOfSample - sampleTimeOfLastSent) > _sendInterval)) ? true : false;
                    tableName = nextSampleValue.TableName;
                    tablePreFix = tableName.Substring(0, tableName.Length - 4);
                }
                if (nextSampleValueIsNull)
                {
                    // RoSchmi
                    this.OnAzureCommandSend(this, new AzureSendEventArgs(false, true, HttpStatusCode.Ambiguous, 1, "Buffer empty"));


                    //Debug.Print("Leaving because buffer is empty. Count in buffer when leaving AzureSendManager = " + Count);
                    break;
                }
                if (!nextSampleValueShallBeSent)
                {
                    //nextSampleValue = DequeueNextSampleValue();    // Discard this to early object
                    DiscardNextSampleValue();
                    // RoSchmi
                    //this.OnAzureCommandSend(this, new AzureSendEventArgs(false, false, HttpStatusCode.Ambiguous, 2, "Early object discarded"));
                }

                
                #region Create a Azure Table, Name = tablePrefix plus the actual year (only when needed)
               
                if ((DateTime.Now.Year != yearOfLastSend) || (!LastTableNames.Contains(tableName)))
                {                 
                    createTableReturnCode = createTable(_CloudStorageAccount, tableName);

                    if ((createTableReturnCode == HttpStatusCode.Created) || (createTableReturnCode == HttpStatusCode.Conflict))
                    {
                        if (createTableReturnCode == HttpStatusCode.Created)
                        {
                            // RoSchmi
                            //this.OnAzureCommandSend(this, new AzureSendEventArgs(false, false, createTableReturnCode, 3, tableName + _Table_created));
                        }
                        else
                        {
                            // RoSchmi
                            //this.OnAzureCommandSend(this, new AzureSendEventArgs(false, false, HttpStatusCode.Ambiguous, 4, tableName + _Table_already_exists));
                        }
                      
                        yearOfLastSend = DateTime.Now.Year;
                                            
                        if (!LastTableNames.Contains(tableName))
                        {
                            LastTableNames.Add(tableName);
                        }
                    }
                    else
                    {
                        if (createTableReturnCode == HttpStatusCode.NoContent)
                        {
                                //Debug.Print("Create Table operation. HttpStatusCode: " + createTableReturnCode.ToString());
                            yearOfLastSend = DateTime.Now.Year;
                        }
                        else
                        {  
                            // RoSchmi
                            //this.OnAzureCommandSend(this, new AzureSendEventArgs(false, false, HttpStatusCode.Ambiguous, 5, tableName + _Failed_to_create_Table));
                            Thread.Sleep(10000);
                            Microsoft.SPOT.Hardware.PowerState.RebootDevice(true, 3000);
                            while (true)
                            {
                                Thread.Sleep(100);
                            }                                                   
                        }
                    }
                    Thread.Sleep(3000);
                }
                #endregion

                
                #region Create an ArrayList  to hold the properties of the entity
                // RoSchmi
                //this.OnAzureCommandSend(this, new AzureSendEventArgs(false, true, HttpStatusCode.Ambiguous, 6, tableName + _Going_to_insert_Entity)); 
                
                // Now we create an Arraylist to hold the properties of a table Row,
                // write these items to an entity
                // and send this entity to the Cloud

                string TimeOffsetUTCString = nextSampleValue.TimeOffSetUTC < 0 ? nextSampleValue.TimeOffSetUTC.ToString("D3") : "+" + nextSampleValue.TimeOffSetUTC.ToString("D3");

                ArrayList propertiesAL = new System.Collections.ArrayList();
                TableEntityProperty property;
                lock (theLock)
                {
                    //Add properties to ArrayList (Name, Value, Type)
                    property = new TableEntityProperty(_sensorValueHeader, nextSampleValue.TheSampleValue.ToString("f2"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));

                    property = new TableEntityProperty("min", nextSampleValue.DayMin.ToString("f2"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));

                    property = new TableEntityProperty("max", nextSampleValue.DayMax.ToString("f2"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));

                    property = new TableEntityProperty("T_1", nextSampleValue.T_0.ToString("f2"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));


                    property = new TableEntityProperty("T_2", nextSampleValue.T_1.ToString("f2"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));


                    property = new TableEntityProperty("T_3", nextSampleValue.T_2.ToString("f2"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));


                    property = new TableEntityProperty("T_4", nextSampleValue.T_3.ToString("f2"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));

                    property = new TableEntityProperty("T_5", nextSampleValue.T_4.ToString("f2"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));

                    property = new TableEntityProperty("T_6", nextSampleValue.T_5.ToString("f2"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));

                    property = new TableEntityProperty(_socketSensorHeader, nextSampleValue.SecondReport, "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));
                    property = new TableEntityProperty("Status", nextSampleValue.Status, "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));

                    property = new TableEntityProperty("Location", nextSampleValue.Location, "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));
                    property = new TableEntityProperty("SampleTime", nextSampleValue.TimeOfSample.ToString() + " " + TimeOffsetUTCString, "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));
                    property = new TableEntityProperty("TimeFromLast", nextSampleValue.TimeFromLast.Days.ToString("D3") + "-" + nextSampleValue.TimeFromLast.Hours.ToString("D2") + ":" + nextSampleValue.TimeFromLast.Minutes.ToString("D2") + ":" + nextSampleValue.TimeFromLast.Seconds.ToString("D2"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));

                    property = new TableEntityProperty("Info", nextSampleValue.SendInfo.ToString("D4"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));

                    property = new TableEntityProperty("RSSI", nextSampleValue.RSSI.ToString("D3"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));

                    property = new TableEntityProperty("Iterations", nextSampleValue.Iterations.ToString("D6"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));
                    property = new TableEntityProperty("Sends", _azureSends.ToString("D6"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));
                    property = new TableEntityProperty("RemainRam", nextSampleValue.RemainingRam.ToString("D7"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));
                    property = new TableEntityProperty("forcedReboots", nextSampleValue.ForcedReboots.ToString("D6"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));
                    property = new TableEntityProperty("badReboots", nextSampleValue.BadReboots.ToString("D6"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));
                    property = new TableEntityProperty("sendErrors", nextSampleValue.SendErrors.ToString("D4"), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));
                    property = new TableEntityProperty("bR", nextSampleValue.BootReason.ToString(), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));
                    property = new TableEntityProperty("fS", nextSampleValue.ForceSend ? "X" : ".", "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));
                    property = new TableEntityProperty("Message", nextSampleValue.Message.ToString(), "Edm.String");
                    propertiesAL.Add(makePropertyArray.result(property));
                }
                #endregion

                
                DateTime actDate = nextSampleValue.TimeOfSample;


                //calculate reverse Date, so the last entity can be retrieved with the Azure $top1 query
                string reverseDate = (10000 - actDate.Year).ToString("D4") + (12 - actDate.Month).ToString("D2") + (31 - actDate.Day).ToString("D2")
                       + (23 - actDate.Hour).ToString("D2") + (59 - actDate.Minute).ToString("D2") + (59 - actDate.Second).ToString("D2");

                TempEntity myTempEntity = new TempEntity(nextSampleValue.PartitionKey, reverseDate, propertiesAL);
                string insertEtag = null;

                //RoSchmi
                Debug.Print("\r\nTry to insert in Table: " + tableName + " RowKey " + reverseDate + "\r\n");

                insertEntityReturnCode = insertTableEntity(_CloudStorageAccount, tableName, myTempEntity, out insertEtag);

                #region Outcommented (for tests)
                // only for testing to produce entity that is rejected by Azure
                // insertEntityReturnCode = insertTableEntity(_CloudStorageAccount, tableName.Substring(0, 6), myTempEntity, out insertEtag);

                //****************  to delete ****************************
                /*
                     if (DateTime.Now < new DateTime(2016, 8, 2, 0, 31, 1))
                     {
                         Debug.Print("Löschen geblockt");
                         insertEntityReturnCode = HttpStatusCode.Ambiguous;
                     }
                     else
                     {
                         Debug.Print("Löschen erlaubt");
                     }
                */
                //*********************************************************
                #endregion


                if ((insertEntityReturnCode == HttpStatusCode.Created) || (insertEntityReturnCode == HttpStatusCode.NoContent) || (insertEntityReturnCode == HttpStatusCode.Conflict))
                {
                    // RoSchmi
                    Debug.Print("Entity was inserted. Try: " + loopCtr.ToString() + " HttpStatusCode: " + insertEntityReturnCode.ToString());

                    nextSampleValue = DequeueNextSampleValue();
                    Thread.Sleep(0);
                    if (nextSampleValue != null)
                    {
                        if (!nextSampleValue.ForceSend)
                        { sampleTimeOfLastSent = nextSampleValue.TimeOfSample; }
                    }

                    if (insertEntityReturnCode == HttpStatusCode.Conflict)
                    {        
                        this.OnAzureCommandSend(this, new AzureSendEventArgs(false, false, HttpStatusCode.Ambiguous, 7, tableName + _Entity_already_created_deleted_from_buffer + reverseDate));
                    }
                    else
                    {
                        this.OnAzureCommandSend(this, new AzureSendEventArgs(true, false, insertEntityReturnCode, 0, tableName + _Entity_was_inserted + reverseDate));
                    }

                    // don't break, the loop is left when we try to get the next row until it is null
                }
                else
                {
                    if (insertEntityReturnCode == HttpStatusCode.Ambiguous) // this is returned when no contact to internet
                    {
                        yearOfLastSend = 2000;
                        
                        this.OnAzureCommandSend(this, new AzureSendEventArgs(false, true, HttpStatusCode.Ambiguous, 7, "No Internet access"));

                        //Debug.Print("Leaving because of no internet access. Count in buffer when leaving AzureSendManager = " + Count);
                        break;
                    }
                    else
                    {
                        yearOfLastSend = 2000;
                        this.OnAzureCommandSend(this, new AzureSendEventArgs(false, false, HttpStatusCode.Ambiguous, 8, tableName + ": Failed to insert Entity, one try" + reverseDate));
                        //Debug.Print("Failed to insert Entity, Try: " + loopCtr.ToString() + " HttpStatusCode: " + insertEntityReturnCode.ToString());
                        Thread.Sleep(3000);
                        loopCtr++;
                    }
                }




                if (loopCtr >= 3)           // if Azure does not accept the entity, we give up after the third try and discard this entity
                {
                    nextSampleValue = DequeueNextSampleValue();
                    this.OnAzureCommandSend(this, new AzureSendEventArgs(false, true, HttpStatusCode.Ambiguous, 9, tableName +  "Failed to insert Entity after 3 tries"));
                    //Debug.Print("Leaving because Entity was discarded after 3rd try. Count in buffer when leaving AzureSendManager = " + Count);
                    if (DateTime.Now < new DateTime(2016, 7, 1))       // Reboot if Entity can not be inserted, probably because of wrong time
                    {
                        Thread.Sleep(20000);
                        Microsoft.SPOT.Hardware.PowerState.RebootDevice(true, 3000);
                        while (true)
                        {
                            Thread.Sleep(100);
                        }
                    }
                    break;
                }
                Thread.Sleep(1);
            }
            Debug.Print("Jumped out of while loop");

            //Debug.Print("Count in buffer when entering AzureSendManager = " + Count);



        }
        #endregion

        #region private method createTable
        private HttpStatusCode createTable(CloudStorageAccount pCloudStorageAccount, string pTableName)
        {
            table = new TableClient(pCloudStorageAccount, caCerts, _debug, _debug_level);



            // To use Fiddler as WebProxy include the following line. Use the local IP-Address of the PC where Fiddler is running
            // see: -http://blog.devmobile.co.nz/2013/01/09/netmf-http-debugging-with-fiddler
            if (attachFiddler)
            { table.attachFiddler(true, fiddlerIPAddress, fiddlerPort); }

            HttpStatusCode resultCode = table.CreateTable(pTableName, TableClient.ContType.applicationIatomIxml, TableClient.AcceptType.applicationIjson, TableClient.ResponseType.dont_returnContent, useSharedKeyLite: false);
            return resultCode;
        }
        #endregion

        #region private method insertTableEntity
        private HttpStatusCode insertTableEntity(CloudStorageAccount pCloudStorageAccount, string pTable, TableEntity pTableEntity, out string pInsertETag)
        {
            table = new TableClient(pCloudStorageAccount, caCerts, _debug, _debug_level);
            // To use Fiddler as WebProxy include the following line. Use the local IP-Address of the PC where Fiddler is running
            // see: -http://blog.devmobile.co.nz/2013/01/09/netmf-http-debugging-with-fiddler
            if (attachFiddler)
            { table.attachFiddler(true, fiddlerIPAddress, fiddlerPort); }

            var resultCode = table.InsertTableEntity(pTable, pTableEntity, TableClient.ContType.applicationIatomIxml, TableClient.AcceptType.applicationIjson, TableClient.ResponseType.dont_returnContent, useSharedKeyLite: false);
            pInsertETag = table.OperationResponseETag;
            //var body = table.OperationResponseBody;
            //Debug.Print("Entity inserted");
            return resultCode;
        }
        #endregion

        #region private method queryTableEntities
        private HttpStatusCode queryTableEntities(CloudStorageAccount pCloudStorageAccount, string tableName, string query, out ArrayList queryResult)
        {
            table = new TableClient(pCloudStorageAccount, caCerts, _debug, _debug_level);


            // To use Fiddler as WebProxy include the following line. Use the local IP-Address of the PC where Fiddler is running
            // see: -http://blog.devmobile.co.nz/2013/01/09/netmf-http-debugging-with-fiddler
            if (attachFiddler)
            { table.attachFiddler(true, fiddlerIPAddress, fiddlerPort); }

            HttpStatusCode resultCode = table.QueryTableEntities(tableName, query, TableClient.ContType.applicationIatomIxml, TableClient.AcceptType.applicationIatomIxml, useSharedKeyLite: false);
            // now we can get the results by reading the properties: table.OperationResponse......
            queryResult = table.OperationResponseQueryList;
            // var body = table.OperationResponseBody;
            // this shows how to get a special value (here the RowKey)of the first entity
            // var entityHashtable = queryResult[0] as Hashtable;
            // var theRowKey = entityHashtable["RowKey"];
            return resultCode;
        }
        #endregion

        #region AzureSendEventArgs
        /// <summary>
        /// Event arguments for the AzureSend event
        /// </summary>
        public class AzureSendEventArgs : EventArgs
        {

            /// <summary>
            /// true if the row was sent
            /// </summary>
            /// 
            public bool azureCommandWasSent
            { get; private set; }


            /// <summary>
            /// true if the row was sent
            /// </summary>
            /// 
            public bool decrementThreadCounter
            { get; private set; }


            /// <summary>
            /// The HttpStatusCode of the response
            /// </summary>
            /// 
            public HttpStatusCode returnCode
            { get; private set; }


            /// <summary>
            /// Additional Code of the response
            /// </summary>
            /// 
            public int Code
            { get; private set; }

            /// <summary>
            /// Additional Message of the response
            /// </summary>
            /// 
            public string Message
            { get; private set; }


            /// <summary>
            /// The time of the completed http response
            /// </summary>
            public DateTime timeOfCompletion
            { get; private set; }

            internal AzureSendEventArgs(bool pAzureCommandWasSent, bool pDecrementThreadCounter, HttpStatusCode pReturnCode, int pCode, string pMessage)
            {
                this.azureCommandWasSent = pAzureCommandWasSent;
                this.decrementThreadCounter = pDecrementThreadCounter;
                this.returnCode = pReturnCode;
                this.Code = pCode;
                this.Message = pMessage;
                this.timeOfCompletion = DateTime.Now;
            }
        }
        #endregion

        #region Delegate
        /// <summary>
        /// The delegate that is used to handle the IR event.
        /// </summary>
        /// <param name="sender">The <see cref="R_433_Receiver"/> object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        //public delegate void SignalReceivedEventHandler(RF_433_Receiver sender, SignalReceivedEventArgs e);
        public delegate void AzureSendManagerEventHandler(AzureSendManager_Froggit sender, AzureSendEventArgs e);

        /// <summary>
        /// Raised when the module detects an 433 Mhz signal.
        /// </summary>
        public event AzureSendManagerEventHandler AzureCommandSend;

        private AzureSendManagerEventHandler onAzureCommandSend;

        private void OnAzureCommandSend(AzureSendManager_Froggit sender, AzureSendEventArgs e)
        {
            if (this.onAzureCommandSend == null)
            {
                this.onAzureCommandSend = this.OnAzureCommandSend;
            }
            //Changed by RoSchmi
            //if (Program.CheckAndInvoke(this.AzureCommandSend, this.onAzureCommandSend, sender, e))
            this.AzureCommandSend(sender, e);
        }

        // myAzureSendManager_Froggit.AzureCommandSend += myAzureSendManager_Froggit_AzureCommandSend;
        #endregion
    }
}
