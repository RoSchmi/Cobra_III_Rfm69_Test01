// Copyright RoSchmi 2016 License Apache 2.0
// Version 2.0 28.05.2016 NETMF 4.3 GHI SDK 2016 R1 Prerelease 2
// Parts of the code were taken from
// by AndyCross: netmfazurestorage / Table / TableClient.cs
// -https://github.com/azure-contrib/netmfazurestorage/blob/master/netmfazurestorage/Table/TableClient.cs
//
// Other parts of the code are taken from martin calsyn
// -https://github.com/PervasiveDigital/serialwifi/tree/master/src/Common

using System;
using Microsoft.SPOT;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using PervasiveDigital.Utilities;
using PervasiveDigital.Security.ManagedProviders;
using System.Security.Cryptography.X509Certificates;

namespace RoSchmi.Net.Azure.Storage
{
    public class TableClient
    {
        private readonly CloudStorageAccount _account;
        //private string VersionHeader = "2011-08-18";
        //private string VersionHeader = "2015-02-21";
        private string VersionHeader = "2015-04-05";
        
        internal DateTime InstanceDate { get; set; }

        private bool _fiddlerIsAttached = false;
        private IPAddress _fiddlerIP = null;
        private int _fiddlerPort = 8888;

        #region "Debugging"
        private AzureStorageHelper.DebugMode _debug = AzureStorageHelper.DebugMode.NoDebug;
        private AzureStorageHelper.DebugLevel _debug_level = AzureStorageHelper.DebugLevel.DebugErrors;
        

        private void _Print_Debug(string message)
        {
            switch (_debug)
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
                    Debug.Print(message);
                    break;
            }
        }
         #endregion


        public enum ContType
        {
            applicationIatomIxml,
            applicationIjson
        }
        public enum AcceptType
        {
            applicationIatomIxml,
            applicationIjson
        }

        public enum ResponseType
        {
            returnContent,
            dont_returnContent
        }
        
        private string _PartitionKey = "";
        private string _RowKey = "";
        private string _Query = "";

        private string _OperationResponseBody = null;
        private string _OperationResponseMD5 = null;
        private string _OperationResponseETag = null;
        private Hashtable _OperationResponseSingleQuery = null;
        private ArrayList _OperationResponseQueryList = null;

        //Root CA Certificate needed to validate HTTPS servers.
        public static X509Certificate[] caCerts;

        /// <summary>
        /// Set the debugging level.
        /// </summary>
        /// <param name="Debug_Level">The debug level</param>
        public void SetDebugLevel(AzureStorageHelper.DebugLevel Debug_Level)
        {
            this._debug_level = Debug_Level;
        }
        /// <summary>
        /// Set the debugging mode.
        /// </summary>
        /// <param name="Debug_Level">The debug level</param>
        public void SetDebugMode(AzureStorageHelper.DebugMode Debug_Mode)
        {
            this._debug = Debug_Mode;
        }


        #region Accessors for OperationResponse
        public string OperationResponseBody
        { get {return _OperationResponseBody;}}
        
        public string OperationResponseMD5
        { get { return _OperationResponseMD5; }}
        
        public string OperationResponseETag
        { get { return _OperationResponseETag; }}
        
        public Hashtable OperationResponseSingleQuery
        { get { return _OperationResponseSingleQuery; }}
        
        public ArrayList OperationResponseQueryList
        { get { return _OperationResponseQueryList; }}
        #endregion

        protected byte[] GetBodyBytesAndLength(string body, out int contentLength)
        {
            var content = Encoding.UTF8.GetBytes(body);
            contentLength = content.Length;
            return content;
        }

        protected string GetDateHeader()
        {
            return DateTime.UtcNow.ToString("R");
        }

        #region Constructor
        public TableClient(CloudStorageAccount account, X509Certificate[] pCertificat, AzureStorageHelper.DebugMode pDebugMode, AzureStorageHelper.DebugLevel pDebugLevel)
        {
            _account = account;
            InstanceDate = DateTime.UtcNow;
            caCerts = pCertificat;
            _debug = pDebugMode;
            _debug_level = pDebugLevel;
        }
        #endregion

        #region private OperationResultsClear
        private void OperationResultsClear()
        {
            _OperationResponseETag = null;
            _OperationResponseBody = null;
            _OperationResponseMD5 = null;
            _OperationResponseSingleQuery = null;
            _OperationResponseQueryList = null;
        }
        #endregion

        #region private getContentTypeString
        private string getContentTypeString(ContType pContentType)
        {
            if (pContentType == ContType.applicationIatomIxml)
            { return "application/atom+xml"; }
            else
            { return "application/json"; }
        }
        #endregion

        #region private getAcceptTypeString
        private string getAcceptTypeString(AcceptType pAcceptType)
        {
            if (pAcceptType == AcceptType.applicationIatomIxml)
            { return "application/atom+xml"; }
            else
            { return "application/json"; }
        }
        #endregion

        #region private getResponseTypeString
        private string getResponseTypeString(ResponseType pResponseType)
        {
            if (pResponseType == ResponseType.returnContent)
            { return "return-content"; }
            else
            { return "return-no-content"; }
        }
        #endregion

        #region public attachFiddler
        public void attachFiddler(bool pfiddlerIsAttached, IPAddress pfiddlerIP, int pfiddlerPort)
        {
            _fiddlerIsAttached = pfiddlerIsAttached;
            _fiddlerIP = pfiddlerIP;
            _fiddlerPort = pfiddlerPort;
        }
        #endregion

        #region CreateTable
        public HttpStatusCode CreateTable(string tableName, ContType pContentType = ContType.applicationIatomIxml, AcceptType pAcceptType = AcceptType.applicationIjson, ResponseType pResponseType = ResponseType.returnContent, bool useSharedKeyLite = false)
        {
            OperationResultsClear(); ;
            string timestamp = GetDateHeader();
            string content = string.Empty;

            string contentType = getContentTypeString(pContentType);
            string acceptType = getAcceptTypeString(pAcceptType);



            content = "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>" +
            "<entry xmlns:d=\"http://schemas.microsoft.com/ado/2007/08/dataservices\"  " +
            "xmlns:m=\"http://schemas.microsoft.com/ado/2007/08/dataservices/metadata\" " +
            "xmlns=\"http://www.w3.org/2005/Atom\"> " +
            "<id>http://" + _account.AccountName + ".table.core.windows.net/Tables('"
                + tableName +
            "')</id>" +
            "<title />" +
            "<updated>" + InstanceDate.ToString("yyyy-MM-ddTHH:mm:ss.0000000Z") + "</updated>" +
            "<author><name/></author> " +
            "<content type=\"application/xml\"><m:properties><d:TableName>" + tableName + "</d:TableName></m:properties></content></entry>";

            string HttpVerb = "POST";
            string ContentMD5 = string.Empty;
            int contentLength = 0;
            byte[] payload = GetBodyBytesAndLength(content, out contentLength);
            string authorizationHeader = CreateTableAuthorizationHeader(payload, StringUtilities.Format("/{0}/{1}", _account.AccountName, "Tables()"), timestamp, HttpVerb, pContentType, out ContentMD5, useSharedKeyLite = false);

            string urlPath = StringUtilities.Format("{0}", tableName);

            string canonicalizedResource = StringUtilities.Format("/{0}/{1}", _account.AccountName, urlPath);

            string canonicalizedHeaders = StringUtilities.Format("Date:{0}\nx-ms-date:{1}\nx-ms-version:{2}", timestamp, timestamp, VersionHeader);
            string TableEndPoint = _account.UriEndpoints["Table"].ToString();

            Uri uri = new Uri(TableEndPoint + "/Tables()");

            var tableTypeHeaders = new Hashtable();
            tableTypeHeaders.Add("Accept-Charset", "UTF-8");
            tableTypeHeaders.Add("MaxDataServiceVersion", "3.0;NetFx");
            tableTypeHeaders.Add("Content-Type", contentType);
            tableTypeHeaders.Add("DataServiceVersion", "3.0");
            tableTypeHeaders.Add("Prefer", getResponseTypeString(pResponseType));
            tableTypeHeaders.Add("Content-MD5", ContentMD5);

            if (_fiddlerIsAttached)
            { AzureStorageHelper.AttachFiddler(_fiddlerIsAttached, _fiddlerIP, _fiddlerPort); }

            BasicHttpResponse response = new BasicHttpResponse();
            try
            {
                AzureStorageHelper.SetDebugMode(_debug);
                AzureStorageHelper.SetDebugLevel(_debug_level);
                response = AzureStorageHelper.SendWebRequest(uri, authorizationHeader, timestamp, VersionHeader, payload, contentLength, HttpVerb, false, acceptType, tableTypeHeaders);
                return response.StatusCode;
            }
            catch (Exception ex)
            {
                _Print_Debug("Exception was cought: " + ex.Message);
                response.StatusCode = HttpStatusCode.Forbidden;
                return response.StatusCode;
            }
        }
        #endregion

        #region DeleteTable
        public HttpStatusCode DeleteTable(string tableName, ContType pContentType = ContType.applicationIatomIxml, AcceptType pAcceptType = AcceptType.applicationIjson, bool useSharedKeyLite = false)
        {
            OperationResultsClear();;
            string timestamp = GetDateHeader();
            string content = string.Empty;
            string queryString = "Tables";

            string contentType = getContentTypeString(pContentType);

            string acceptType = getAcceptTypeString(pAcceptType);
            if (pAcceptType == AcceptType.applicationIjson)
            { acceptType = "application/json;odata=minimalmetadata"; }

            string HttpVerb = "DELETE";
            string ContentMD5 = string.Empty;
            int contentLength = 0;
            byte[] payload = GetBodyBytesAndLength(content, out contentLength);
            if (tableName != string.Empty)
            {
                queryString = StringUtilities.Format("Tables('{0}')", tableName);
            }
            string authorizationHeader = CreateTableAuthorizationHeader(payload, StringUtilities.Format("/{0}/{1}", _account.AccountName, queryString), timestamp, HttpVerb, pContentType, out ContentMD5, useSharedKeyLite);

            string TableEndPoint = _account.UriEndpoints["Table"].ToString();
            Uri uri = new Uri(TableEndPoint + "/" + queryString);
            var tableTypeHeaders = new Hashtable();
            tableTypeHeaders.Add("Accept-Charset", "UTF-8");
            tableTypeHeaders.Add("MaxDataServiceVersion", "3.0;NetFx");
            tableTypeHeaders.Add("Content-Type", contentType);
            tableTypeHeaders.Add("DataServiceVersion", "3.0");
            tableTypeHeaders.Add("Content-MD5", ContentMD5);

            if (_fiddlerIsAttached)
            { AzureStorageHelper.AttachFiddler(_fiddlerIsAttached, _fiddlerIP, _fiddlerPort); }

            BasicHttpResponse response = new BasicHttpResponse();
            try
            {
                AzureStorageHelper.SetDebugMode(_debug);
                AzureStorageHelper.SetDebugLevel(_debug_level);
                response = AzureStorageHelper.SendWebRequest(uri, authorizationHeader, timestamp, VersionHeader, payload, contentLength, HttpVerb, false, acceptType, tableTypeHeaders);

                return response.StatusCode;
            }
            catch (Exception ex)
            {
                _Print_Debug("Exception was cought: " + ex.Message);
                response.StatusCode = HttpStatusCode.Forbidden;
                return response.StatusCode;
            }
        }
        #endregion

        #region QueryTables

        public  HttpStatusCode QueryTables(string tableName, ContType pContentType = ContType.applicationIatomIxml, AcceptType pAcceptType =  AcceptType.applicationIjson, bool useSharedKeyLite = false)
        {
            OperationResultsClear();;
            string timestamp = GetDateHeader();
            string content = string.Empty;
            string queryString = "Tables";

            string contentType = getContentTypeString(pContentType);

            string acceptType = getAcceptTypeString(pAcceptType);
            if (pAcceptType == AcceptType.applicationIjson)
            { acceptType = "application/json;odata=minimalmetadata"; }

            string HttpVerb = "GET";
            string ContentMD5 = string.Empty;
            int contentLength = 0;
            byte[] payload = GetBodyBytesAndLength(content, out contentLength);
            if (tableName != string.Empty)
            {
                queryString = StringUtilities.Format("Tables('{0}')", tableName);
            }
            string authorizationHeader = CreateTableAuthorizationHeader(payload, StringUtilities.Format("/{0}/{1}", _account.AccountName, queryString), timestamp, HttpVerb, pContentType, out ContentMD5, useSharedKeyLite);
            
            string TableEndPoint = _account.UriEndpoints["Table"].ToString();
            Uri uri = new Uri(TableEndPoint + "/" + queryString);
            var tableTypeHeaders = new Hashtable();
            tableTypeHeaders.Add("Accept-Charset", "UTF-8");
            tableTypeHeaders.Add("MaxDataServiceVersion", "3.0;NetFx");
            tableTypeHeaders.Add("Content-Type", contentType);
            tableTypeHeaders.Add("DataServiceVersion", "3.0");
            tableTypeHeaders.Add("Content-MD5", ContentMD5);

            if (_fiddlerIsAttached)
            { AzureStorageHelper.AttachFiddler(_fiddlerIsAttached, _fiddlerIP, _fiddlerPort); }

            BasicHttpResponse response = new BasicHttpResponse();
            try
            {
                AzureStorageHelper.SetDebugMode(_debug);
                AzureStorageHelper.SetDebugLevel(_debug_level);
                response = AzureStorageHelper.SendWebRequest(uri, authorizationHeader, timestamp, VersionHeader, payload, contentLength, HttpVerb, false, acceptType, tableTypeHeaders);

                return response.StatusCode;
            }
            catch (Exception ex)
            {
                _Print_Debug("Exception was cought: " + ex.Message);
                response.StatusCode = HttpStatusCode.Forbidden;
                return response.StatusCode;
            }
        }
        #endregion

        #region InsertTabelEntity
        public HttpStatusCode InsertTableEntity(string tableName, TableEntity pEntity, ContType pContentType = ContType.applicationIatomIxml, AcceptType pAcceptType = AcceptType.applicationIjson, ResponseType pResponseType = ResponseType.returnContent, bool useSharedKeyLite = false)
        {
            OperationResultsClear(); ;
            string timestamp = GetDateHeader();
            string content = string.Empty;

            string contentType = getContentTypeString(pContentType);
            string acceptType = getAcceptTypeString(pAcceptType);

            switch (contentType)
            {
                case "application/json":
                    {
                        content = pEntity.ReadJson();
                    }
                    break;
                case "application/atom+xml":
                    {
                        content =
                          StringUtilities.Format("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><entry xmlns:d=\"http://schemas.microsoft.com/ado/2007/08/dataservices\" xmlns:m=\"http://schemas.microsoft.com/ado/2007/08/dataservices/metadata\" xmlns=\"http://www.w3.org/2005/Atom\">" +
            "<id>http://{0}.table.core.windows.net/{5}(PartitionKey='{2}',RowKey='{3}')</id>" +
            "<title/><updated>{1}</updated>" +
            "<author><name /></author>" +
            "<content type=\"application/atom+xml\"><m:properties><d:PartitionKey>{2}</d:PartitionKey><d:RowKey>{3}</d:RowKey>" +
            "{4}" +
            "</m:properties>" +
            "</content>" +
            "</entry>", _account.AccountName, timestamp, pEntity.PartitionKey, pEntity.RowKey, GetTableXml(pEntity.Properties), tableName);

                    }
                    break;
                default:
                    {
                        throw new NotSupportedException("ContentType must be 'application/json' or 'application/atom+xml'");
                    }
            }
            string HttpVerb = "POST";
            int contentLength = 0;
            byte[] payload = GetBodyBytesAndLength(content, out contentLength);
            string ContentMD5 = string.Empty;
            var authorizationHeader = CreateTableAuthorizationHeader(payload, StringUtilities.Format("/{0}/{1}", _account.AccountName, tableName + "()"), timestamp, HttpVerb, pContentType, out ContentMD5, useSharedKeyLite);

            string urlPath = StringUtilities.Format("{0}", tableName);

            string canonicalizedResource = StringUtilities.Format("/{0}/{1}", _account.AccountName, urlPath);

            string canonicalizedHeaders = StringUtilities.Format("Date:{0}\nx-ms-date:{1}\nx-ms-version:{2}", timestamp, timestamp, VersionHeader);

            string TableEndPoint = _account.UriEndpoints["Table"].ToString();

            Uri uri = new Uri(TableEndPoint + "/" + tableName + "()");

            var tableTypeHeaders = new Hashtable();
            tableTypeHeaders.Add("Accept-Charset", "UTF-8");
            tableTypeHeaders.Add("MaxDataServiceVersion", "3.0;NetFx");
            tableTypeHeaders.Add("Content-Type", contentType);
            tableTypeHeaders.Add("DataServiceVersion", "3.0");
            tableTypeHeaders.Add("Prefer", getResponseTypeString(pResponseType));
            tableTypeHeaders.Add("Content-MD5", ContentMD5);

            if (_fiddlerIsAttached)
            { AzureStorageHelper.AttachFiddler(_fiddlerIsAttached, _fiddlerIP, _fiddlerPort); }

            BasicHttpResponse response = new BasicHttpResponse();
            try
            {
                AzureStorageHelper.SetDebugMode(_debug);
                AzureStorageHelper.SetDebugLevel(_debug_level);
                response = AzureStorageHelper.SendWebRequest(uri, authorizationHeader, timestamp, VersionHeader, payload, contentLength, HttpVerb, false, acceptType, tableTypeHeaders);
                _OperationResponseETag = response.ETag;
                _OperationResponseMD5 = response.Content_MD5;
                return response.StatusCode;
            }
            catch (Exception ex)
            {
                _Print_Debug("Exception was cought: " + ex.Message);
                response.StatusCode = HttpStatusCode.Forbidden;
                return response.StatusCode;
            }
        }
        #endregion

        #region QueryTableEnities (overloaded)
        public  HttpStatusCode QueryTableEntities(string tableName, string partitionKey, string rowKey, string query = "", ContType contentType = ContType.applicationIatomIxml, AcceptType acceptType =  AcceptType.applicationIjson, bool useSharedKeyLite = false)
        {
            _Query = query;
            _PartitionKey = partitionKey;
            _RowKey = rowKey;
            return QueryTableEntities(tableName, contentType, acceptType, useSharedKeyLite);
        }
       
        public HttpStatusCode QueryTableEntities(string tableName, string query = "", ContType contentType = ContType.applicationIatomIxml, AcceptType acceptType = AcceptType.applicationIjson, bool useSharedKeyLite = false)
        {
            _Query = query;
            _PartitionKey = "";
            _RowKey = "";
            return QueryTableEntities(tableName, contentType, acceptType, useSharedKeyLite);
        }

        private HttpStatusCode QueryTableEntities(string tableName, ContType pContentType = ContType.applicationIatomIxml, AcceptType pAcceptType = AcceptType.applicationIjson, bool useSharedKeyLite = false)
        {
            OperationResultsClear();
            string pQuery = _Query;
            string partitionKey = _PartitionKey;
            string rowKey = _RowKey;

            string timestamp = GetDateHeader();
            string content = string.Empty;
            //string queryString = "Tables";

            string contentType = getContentTypeString(pContentType);

            string acceptType = getAcceptTypeString(pAcceptType);
            if (pAcceptType == AcceptType.applicationIjson)
            { acceptType = "application/json;odata=minimalmetadata"; }

            string HttpVerb = "GET";
            string ContentMD5 = string.Empty;
            int contentLength = 0;
            byte[] payload = GetBodyBytesAndLength(content, out contentLength);

            string resourceString = string.Empty;
            string queryString = string.Empty;
           
            if (!StringUtilities.IsNullOrEmpty(pQuery))
            {
                queryString = "?" + pQuery;
            }

            if ((!StringUtilities.IsNullOrEmpty(partitionKey) && (!StringUtilities.IsNullOrEmpty(rowKey))))
            {
                resourceString = StringUtilities.Format("{1}(PartitionKey='{2}',RowKey='{3}')", _account.AccountName, tableName, partitionKey, rowKey);
            }
            if ((StringUtilities.IsNullOrEmpty(partitionKey) && (StringUtilities.IsNullOrEmpty(rowKey))))
            {
                resourceString = StringUtilities.Format("{1}()", _account.AccountName, tableName, partitionKey, rowKey);
            }

            var authorizationHeader = CreateTableAuthorizationHeader(payload, StringUtilities.Format("/{0}/{1}", _account.AccountName, resourceString), timestamp, HttpVerb, ContType.applicationIatomIxml, out ContentMD5, useSharedKeyLite);

            // only for tests to provoke an authentication error
            // var authorizationHeader = CreateTableAuthorizationHeader(payload, StringUtilities.Format("/{0}/{1}", "Hallo", resourceString), timestamp, HttpVerb, ContType.applicationIatomIxml, out ContentMD5, useSharedKeyLite);
            
            string TableEndPoint = _account.UriEndpoints["Table"].ToString();

            // Changed by RoSchmi (Tests)
            //IPAddress[] AzureIPs = ALL3075V3_433_Azure.myDNS.ResolveHostname("roschmi01.table.core.windows.net");
            //IPAddress[] AzureIPs = ALL3075V3_433_Azure.myDNS.ResolveHostname("www.google.de");
            

            Uri uri = new Uri(TableEndPoint + "/" + resourceString + queryString);

            var tableTypeHeaders = new Hashtable();
            tableTypeHeaders.Add("MaxDataServiceVersion", "3.0;NetFx");
            tableTypeHeaders.Add("Content-Type", contentType);
            tableTypeHeaders.Add("DataServiceVersion", "3.0;NetFx");
            tableTypeHeaders.Add("Content-MD5", ContentMD5);

            if (_fiddlerIsAttached)
            { AzureStorageHelper.AttachFiddler(_fiddlerIsAttached, _fiddlerIP, _fiddlerPort); }

            BasicHttpResponse response = new BasicHttpResponse();
            try
            {
                AzureStorageHelper.SetDebugMode(_debug);
                AzureStorageHelper.SetDebugLevel(_debug_level);
                response = AzureStorageHelper.SendWebRequest(uri, authorizationHeader, timestamp, VersionHeader, payload, contentLength, HttpVerb, false, acceptType, tableTypeHeaders);
                var entities = ParseResponse(response.Body);
                _OperationResponseBody = response.Body;
                _OperationResponseQueryList = entities;
                //if (entities.Count != 0)
                //{
                    if (entities.Count == 1)
                    {
                        _OperationResponseETag = response.ETag;
                        _OperationResponseSingleQuery = entities[0] as Hashtable;
                    }
               // }

                return response.StatusCode;
            }
            catch (Exception ex)
            {
                _Print_Debug("Exception was cought: " + ex.Message);
                response.StatusCode = HttpStatusCode.Forbidden;
                //return null;
                return response.StatusCode;
            }
        }
        #endregion

        #region DeleteTableEntity
            public HttpStatusCode DeleteTableEntity(string tableName, string partitionKey, string rowKey, ContType pContentType = ContType.applicationIatomIxml, AcceptType pAcceptType = AcceptType.applicationIjson, string ETag = "", bool useSharedKeyLite = false)
            {
                string timestamp = GetDateHeader();
                string content = string.Empty;
                string contentType = getContentTypeString(pContentType);
                string acceptType = getAcceptTypeString(pAcceptType);
                if (pAcceptType == AcceptType.applicationIjson)
                { acceptType = "application/json;odata=minimalmetadata"; }

                string HttpVerb = "DELETE";
                int contentLength = 0;
                byte[] payload = GetBodyBytesAndLength(content, out contentLength);
                string ContentMD5 = string.Empty;
                string matchString = StringUtilities.Format("{1}(PartitionKey='{2}',RowKey='{3}')", _account.AccountName, tableName, partitionKey, rowKey);

                var authorizationHeader = CreateTableAuthorizationHeader(payload, StringUtilities.Format("/{0}/{1}",_account.AccountName, matchString), timestamp, HttpVerb, ContType.applicationIatomIxml, out ContentMD5, useSharedKeyLite);
                
                string TableEndPoint = _account.UriEndpoints["Table"].ToString();

                Uri uri = new Uri(TableEndPoint + "/" + matchString);

                var tableTypeHeaders = new Hashtable();

                
                
                if (ETag == "")
                { 
                    tableTypeHeaders.Add("If-Match", "*");
                }
                else
                {
                    tableTypeHeaders.Add("If-Match", ETag);
                }
                
                tableTypeHeaders.Add("Accept-Charset", "UTF-8");
                tableTypeHeaders.Add("MaxDataServiceVersion", "3.0;NetFx");
                tableTypeHeaders.Add("Content-Type", contentType);
                tableTypeHeaders.Add("DataServiceVersion", "3.0");
                tableTypeHeaders.Add("Content-MD5", ContentMD5);

                if (_fiddlerIsAttached)
                { AzureStorageHelper.AttachFiddler(_fiddlerIsAttached, _fiddlerIP, _fiddlerPort); }

                BasicHttpResponse response = new BasicHttpResponse();
                try
                {
                    AzureStorageHelper.SetDebugMode(_debug);
                    AzureStorageHelper.SetDebugLevel(_debug_level);
                    response = AzureStorageHelper.SendWebRequest(uri, authorizationHeader, timestamp, VersionHeader, payload, contentLength, HttpVerb, false, acceptType, tableTypeHeaders);

                    return response.StatusCode;
                }
                catch (Exception ex)
                {
                    _Print_Debug("Exception was cought: " + ex.Message);
                    response.StatusCode = HttpStatusCode.Forbidden;
                    return response.StatusCode;
                }
            }
            #endregion

        #region UpdateTableEntity

            public HttpStatusCode UpdateTableEntity(string tableName, string partitionKey, string rowKey, TableEntity pEntity, ContType pContentType = ContType.applicationIatomIxml, AcceptType pAcceptType = AcceptType.applicationIjson, ResponseType pResponseType = ResponseType.returnContent, string ETag = "", bool useSharedKeyLite = false)
            {
                OperationResultsClear(); ;
                string timestamp = GetDateHeader();
                string content = string.Empty;

                string contentType = getContentTypeString(pContentType);
                string acceptType = getAcceptTypeString(pAcceptType);

                switch (contentType)
                {
                    case "application/json":
                        {
                            content = pEntity.ReadJson();
                        }
                        break;
                    case "application/atom+xml":
                        {
                           content = StringUtilities.Format("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><entry xmlns:d=\"http://schemas.microsoft.com/ado/2007/08/dataservices\" xmlns:m=\"http://schemas.microsoft.com/ado/2007/08/dataservices/metadata\" xmlns=\"http://www.w3.org/2005/Atom\">" +
                            "<id>http://{0}.table.core.windows.net/{5}(PartitionKey='{2}',RowKey='{3}')</id>" +
                            "<title/><updated>{1}</updated>" +
                            "<author><name /></author>" +
                            "<content type=\"application/atom+xml\"><m:properties><d:PartitionKey>{2}</d:PartitionKey><d:RowKey>{3}</d:RowKey>" +
                            "{4}" +
                            "</m:properties>" +
                            "</content>" +
                            "</entry>", _account.AccountName, timestamp, pEntity.PartitionKey, pEntity.RowKey, GetTableXml(pEntity.Properties), tableName);
                        }
                        break;
                    default:
                        {
                            throw new NotSupportedException("ContentType must be 'application/json' or 'application/atom+xml'");
                        }
                }


                string HttpVerb = "PUT";
                var contentLength = 0;
                var payload = GetBodyBytesAndLength(content, out contentLength);
                string ContentMD5 = string.Empty;

                string matchString = StringUtilities.Format("{1}(PartitionKey='{2}',RowKey='{3}')", _account.AccountName, tableName, partitionKey, rowKey);

                var authorizationHeader = CreateTableAuthorizationHeader(payload, StringUtilities.Format("/{0}/{1}", _account.AccountName, matchString), timestamp, HttpVerb, ContType.applicationIatomIxml, out ContentMD5, useSharedKeyLite);

                string TableEndPoint = _account.UriEndpoints["Table"].ToString();

                Uri uri = new Uri(TableEndPoint + "/" + matchString);

                var tableTypeHeaders = new Hashtable();

                if (ETag == "")
                {
                    tableTypeHeaders.Add("If-Match", "*");
                }
                else
                {
                    tableTypeHeaders.Add("If-Match", ETag);
                }

                tableTypeHeaders.Add("Accept-Charset", "UTF-8");
                tableTypeHeaders.Add("MaxDataServiceVersion", "3.0;NetFx");
                tableTypeHeaders.Add("Content-Type", contentType);
                tableTypeHeaders.Add("DataServiceVersion", "3.0");
                tableTypeHeaders.Add("Prefer", getResponseTypeString(pResponseType));
                tableTypeHeaders.Add("Content-MD5", ContentMD5);

                if (_fiddlerIsAttached)
                { AzureStorageHelper.AttachFiddler(_fiddlerIsAttached, _fiddlerIP, _fiddlerPort); }

                BasicHttpResponse response = new BasicHttpResponse();
                try
                {
                    AzureStorageHelper.SetDebugMode(_debug);
                    AzureStorageHelper.SetDebugLevel(_debug_level);
                    response = AzureStorageHelper.SendWebRequest(uri, authorizationHeader, timestamp, VersionHeader, payload, contentLength, HttpVerb, false, acceptType, tableTypeHeaders);
                    _OperationResponseETag = response.ETag;
                    _OperationResponseMD5 = response.Content_MD5;
                    return response.StatusCode;
                }
                catch (Exception ex)
                {
                    _Print_Debug("Exception was cought: " + ex.Message);
                    response.StatusCode = HttpStatusCode.Forbidden;
                    return response.StatusCode;
                }
            }
        #endregion

        #region FormatEntityXml
            private string FormatEntityXml(string tablename, string partitionKey, string rowKey, DateTime timeStamp, Hashtable tableEntityProperties)
            {
            var timestamp = timeStamp.ToString("yyyy-MM-ddTHH:mm:ss.0000000Z");

            string xml =
                StringUtilities.Format("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><entry xmlns:d=\"http://schemas.microsoft.com/ado/2007/08/dataservices\" xmlns:m=\"http://schemas.microsoft.com/ado/2007/08/dataservices/metadata\" xmlns=\"http://www.w3.org/2005/Atom\">" +
                "<id>http://{0}.table.core.windows.net/{5}(PartitionKey='{2}',RowKey='{3}')</id>" +
                "<title/><updated>{1}</updated><author><name /></author>" +
                "<link />" +
                //"<category term=\"{0}.Tables\" scheme=\"http://schemas.microsoft.com/ado/2007/08/dataservices/scheme\" />" +
                "<content type=\"application/xml\"><m:properties><d:PartitionKey>{2}</d:PartitionKey><d:RowKey>{3}</d:RowKey>" +
                "<d:Timestamp m:type=\"Edm.DateTime\">{1}</d:Timestamp>" +
                "{4}" +
                "</m:properties>" +
                "</content>" +
                "</entry>", _account.AccountName, timestamp, partitionKey, rowKey, GetTableXml(tableEntityProperties), tablename);
            return xml;
            }
            #endregion

        #region GetTableXml
            private string GetTableXml(ArrayList tableEntityProperties)
            {
            string result = string.Empty;
            string prop = string.Empty;
            string key = string.Empty;
            for (int i = 0; i < tableEntityProperties.Count; i++)
            {
                //key = ((string[])tableEntityProperties[i])[1];
                //if ((key != "PartitionKey") && (key != "RowKey"))   // Skip PartitionKey and RowKey
                //{ 
                    prop = ((string[])tableEntityProperties[i])[0];
                    if (prop != null)
                    {
                        result += prop.ToString();
                    }
                //}
            }
            
            return result;
        }
        

        private static string GetTableXml(Hashtable tableEntityProperties)
        {
            string result = string.Empty;
            foreach (var key in tableEntityProperties.Keys)
            {
                var value = tableEntityProperties[key];
                if (value == null) continue;
                var type = value.GetType().Name;
                switch (type)
                {
                    case "DateTime":
                        value = ((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss.0000000Z");
                        break;
                    case "Boolean":
                        value = (Boolean) value ? "true" : "false"; // bool is title case when you call ToString()
                        break;
                }
                result += StringUtilities.Format("<d:{0} m:type=\"Edm.{2}\">{1}</d:{0}>", key, value, type);
            }
            return result;
        }
        #endregion

        #region ParseResponse
           private ArrayList ParseResponse(string xml)
            {
            var results = new ArrayList();
            string entityToken = null;
            var nextStart = 0;
                while (null != (entityToken = NextToken(xml, "<m:properties>", "</m:properties>", nextStart, out nextStart)))
                {
                    var currentObject = new Hashtable();
                    string propertyToken = null;
                    int nextPropertyStart = 0;
                    while (null != (propertyToken = NextToken(entityToken, "<d:", "</d", nextPropertyStart, out nextPropertyStart)))
                    {
                        var parts = propertyToken.Split('>');
                        if (parts.Length != 2) continue;
                        var rawvalue = parts[1];
                        var propertyName = parts[0].Split(' ')[0];

                        var _ = 0;
                        var type = NextToken(propertyToken, "m:type=\"", "\"", 0, out _);
                        if (null == type)
                        {
                            type = "Edm.String";
                        }
                        if (currentObject.Contains(propertyName)) continue;
                        switch (type)
                        {
                            case "Edm.String":
                                currentObject.Add(propertyName, rawvalue);
                            break;
                            case "Edm.DateTime":
                            // not supported
                            break;
                            case "Edm.Int64":
                                currentObject.Add(propertyName, Int64.Parse(rawvalue));
                            break;
                            case "Edm.Int32":
                                currentObject.Add(propertyName, Int32.Parse(rawvalue));
                            break;
                            case "Edm.Double":
                                currentObject.Add(propertyName, Double.Parse(rawvalue));
                            break;
                            case "Edm.Boolean":
                                currentObject.Add(propertyName, rawvalue == "true");
                            break;
                            case "Edm.Guid":
                            // not supported
                            break;
                    }
                }
                results.Add(currentObject);
            }
            return results;
        }

        private string NextToken(string xml, string startToken, string endToken, int startPosition, out int nextStart)
        {
            if (startPosition > xml.Length)
            {
                nextStart = xml.Length;
                return null;
            }
            var start = xml.IndexOf(startToken, startPosition);
            nextStart = 0;
            if (start < 0) return null;
            start += startToken.Length;
            var end = xml.IndexOf(endToken, start);
            if (end < 0) return null;
            nextStart = end + endToken.Length;
            return xml.Substring(start, end - start);           
        }
        #endregion

        #region Shared Access Signature
        private string MD5ComputeHash(byte[] data)
        {
            byte[] hash;
            using (HashAlgorithm csp = new HashAlgorithm(HashAlgorithmType.MD5))
            {
                hash = csp.ComputeHash(data);
            }

            string hashString = ByteExtensions.ToHexString(hash, "");
            return hashString;
        }
        #endregion

        #region CreateTableAuthorizationHeader
        protected string CreateTableAuthorizationHeader(byte[] content, string canonicalResource, string ptimeStamp, string pHttpVerb, ContType pContentType, out string pMD5Hash, bool useSharedKeyLite = false)
        {
            string contentType = getContentTypeString(pContentType);
            pMD5Hash = string.Empty;
            if (!useSharedKeyLite)
            {
                pMD5Hash = MD5ComputeHash(content);
            }

            string toSign = string.Empty;
            if (useSharedKeyLite)
            {
                toSign = StringUtilities.Format("{0}\n{1}", ptimeStamp, canonicalResource);
            }
            else
            {
                toSign = StringUtilities.Format("{0}\n{4}\n{1}\n{2}\n{3}", pHttpVerb, contentType, ptimeStamp, canonicalResource, pMD5Hash);
            }

            string signature;
            var hmac = new HMACSHA256(Convert.FromBase64String(_account.AccountKey));
            var hmacBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign));
            signature = Convert.ToBase64String(hmacBytes).Replace("!", "+").Replace("*", "/"); ;
            if (useSharedKeyLite)
            {
                return "SharedKeyLite " + _account.AccountName + ":" + signature;
            }
            else
            {
                return "SharedKey " + _account.AccountName + ":" + signature;
            }
        }
        #endregion
    }
}

