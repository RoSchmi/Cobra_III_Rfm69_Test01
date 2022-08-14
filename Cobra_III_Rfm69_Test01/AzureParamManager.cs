using System;
using Microsoft.SPOT;
using System.Collections;
using System.Threading;
using System.Net;
using RoSchmi.Net.Azure.Storage;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Cobra_III_Rfm69_Test01
{
    class AzureParamManager
    {
        #region fields belonging to AzureParamManager
        //****************  ParamManager *************************************
        //static int yearOfLastSend = 2000;
        //public static DateTime sampleTimeOfLastSent;  // initial value is set in ProgramStarted
        //private DateTime _timeOfLastSend;
        //private TimeSpan _sendInterval;
        //private int _azureSends;
        private bool _useHttps = false;
        CloudStorageAccount _CloudStorageAccount;
        string _tablePrefix = "Y";
        //string _sensorValueHeader = "Value";
        //string _socketSensorHeader = "SecValue";
        TableClient table;
        X509Certificate[] caCerts;
        private bool attachFiddler = false;
        private IPAddress fiddlerIPAddress;
        private int fiddlerPort = 8888;                   // Standard port of fiddler
        private Object lockThread = new object();
        private AzureStorageHelper.DebugMode _DebugMode = AzureStorageHelper.DebugMode.NoDebug;
        private AzureStorageHelper.DebugLevel _DebugLevel = AzureStorageHelper.DebugLevel.DebugErrors;
        //*******************************************************************************
        #endregion


        #region AzureParamManager Constructor
        public AzureParamManager(CloudStorageAccount pCloudStorageAccount, string pTablePreFix, X509Certificate[] pCaCerts, AzureStorageHelper.DebugMode pDebugMode, AzureStorageHelper.DebugLevel pDebugLevel, IPAddress pFiddlerIPAddress, bool pAttachFiddler, int pFiddlerPort, bool pUseHttps)
        {
            _useHttps = pUseHttps;
            //_azureSends = pAzureSends;
            _tablePrefix = pTablePreFix;
            //_sensorValueHeader = pSensorValueHeader;
            //_socketSensorHeader = pSocketSensorHeader;
            _CloudStorageAccount = pCloudStorageAccount;
            //_timeOfLastSend = pTimeOfLastSend;
            //_sendInterval = pSendInterval;
            attachFiddler = pAttachFiddler;
            fiddlerIPAddress = pFiddlerIPAddress;
            fiddlerPort = pFiddlerPort;
            caCerts = pCaCerts;
            _DebugMode = pDebugMode;
            _DebugLevel = pDebugLevel;
        }
        #endregion

        #region private method insertTableEntity
        public HttpStatusCode insertTableEntity(CloudStorageAccount pCloudStorageAccount, string pTable, TableEntity pTableEntity, out string pInsertETag)
        {
            table = new TableClient(pCloudStorageAccount, caCerts, _DebugMode, _DebugLevel);
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

        #region public Method queryTableEntities
        public HttpStatusCode queryTableEntities(string query, out ArrayList queryResult)
        {
            // Now we query for the last row of the table as selected by the query string "$top=1"
            ArrayList queryArrayList = new ArrayList();

            //This operation does not work with https, so the CloudStorageAccount is set to use http
            _CloudStorageAccount = new CloudStorageAccount(_CloudStorageAccount.AccountName, _CloudStorageAccount.AccountKey, useHttps: false);

            string tableName = _tablePrefix + "Params";
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

        #region private method queryTableEntities
        private HttpStatusCode queryTableEntities(CloudStorageAccount pCloudStorageAccount, string tableName, string query, out ArrayList queryResult)
        {
            table = new TableClient(pCloudStorageAccount, caCerts, _DebugMode, _DebugLevel);


            // To use Fiddler as WebProxy include the following line. Use the local IP-Address of the PC where Fiddler is running
            // see: -http://blog.devmobile.co.nz/2013/01/09/netmf-http-debugging-with-fiddler
            if (attachFiddler)
            { table.attachFiddler(true, fiddlerIPAddress, fiddlerPort); }

            HttpStatusCode resultCode = table.QueryTableEntities(tableName, query, TableClient.ContType.applicationIatomIxml, TableClient.AcceptType.applicationIatomIxml, useSharedKeyLite: false);
            //HttpStatusCode resultCode = table.QueryTableEntities(tableName.Substring(0,tableName.Length -2) , query, TableClient.ContType.applicationIatomIxml, TableClient.AcceptType.applicationIatomIxml, useSharedKeyLite: false);
            // now we can get the results by reading the properties: table.OperationResponse......
            queryResult = table.OperationResponseQueryList;
            // var body = table.OperationResponseBody;
            // this shows how to get a special value (here the RowKey)of the first entity
            // var entityHashtable = queryResult[0] as Hashtable;
            // var theRowKey = entityHashtable["RowKey"];
            return resultCode;
        }
        #endregion

        #region private method createTable
        public HttpStatusCode createTable(CloudStorageAccount pCloudStorageAccount, string pTableName)
        {

            table = new TableClient(pCloudStorageAccount, caCerts, _DebugMode, _DebugLevel);

            // To use Fiddler as WebProxy include the following line. Use the local IP-Address of the PC where Fiddler is running
            // see: -http://blog.devmobile.co.nz/2013/01/09/netmf-http-debugging-with-fiddler
            if (attachFiddler)
            { table.attachFiddler(true, fiddlerIPAddress, fiddlerPort); }

            HttpStatusCode resultCode = table.CreateTable(pTableName, TableClient.ContType.applicationIatomIxml, TableClient.AcceptType.applicationIjson, TableClient.ResponseType.dont_returnContent, useSharedKeyLite: false);
            return resultCode;
        }
        #endregion


    }
}
