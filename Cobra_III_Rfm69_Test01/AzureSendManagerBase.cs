using System;
using Microsoft.SPOT;
using System.Collections;
using System.Net;
using RoSchmi.Net.Azure.Storage;
using System.Security.Cryptography.X509Certificates;
using RoSchmi.DayLihtSavingTime;

//namespace HeatingSurvey
namespace   Cobra_III_Rfm69_Test01
{
    abstract class AzureSendManagerBase
    {
        // static fields have the same content independent of the instance
        internal static CloudStorageAccount _CloudStorageAccount;
        internal static int _timeZoneOffset;
        internal static X509Certificate[] caCerts;       
        internal static bool attachFiddler;
        internal static IPAddress fiddlerIPAddress;
        internal static int fiddlerPort = 8888;
        internal static bool _useHttps;
        internal static int dstOffset;
        internal static string dstStart;
        internal static string dstEnd;
        internal static AzureStorageHelper.DebugMode _debug = AzureStorageHelper.DebugMode.StandardDebug;
        internal static AzureStorageHelper.DebugLevel _debug_level = AzureStorageHelper.DebugLevel.DebugAll;

        internal static readonly object theLock = new object();

        // Strings used mulziple times (saves Code)
        internal static string _Going_to_insert_Entity = ": Going to insert Entity ";
        internal static string _Table_already_exists = ": Table already exists";
        internal static string _Table_created = ": Table created";
        internal static string _Failed_to_create_Table = ": Failed to create Table";
        internal static string _Buffer_empty = ": Buffer empty";
        internal static string _Early_object_discarded = ": Early object discarded";
        internal static string _Entity_was_inserted = ": Entity was inserted ";
        internal static string _Entity_already_created_deleted_from_buffer = ": Entity already created, deleted from buffer ";


        

        public AzureSendManagerBase(CloudStorageAccount pCloudStorageAccount, int pTimeZoneOffset, string pDstStart, string pDstEnd, int pDstOffset, X509Certificate[] pCaCerts, AzureStorageHelper.DebugMode pDebugMode, AzureStorageHelper.DebugLevel pDebugLevel, IPAddress pFiddlerIPAddress, bool pAttachFiddler, int pFiddlerPort, bool pUseHttps)
        {
            _CloudStorageAccount = pCloudStorageAccount;
            _timeZoneOffset = pTimeZoneOffset < -720 ? -720 : pTimeZoneOffset > 720 ? 720 : pTimeZoneOffset;          
            caCerts = pCaCerts;
            _debug = pDebugMode;
            _debug_level = pDebugLevel;
            attachFiddler = pAttachFiddler;
            fiddlerIPAddress = pFiddlerIPAddress;
            fiddlerPort = pFiddlerPort;
            _useHttps = pUseHttps;          
            dstStart = pDstStart;
            dstEnd = pDstEnd;
            dstOffset = pDstOffset;
        }

        
        #region method createTable
        internal HttpStatusCode createTable(CloudStorageAccount pCloudStorageAccount, string pTableName)
        {
            //TableClient table = new TableClient(pCloudStorageAccount, caCerts, _timeZoneOffset, _debug, _debug_level);
            TableClient table = new TableClient(pCloudStorageAccount, caCerts, _debug, _debug_level);

            // To use Fiddler as WebProxy include the following line. Use the local IP-Address of the PC where Fiddler is running
            // see: -http://blog.devmobile.co.nz/2013/01/09/netmf-http-debugging-with-fiddler
            if (attachFiddler)
            { table.attachFiddler(true, fiddlerIPAddress, fiddlerPort); }

            HttpStatusCode resultCode = table.CreateTable(pTableName, TableClient.ContType.applicationIatomIxml, TableClient.AcceptType.applicationIjson, TableClient.ResponseType.dont_returnContent, useSharedKeyLite: false);
            return resultCode;
        }
        #endregion
        
       
        internal ArrayList createOnOffPropertyArrayList(OnOffSample nextSampleValue, int azureSends)
        {
            string TimeOffsetUTCString = nextSampleValue.TimeOffsetUTC < 0 ? nextSampleValue.TimeOffsetUTC.ToString("D3") : "+" + nextSampleValue.TimeOffsetUTC.ToString("D3");
            
            ArrayList propertiesAL = new System.Collections.ArrayList();
            TableEntityProperty property;

            //Add properties to ArrayList (Name, Value, Type)
            property = new TableEntityProperty("Status", nextSampleValue.Status, "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("LastStatus", nextSampleValue.LastStatus, "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("Location", nextSampleValue.Location, "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("SampleTime", nextSampleValue.TimeOfSample.ToString() + " " + TimeOffsetUTCString, "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("TimeFromLast", nextSampleValue.TimeFromLast.Days.ToString("D3") + "-" + nextSampleValue.TimeFromLast.Hours.ToString("D2") + ":" + nextSampleValue.TimeFromLast.Minutes.ToString("D2") + ":" + nextSampleValue.TimeFromLast.Seconds.ToString("D2"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("OnTimeDay", nextSampleValue.OnTimeDay.Days.ToString("D3") + "-" + nextSampleValue.OnTimeDay.Hours.ToString("D2") + ":" + nextSampleValue.OnTimeDay.Minutes.ToString("D2") + ":" + nextSampleValue.OnTimeDay.Seconds.ToString("D2"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("CD", nextSampleValue.CD.ToString("D3"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("OnTimeWeek", nextSampleValue.OnTimeWeek.Days.ToString("D3") + "-" + nextSampleValue.OnTimeWeek.Hours.ToString("D2") + ":" + nextSampleValue.OnTimeWeek.Minutes.ToString("D2") + ":" + nextSampleValue.OnTimeWeek.Seconds.ToString("D2"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("CW", nextSampleValue.CW.ToString("D3"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("OnTimeMonth", nextSampleValue.OnTimeMonth.Days.ToString("D3") + "-" + nextSampleValue.OnTimeMonth.Hours.ToString("D2") + ":" + nextSampleValue.OnTimeMonth.Minutes.ToString("D2") + ":" + nextSampleValue.OnTimeMonth.Seconds.ToString("D2"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("CM", nextSampleValue.CM.ToString("D4"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("OnTimeYear", nextSampleValue.OnTimeYear.Days.ToString("D3") + "-" + nextSampleValue.OnTimeYear.Hours.ToString("D2") + ":" + nextSampleValue.OnTimeYear.Minutes.ToString("D2") + ":" + nextSampleValue.OnTimeYear.Seconds.ToString("D2"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("CY", nextSampleValue.CY.ToString("D5"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("Iterations", nextSampleValue.Iterations.ToString("D6"), "Edm.String");
            propertiesAL.Add(property.propertyArray());           
            property = new TableEntityProperty("Sends", azureSends.ToString("D6"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("RemainRam", nextSampleValue.RemainingRam.ToString("D7"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("forcedReboots", nextSampleValue.ForcedReboots.ToString("D6"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("badReboots", nextSampleValue.BadReboots.ToString("D6"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("sendErrors", nextSampleValue.SendErrors.ToString("D4"), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("bR", nextSampleValue.BootReason.ToString(), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("fS", nextSampleValue.ForceSend ? "X" : ".", "Edm.String");
            propertiesAL.Add(property.propertyArray());
            property = new TableEntityProperty("Message", nextSampleValue.Message.ToString(), "Edm.String");
            propertiesAL.Add(property.propertyArray());
            return propertiesAL;
        }

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
    }
}
