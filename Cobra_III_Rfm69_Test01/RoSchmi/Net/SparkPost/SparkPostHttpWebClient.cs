using System;
using Microsoft.SPOT;
using System.Threading;
using System.Net;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Collections;

//using ALL3075V3_433_Azure;

namespace RoSchmi.Net.SparkPost
{
    class SparkPostHttpWebClient
    {
        private Uri _AppUri;
        private string _User = "";
        private string _PassWord = "";

        private byte[] payload = new byte[0];
        private Hashtable additionalHeaders;
        private bool expect100Continue = true;

        private StringBuilder uri;
        private Uri requestUri;
        private string authorization = null;

        //Root CA Certificate needed to validate HTTPS servers.
        private X509Certificate[] caCerts;

        private bool _fiddlerIsAttached = false;
        private IPAddress _fiddlerIP = null;
        private int _fiddlerPort = 8888;

        #region public attachFiddler
        public void attachFiddler(bool pfiddlerIsAttached, IPAddress pfiddlerIP, int pfiddlerPort)
        {
            this._fiddlerIsAttached = pfiddlerIsAttached;
            this._fiddlerIP = pfiddlerIP;
            this._fiddlerPort = pfiddlerPort;
        }
        #endregion

        #region "Debugging"
        private SparkPostHttpWebRequestHelper.DebugMode _debug = SparkPostHttpWebRequestHelper.DebugMode.StandardDebug;
        private SparkPostHttpWebRequestHelper.DebugLevel _debug_level = SparkPostHttpWebRequestHelper.DebugLevel.DebugAll;
        #endregion

        #region _Print_Debug
        private void _Print_Debug(string message)
        {
            switch (_debug)
            {
                //Do nothing
                case SparkPostHttpWebRequestHelper.DebugMode.NoDebug:
                    break;

                //Output Debugging info to the serial port
                case SparkPostHttpWebRequestHelper.DebugMode.SerialDebug:
                    //Convert the message to bytes
                    /*
                    byte[] message_buffer = System.Text.Encoding.UTF8.GetBytes(message);
                    _debug_port.Write(message_buffer,0,message_buffer.Length);
                    */
                    break;

                //Print message to the standard debug output
                case SparkPostHttpWebRequestHelper.DebugMode.StandardDebug:
                    Debug.Print(message);
                    break;
            }
        }
        #endregion

        #region Set the debugging level
        /// <summary>
        /// Set the debugging level.
        /// </summary>
        /// <param name="Debug_Level">The debug level</param>
        public void SetDebugLevel(SparkPostHttpWebRequestHelper.DebugLevel Debug_Level)
        {
            this._debug_level = Debug_Level;
        }
        /// <summary>
        /// Set the debugging mode.
        /// </summary>
        /// <param name="Debug_Level">The debug level</param>
        public void SetDebugMode(SparkPostHttpWebRequestHelper.DebugMode Debug_Mode)
        {
            this._debug = Debug_Mode;
        }
        #endregion

        #region Constructor
        public SparkPostHttpWebClient(SparkPostAccount account, X509Certificate[] Certificat, SparkPostHttpWebRequestHelper.DebugMode debugMode, SparkPostHttpWebRequestHelper.DebugLevel debugLevel)
            : this(new Uri(account.UriEndpoints["Default"].ToString()), Certificat, account.AccountUser, account.AccountPassword, account.AccountUseHttps, debugMode, debugLevel)
        {
        }

        private SparkPostHttpWebClient(Uri pAppUri, X509Certificate[] pCertificat, string pUser, string pPassword, bool pUseHttps, SparkPostHttpWebRequestHelper.DebugMode debugMode, SparkPostHttpWebRequestHelper.DebugLevel debugLevel)   // Constructor
        {
            this._AppUri = pAppUri;
            this._User = pUser;
            this._PassWord = pPassword;
            this.uri = new StringBuilder();
            this._debug = debugMode;
            this._debug_level = debugLevel;
            this.caCerts  = pCertificat;

            if (pAppUri == null)
                throw new ArgumentNullException("applicationUri parameter cannot be null");

            if ((pUser != null) && (pPassword != null))
            {
                this.authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(pUser + ":" + pPassword));
            }
            SparkPostHttpWebRequestHelper.SetDebugMode(_debug);
            SparkPostHttpWebRequestHelper.SetDebugLevel(_debug_level);
           
        }
        #endregion

        #region sendEmail
        public void sendEmail(string pStringPayload)
        {
            this.payload = Encoding.UTF8.GetBytes(pStringPayload);
            this.additionalHeaders = new Hashtable(1);
            this.additionalHeaders.Add("Content-Type", "text/plain; charset=utf-8");
            this.expect100Continue = false;  // true results in an exception in SparkPostHttpWebRequestHelper
            
            Thread sendEmailThread = new Thread(new ThreadStart(runEmailSendThread));
            sendEmailThread.Start();
        }
        #endregion
        
        #region Thread runEmailSendThread
        private void runEmailSendThread()
        {
            // perform the post request in a new thread
            string HttpVerb = "POST";
            this.uri.Clear();
            this.uri.Append(this._AppUri.AbsoluteUri).Append("transmissions");

            this.requestUri = new Uri(this.uri.ToString());

            if (_fiddlerIsAttached)
            { SparkPostHttpWebRequestHelper.AttachFiddler(_fiddlerIsAttached, _fiddlerIP, _fiddlerPort); }

            SparkPostBasicHttpWebResponse response = new SparkPostBasicHttpWebResponse();
            try
            {
                SparkPostHttpWebRequestHelper.SetDebugMode(_debug);
                SparkPostHttpWebRequestHelper.SetDebugLevel(_debug_level);
                
                response = SparkPostHttpWebRequestHelper.SendWebRequest(requestUri, caCerts, _PassWord, payload, payload.Length, HttpVerb, expect100Continue, additionalHeaders);
                
                var theResponse = response.StatusCode;

                
                this.OnSparkPostCommandSent(this, new SparkPostSendEventArgs(response.StatusCode, response.Body));
               
            }
            catch (Exception ex)
            {
                _Print_Debug("Exception was cought: " + ex.Message);
                response.StatusCode = HttpStatusCode.Forbidden;
                var theResponse = response.StatusCode;
                this.OnSparkPostCommandSent(this, new SparkPostSendEventArgs(response.StatusCode, null));
            }
        }
        #endregion

        #region OnSparkPostCommandSent
        private void OnSparkPostCommandSent(SparkPostHttpWebClient sender, SparkPostSendEventArgs e)
        {
            if (this.onSparkPostCommandSent == null)
            {
                this.onSparkPostCommandSent = this.OnSparkPostCommandSent;
            }
            //Changed by RoSchmi
            //if (Program.CheckAndInvoke(this.SparkPostCommandSent, this.onSparkPostCommandSent, sender, e))

            this.SparkPostCommandSent(sender, e);
        }
        #endregion

        #region Delegate
        /// <summary>
        /// The delegate that is used to handle the SparkPostWebClientEvent
        /// </summary>
        /// <param name="sender">The <see cref="SparkPostHttpWebClient"/> object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        
        public delegate void SparkPostWebClientEventHandler(SparkPostHttpWebClient sender, SparkPostSendEventArgs e);

        /// <summary>
        /// Raised when emails were sent successfully to SparkPost
        /// </summary>

        public event SparkPostWebClientEventHandler SparkPostCommandSent;

        private SparkPostWebClientEventHandler onSparkPostCommandSent;
        #endregion

        #region SparkPostSendEventArgs
        /// <summary>
        /// Event arguments for the SparkPostSentEvent
        /// </summary>
        public class SparkPostSendEventArgs : EventArgs
        {
            /// <summary>
            /// The HttpStatusCode of the response
            /// </summary>
            /// 
            public HttpStatusCode returnCode
            { get; private set; }

            /// <summary>
            /// The time of the completed http response
            /// </summary>
            public string body
            { get; private set; }

            internal SparkPostSendEventArgs(HttpStatusCode pReturnCode, string pBody)
            {
            
                this.returnCode = pReturnCode;
                this.body = pBody;
            }

        }
        #endregion
    }

}
