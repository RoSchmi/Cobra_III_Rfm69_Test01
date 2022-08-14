using System;
using Microsoft.SPOT;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.SPOT.Net.Security;
using System.Threading;
using System.Text;

namespace RoSchmi.Net.SparkPost
{
    public static class SparkPostHttpWebRequestHelper
    {
        private static Object theLock1 = new Object();

        private static bool _fiddlerIsAttached = false;
        private static IPAddress _fiddlerIP = null;
        private static int _fiddlerPort = 8888;

        #region "Debugging"
        private static DebugMode _debug = DebugMode.NoDebug;
        private static DebugLevel _debug_level = DebugLevel.DebugErrors;

        /// <summary>
        /// Represents the debug mode.
        /// </summary>
        public enum DebugMode
        {
            /// <summary>
            /// Use no debugging
            /// </summary>
            NoDebug,

            /// <summary>
            /// Report debugging to Visual Studio debug output
            /// </summary>
            StandardDebug,

            /// <summary>
            /// Re-direct debugging to a given serial port.
            /// Console Debugging
            /// </summary>
            SerialDebug
        };

        /// <summary>
        /// Represents the debug level.
        /// </summary>
        public enum DebugLevel
        {
            /// <summary>
            /// Only debug errors.
            /// </summary>
            DebugErrors,
            /// <summary>
            /// Debug everything.
            /// </summary>
            DebugErrorsPlusMessages,
            /// <summary>
            /// Debug everything.
            /// </summary>
            DebugAll
        };

        private static void _Print_Debug(string message)
        {
            lock (theLock1)
            {
                switch (_debug)
                {
                    //Do nothing
                    case DebugMode.NoDebug:
                        break;

                    //Output Debugging info to the serial port
                    case DebugMode.SerialDebug:
                        //Convert the message to bytes
                        /*
                        byte[] message_buffer = System.Text.Encoding.UTF8.GetBytes(message);
                        _debug_port.Write(message_buffer,0,message_buffer.Length);
                        */
                        break;

                    //Print message to the standard debug output
                    case DebugMode.StandardDebug:
                        Debug.Print(message);
                        break;
                }
            }
        }
        #endregion
        /// <summary>
        /// Set the debugging level.
        /// </summary>
        /// <param name="Debug_Level">The debug level</param>
        public static void SetDebugLevel(DebugLevel Debug_Level)
        {
            lock (theLock1)
            {
                _debug_level = Debug_Level;
            }
        }
        /// <summary>
        /// Set the debugging mode.
        /// </summary>
        /// <param name="Debug_Level">The debug level</param>
        public static void SetDebugMode(DebugMode Debug_Mode)
        {
            lock (theLock1)
            {
                _debug = Debug_Mode;
            }
        }

        public static void AttachFiddler(bool pfiddlerIsAttached, IPAddress pfiddlerIP, int pfiddlerPort)
        {
            lock (theLock1)
            {
                _fiddlerIsAttached = pfiddlerIsAttached;
                _fiddlerIP = pfiddlerIP;
                _fiddlerPort = pfiddlerPort;
            }
        }
        

        /// <summary>
        /// Sends a Web Request prepared for Allnet Device
        /// </summary>
        /// <param name="url"></param>
        /// <param name="authHeader"></param>
        /// <param name="fileBytes"></param>
        /// <param name="contentLength"></param>
        /// <param name="httpVerb"></param>
        /// <param name="expect100Continue"></param>
        /// <param name="additionalHeaders"></param>
        /// <returns></returns>
        ///
        public static SparkPostBasicHttpWebResponse SendWebRequest(Uri url, X509Certificate[] caCerts, string authHeader, byte[] payload = null, int contentLength = 0, string httpVerb = "GET", bool expect100Continue = false, Hashtable additionalHeaders = null)
        {
            string userAgent = "RsNetmfHttpClient";
            string responseBody = "";
            HttpStatusCode responseStatusCode = HttpStatusCode.Ambiguous;
            string _date = DateTime.Now.ToString();

            lock (theLock1)
            {
                if (_debug_level == DebugLevel.DebugErrorsPlusMessages)
                {
                    _Print_Debug("Time of request: " + _date);
                    _Print_Debug("Url: " + url.AbsoluteUri);
                }
            }

            if (url.Scheme == "https")
            {
                #region Code for request using https (using Socket)
                try
                {
                    string headerString = PrepareHeaderString(url, authHeader, payload, contentLength, httpVerb, userAgent, expect100Continue, additionalHeaders);

                    IPHostEntry hostEntry = Dns.GetHostEntry(url.Host);

                    if (hostEntry.AddressList.Length < 1)
                    {
                        lock (theLock1)
                        {
                            _Print_Debug("An error occured. Url " + url.Host + " could not be resolved by DNS!");
                        }
                        throw new Exception("Url could not be resolved by DNS");
                    }

                    IPAddress hostIP = hostEntry.AddressList[0];

                    lock (theLock1)
                    {
                        if (_debug_level == DebugLevel.DebugErrorsPlusMessages)
                        {
                            _Print_Debug("\r\nRemote Host-IP of " + url.Host + " = " + hostIP.ToString());
                        }
                    }
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint ep = new IPEndPoint(hostIP, 443);

                    socket.Connect(ep);
                    SslStream sslStream = new SslStream(socket);
                    sslStream.AuthenticateAsClient(hostIP.ToString(), null, SslVerification.CertificateRequired, SslProtocols.TLSv1);
                    Char[] content = Encoding.UTF8.GetChars(payload);
                    
                    string httpRequest = headerString + new string(content);
                    Byte[] httpResponseArray = Encoding.UTF8.GetBytes(httpRequest);
                    sslStream.Write(httpResponseArray, 0, httpResponseArray.Length);

                    sslStream.ReadTimeout = 5000;
                    
                    byte[] buffer = new byte[2048];
                    StringBuilder messageData = new StringBuilder();

                    // Wait for socket to have data, Not sure if this is needed
                    DateTime timeoutAt = DateTime.Now.AddSeconds(5);
                    while (socket.Available == 0 && DateTime.Now < timeoutAt)
                    {
                        Thread.Sleep(100);
                    }
                    if (socket.Available == 0)
                    {
                        lock (theLock1)
                        {
                            _Print_Debug("No data from remote host");
                        }
                        throw new Exception("No data from remote host");
                    }
                    #region Read the response from the socket
                    int bytes = 0;
                    do
                    {
                        try
                        {
                            bytes = sslStream.Read(buffer, 0, buffer.Length);
                        }
                        catch (Exception ex)
                        {
                            lock (theLock1)
                            {
                                _Print_Debug("Error when reading fromm SSL" + ex.Message);
                            }
                            throw new Exception("Error when reading fromm SSL" + ex.Message);
                        }
                        if (bytes > 0)
                        {
                            char[] chars = Encoding.UTF8.GetChars(buffer, 0, bytes);
                            messageData.Append(chars);
                        }
                       
                        Thread.Sleep(100);
                        
                       // Debug.Print("Read bytes: " + bytes.ToString() + "TotalBytes: " + messageData.Length);
                    }
                    while (bytes != 0);
                    #endregion

                    string httpResponse = messageData.ToString();
                    string responseHeader = string.Empty; 
                    string responseContent = string.Empty;
                    string responseFirstLine = string.Empty;
                    string httpResponseStatusCode = string.Empty;
                    bool _responseIsValid = false;
                    
                    try
                    { responseContent = httpResponse.Substring(httpResponse.IndexOf("\r\n\r\n") + 4); 
                    }
                    catch { }

                    try
                    {
                        responseHeader = httpResponse.Substring(0, httpResponse.IndexOf("\r\n\r\n"));
                    }
                    catch { }
                   
                    try
                    {
                        responseFirstLine = httpResponse.Substring(responseHeader.IndexOf("HTTP/1.1"), responseHeader.IndexOf("\r\n"));
                    }
                    catch { }
                    
                    try
                    {
                        int firstIndex = responseFirstLine.IndexOf((char)0x20);
                        int secondIndex = responseFirstLine.IndexOf((char)0x20, firstIndex + 1);
                        httpResponseStatusCode = responseFirstLine.Substring(firstIndex + 1, secondIndex - (firstIndex +1) );
                    }
                    catch { }

                    _responseIsValid = (httpResponseStatusCode != string.Empty);

                    if (_responseIsValid)
                    {
                        try
                        {
                            responseStatusCode = (HttpStatusCode)(int.Parse(httpResponseStatusCode));
                            responseBody = responseContent;
                        }
                        catch { }; 
                    }

                    return new SparkPostBasicHttpWebResponse() { ResponseIsValid = _responseIsValid, Date = _date, Body = responseBody, StatusCode = responseStatusCode };
            }
            
            catch (Exception ex2)
            {
                lock (theLock1)
                {
                    _Print_Debug("Exception in HttpWebRequest.GetResponse(): " + ex2.Message);
                }
                return new SparkPostBasicHttpWebResponse() { ResponseIsValid = false, Date = _date, Body = responseBody, StatusCode = responseStatusCode };
            }
        #endregion
            }
            else
            {
                #region Code for request using http with HttpWebRequest Class
                try
                {
                    HttpWebRequest request = PrepareRequest(url, authHeader, payload, contentLength, httpVerb, userAgent, expect100Continue, additionalHeaders);
                    
                    if (request != null)
                    {
                        // Assign the certificates. The value must not be null if the
                        // connection is HTTPS.
                        request.HttpsAuthentCerts = caCerts;

                        //request.Proxy = new WebProxy();
                        //Evtl. set a WebProxy
                        //HttpWebRequest.DefaultWebProxy = new WebProxy("4.2.2.2", true);

                        // Evtl. set request.KeepAlive to use a persistent connection.
                        request.KeepAlive = false;
                        request.Timeout = 10000;               // timeout 10 sec = standard = 100
                        request.ReadWriteTimeout = 10000;      // timeout 10 sec, standard = 300

                        //Debug.GC(false);

                        // This is needed since there is an exception if the GetRequestStream method is called with GET or HEAD
                        if ((httpVerb != "GET") && (httpVerb != "HEAD"))
                        {
                            using (Stream requestStream = request.GetRequestStream())
                            {
                                requestStream.Write(payload, 0, contentLength);
                            }
                        }
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            if (response != null)
                            {
                                if (response.Headers.Count > 0)
                                {
                                    try
                                    {
                                        _date = response.GetResponseHeader("Date");
                                    }
                                    catch { }
                                }
                                responseStatusCode = response.StatusCode;

                                using (Stream dataStream = response.GetResponseStream())
                                {
                                    using (StreamReader reader = new StreamReader(dataStream))
                                    {
                                        responseBody = reader.ReadToEnd();
                                        reader.Close();
                                    }
                                }
                                //Report all incomming data to the debug
                                lock (theLock1)
                                {
                                    if (_debug_level == DebugLevel.DebugErrorsPlusMessages)
                                    {
                                        _Print_Debug(responseBody);
                                    }
                                }

                                if (response.StatusCode == HttpStatusCode.Forbidden)
                                {
                                    lock (theLock1)
                                    {
                                        _Print_Debug("Problem with signature. Check next debug statement for stack");
                                    }
                                    throw new WebException("Forbidden", null, WebExceptionStatus.TrustFailure, response);
                                }
                                response.Close();
                                if (responseBody == null)
                                    responseBody = "No body content";

                                return new SparkPostBasicHttpWebResponse() { ResponseIsValid = true, Date = _date, Body = responseBody, StatusCode = responseStatusCode };
                            }
                            else
                            {
                                return new SparkPostBasicHttpWebResponse() { ResponseIsValid = false, Date = _date, Body = "No response on Http-Request", StatusCode = responseStatusCode };
                            }
                        }
                    }
                    else
                    {
                        lock (theLock1)
                        {
                            _Print_Debug("Failure: Request is null");
                        }
                        return new SparkPostBasicHttpWebResponse() { ResponseIsValid = false, Date = _date, Body = "Failure, Request was null", StatusCode = responseStatusCode };
                    }
                }
                catch (WebException ex)
                {
                    if (ex.Response != null)
                    {
                        lock (theLock1)
                        {
                            _Print_Debug("An error occured. Status code:" + ((HttpWebResponse)ex.Response).StatusCode);
                        }
                        responseStatusCode = ((HttpWebResponse)ex.Response).StatusCode;
                        using (Stream stream = ex.Response.GetResponseStream())
                        {
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                StringBuilder sB = new StringBuilder("");
                                Char[] chunk = new char[20];

                                while (sr.Peek() > -1)
                                {
                                    int readBytes = sr.Read(chunk, 0, chunk.Length);
                                    sB.Append(chunk, 0, readBytes);
                                }
                                responseBody = sB.ToString();
                                lock (theLock1)
                                {
                                    _Print_Debug(responseBody);
                                }
                                /*
                                var s = sr.ReadToEnd();
                                lock (theLock1)
                                {
                                    _Print_Debug(s);
                                }
                                responseBody = s;
                                */
                                return new SparkPostBasicHttpWebResponse() { ResponseIsValid = true, Date = _date, Body = responseBody, StatusCode = responseStatusCode };
                            }
                        }
                    }
                    else
                    {
                        lock (theLock1)
                        {
                            _Print_Debug("An error occured. Response: " + ((ex.Response == null) ? "is null" : "is not null ") + "  Status code:" + ex.Status);
                        }
                        return new SparkPostBasicHttpWebResponse() { ResponseIsValid = false, Date = _date, Body = "No response on Http-Request", StatusCode = responseStatusCode };
                    }
                }
                catch (Exception ex2)
                {
                    lock (theLock1)
                    {
                        _Print_Debug("Exception in HttpWebRequest.GetResponse(): " + ex2.Message);
                    }
                    return new SparkPostBasicHttpWebResponse() { ResponseIsValid = false, Date = _date, Body = responseBody, StatusCode = responseStatusCode };
                }
            #endregion
            }
        
        }

        /// <summary>
        /// Prepares a string containig the header for a http-request using sockets 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="authHeader"></param>
        /// <param name="fileBytes"></param>
        /// <param name="contentLength"></param>
        /// <param name="httpVerb"></param>
        /// <param name="expect100Continue"></param>
        /// <param name="additionalHeaders"></param>
        /// <returns></returns>

        private static string PrepareHeaderString(Uri url, string authHeader, byte[] fileBytes, int contentLength, string httpVerb, string userAgent, bool expect100Continue = false, Hashtable additionalHeaders = null)
        {
            StringBuilder headerString = new StringBuilder(httpVerb + " " + url.AbsoluteUri + " HTTP/1.1\r\n" + "User-Agent: " + userAgent + "\r\n");

            if (additionalHeaders != null)
            {
                foreach (var additionalHeader in additionalHeaders.Keys)
                {
                    headerString.Append(additionalHeader.ToString() + ": " + additionalHeaders[additionalHeader].ToString() + "\r\n");
                }
            }
            headerString.Append("authorization: " + authHeader + "\r\n");
            headerString.Append("Host: " + url.Host + "\r\n");
            headerString.Append("Content-Length: " + contentLength + "\r\n");
            if (expect100Continue)
            {
                headerString.Append("Expect: " + "100-continue\r\n");
            }
            headerString.Append("Connection: " + "Close\r\n");
            headerString.Append("\r\n");  // End of header

            return headerString.ToString();
        }


        /// <summary>
        /// Prepares a HttpWebRequest with required headers, Authorization and others
        /// </summary>
        /// <param name="url"></param>
        /// <param name="authHeader"></param>
        /// <param name="fileBytes"></param>
        /// <param name="contentLength"></param>
        /// <param name="httpVerb"></param>
        /// <param name="expect100Continue"></param>
        /// <param name="additionalHeaders"></param>
        /// <returns></returns>

        private static HttpWebRequest PrepareRequest(Uri url, string authHeader, byte[] fileBytes, int contentLength, string httpVerb, string userAgent, bool expect100Continue = false, Hashtable additionalHeaders = null)
        {
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)WebRequest.Create(url);
            request.Method = httpVerb;
            request.ContentLength = contentLength;
            request.UserAgent = userAgent;
            request.Headers.Add("authorization", authHeader);

            if (expect100Continue)
            {
                request.Expect = "100-continue";
            }
            if (additionalHeaders != null)
            {
                foreach (var additionalHeader in additionalHeaders.Keys)
                {
                    request.Headers.Add(additionalHeader.ToString(), additionalHeaders[additionalHeader].ToString());
                }
            }

            //*******************************************************
            // To use Fiddler as WebProxy include this code segment
            // Use the local IP-Address of the PC where Fiddler is running
            // See here how to configurate Fiddler; -http://blog.devmobile.co.nz/2013/01/09/netmf-http-debugging-with-fiddler
            lock (theLock1)
            {
                if (_fiddlerIsAttached)
                {
                    request.Proxy = new WebProxy(_fiddlerIP.ToString(), _fiddlerPort);
                }
            }
            //**********

            //PrintKeysAndValues(request.Headers);

            return request;
        }

        public static void PrintKeysAndValues(WebHeaderCollection myHT)
        {
            lock (theLock1)
            {
                string[] allKeys = myHT.AllKeys;
                _Print_Debug("\r\nThe request was sent with the following headers");
                foreach (string Key in allKeys)
                {
                    _Print_Debug(Key + ":");
                }
                _Print_Debug("\r\n");
            }
        }

        public static void PrintKeysAndValues(Hashtable myHT)
        {
            lock (theLock1)
            {
                _Print_Debug("\r\nThe request was sent with the following headers");
                foreach (DictionaryEntry de in myHT)
                {
                    _Print_Debug(de.Key + ":" + de.Value);
                }
                _Print_Debug("\r\n");
            }
        }
    }
}
