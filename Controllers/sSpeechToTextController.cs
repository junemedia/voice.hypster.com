using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using System.Collections.Specialized;
using System.Web.UI.HtmlControls;
using hypster_voice.Code;


namespace hypster_voice.Controllers
{
    public class sSpeechToTextController : Controller
    {

        #region Class variables and Data structures
        /// <summary>
        /// Temporary variables for processing
        /// </summary>
        private string fqdn, accessTokenFilePath;

        /// <summary>
        /// Temporary variables for processing
        /// </summary>
        private string apiKey, secretKey, accessToken, scope, refreshToken, refreshTokenExpiryTime, accessTokenExpiryTime, bypassSSL;

        /// <summary>
        /// variable for having the posted file.
        /// </summary>
        private string SpeechFilesDir;

        /// <summary>
        /// Gets or sets the value of refreshTokenExpiresIn
        /// </summary>
        private int refreshTokenExpiresIn;


        public string speechErrorMessage = string.Empty;
        public string speechSuccessMessage = string.Empty;
        public SpeechResponse speechResponseData = null;

        /// <summary>
        /// Access Token Types
        /// </summary>
        public enum AccessType
        {
            /// <summary>
            /// Access Token Type is based on Client Credential Mode
            /// </summary>
            ClientCredential,

            /// <summary>
            /// Access Token Type is based on Refresh Token
            /// </summary>
            RefreshToken
        }
        #endregion





        protected string Curr_Client = "";

        public string Curr_Return_Str = "";

        public string SpeechTest = "";

        public string SpeechContext = "";

        private string CurrUser = "";



        //
        // GET: /sSpeechToText/

        public ActionResult Index()
        {
            return View();
        }




        public string getText()
        {
            if (Request.QueryString["qq"] != null)
            {
                SpeechTest = Request.QueryString["qq"].ToString() + ".wav";
            }

            if (Request.QueryString["context"] != null)
            {
                SpeechContext = Request.QueryString["context"].ToString();
            }


            System.Web.HttpBrowserCapabilitiesBase browser = Request.Browser;
            Curr_Client = browser.Browser;


            BypassCertificateError();
 
            this.ReadConfigFile();
 
            //this.setXArgsContent();

            BtnSubmit_Click();


            if ((speechResponseData.Recognition.NBest != null) && (speechResponseData.Recognition.NBest.Count > 0))
            {
                foreach (NBest nbest in speechResponseData.Recognition.NBest)
                {
                    Curr_Return_Str = nbest.ResultText;
                }
            }


            return "sSpeechRes({'result': '" +  Curr_Return_Str.Replace(".","") + "'});";
        }













         /// <summary>
        /// Method that calls SpeechToText api when user clicked on submit button
        /// </summary>
        /// <param name="sender">sender that invoked this event</param>
        /// <param name="e">eventargs of the button</param>
        public  void BtnSubmit_Click()
        {
            try
            {
                bool IsValid = true;
 
                IsValid = this.ReadAndGetAccessToken(ref speechErrorMessage);
                if (IsValid == false)
                {
                    speechErrorMessage = "Unable to get access token";
                    return;
                }
                var chunkValue = false;


                var speechFile = ConfigurationManager.AppSettings["voiceSavePath"] + SpeechTest;
                this.ConvertToSpeech(this.fqdn + "/speech/v3/speechToText", this.accessToken, "Generic", ConfigurationManager.AppSettings["X-ArgGeneric"].ToString(), speechFile, chunkValue);
            }
            catch (Exception ex)
            {
            speechErrorMessage = ex.Message;
            return;
            }   
        }






        #region Access Token Related Functions

        /// <summary>
        /// Read parameters from configuraton file
        /// </summary>
        /// <returns>true/false; true if all required parameters are specified, else false</returns>
        private bool ReadConfigFile()
        {
            this.accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePathSpeech"];
            if (string.IsNullOrEmpty(this.accessTokenFilePath))
            {
                this.accessTokenFilePath = "~\\SpeechApp1AccessToken.txt";
            }

            this.fqdn = ConfigurationManager.AppSettings["FQDN"];
            if (string.IsNullOrEmpty(this.fqdn))
            {
                speechErrorMessage = "FQDN is not defined in configuration file";
                return false;
            }

            this.apiKey = ConfigurationManager.AppSettings["api_key"];
            if (string.IsNullOrEmpty(this.apiKey))
            {
                speechErrorMessage = "api_key is not defined in configuration file";
                return false;
            }

            this.secretKey = ConfigurationManager.AppSettings["secret_key"];
            if (string.IsNullOrEmpty(this.secretKey))
            {
                speechErrorMessage = "secret_key is not defined in configuration file";
                return false;
            }

            this.scope = ConfigurationManager.AppSettings["scopeSPEECH"];
            if (string.IsNullOrEmpty(this.scope))
            {
                this.scope = "SPEECH";
            }

            string refreshTokenExpires = ConfigurationManager.AppSettings["refreshTokenExpiresIn"];
            if (!string.IsNullOrEmpty(refreshTokenExpires))
            {
                this.refreshTokenExpiresIn = Convert.ToInt32(refreshTokenExpires);
            }
            else
            {
                this.refreshTokenExpiresIn = 24;
            }
           

            return true;
        }



        private void setXArgsContent()
        {   
        }



        /// <summary>
        /// This function reads the Access Token File and stores the values of access token, expiry seconds
        /// refresh token, last access token time and refresh token expiry time
        /// This funciton returns true, if access token file and all others attributes read successfully otherwise returns false
        /// </summary>
        /// <param name="panelParam">Panel Details</param>
        /// <returns>Returns boolean</returns>    
        private bool ReadAccessTokenFile(ref string message)
        {
            FileStream fileStream = null;
            StreamReader streamReader = null;
            try
            {
                fileStream = new FileStream(Request.MapPath(this.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Read);
                streamReader = new StreamReader(fileStream);
                this.accessToken = streamReader.ReadLine();
                this.accessTokenExpiryTime = streamReader.ReadLine();
                this.refreshToken = streamReader.ReadLine();
                this.refreshTokenExpiryTime = streamReader.ReadLine();
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
            finally
            {
                if (null != streamReader)
                {
                    streamReader.Close();
                }

                if (null != fileStream)
                {
                    fileStream.Close();
                }
            }

            if ((this.accessToken == null) || (this.accessTokenExpiryTime == null) || (this.refreshToken == null) || (this.refreshTokenExpiryTime == null))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// This function validates the expiry of the access token and refresh token.
        /// function compares the current time with the refresh token taken time, if current time is greater then returns INVALID_REFRESH_TOKEN
        /// function compares the difference of last access token taken time and the current time with the expiry seconds, if its more, returns INVALID_ACCESS_TOKEN    
        /// otherwise returns VALID_ACCESS_TOKEN
        /// </summary>
        /// <returns>string, which specifies the token validity</returns>
        private string IsTokenValid()
        {
            try
            {
                DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();
                if (currentServerTime >= DateTime.Parse(this.accessTokenExpiryTime))
                {
                    if (currentServerTime >= DateTime.Parse(this.refreshTokenExpiryTime))
                    {
                        return "INVALID_ACCESS_TOKEN";
                    }
                    else
                    {
                        return "REFRESH_TOKEN";
                    }
                }
                else
                {
                    return "VALID_ACCESS_TOKEN";
                }
            }
            catch
            {
                return "INVALID_ACCESS_TOKEN";
            }
        }

        /// <summary>
        /// This function get the access token based on the type parameter type values.
        /// If type value is 1, access token is fetch for client credential flow
        /// If type value is 2, access token is fetch for client credential flow based on the exisiting refresh token
        /// </summary>
        /// <param name="type">Type as integer</param>
        /// <param name="panelParam">Panel details</param>
        /// <returns>Return boolean</returns>
        private bool GetAccessToken(AccessType type, ref string message)
        {
            FileStream fileStream = null;
            Stream postStream = null;
            StreamWriter streamWriter = null;

            // This is client credential flow
            if (type == AccessType.ClientCredential)
            {
                try
                {
                    DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();

                    WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create(string.Empty + this.fqdn + "/oauth/v4/token");
                    accessTokenRequest.Method = "POST";
                    string oauthParameters = string.Empty;
                    if (type == AccessType.ClientCredential)
                    {
                        oauthParameters = "client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&grant_type=client_credentials&scope=" + this.scope;
                    }
                    else
                    {
                        oauthParameters = "grant_type=refresh_token&client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&refresh_token=" + this.refreshToken;
                    }

                    accessTokenRequest.ContentType = "application/x-www-form-urlencoded";

                    UTF8Encoding encoding = new UTF8Encoding();
                    byte[] postBytes = encoding.GetBytes(oauthParameters);
                    accessTokenRequest.ContentLength = postBytes.Length;

                    postStream = accessTokenRequest.GetRequestStream();
                    postStream.Write(postBytes, 0, postBytes.Length);

                    WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
                    using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
                    {
                        string jsonAccessToken = accessTokenResponseStream.ReadToEnd().ToString();
                        JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();

                        AccessTokenResponse deserializedJsonObj = (AccessTokenResponse)deserializeJsonObject.Deserialize(jsonAccessToken, typeof(AccessTokenResponse));

                        this.accessToken = deserializedJsonObj.access_token;
                        this.accessTokenExpiryTime = currentServerTime.AddSeconds(Convert.ToDouble(deserializedJsonObj.expires_in)).ToString();
                        this.refreshToken = deserializedJsonObj.refresh_token;

                        DateTime refreshExpiry = currentServerTime.AddHours(this.refreshTokenExpiresIn);

                        if (deserializedJsonObj.expires_in.Equals("0"))
                        {
                            int defaultAccessTokenExpiresIn = 100; // In Yearsint yearsToAdd = 100;
                            this.accessTokenExpiryTime = currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToLongDateString() + " " + currentServerTime.AddYears(defaultAccessTokenExpiresIn).ToLongTimeString();
                        }

                        this.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();

                        fileStream = new FileStream(Request.MapPath(this.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write);
                        streamWriter = new StreamWriter(fileStream);
                        streamWriter.WriteLine(this.accessToken);
                        streamWriter.WriteLine(this.accessTokenExpiryTime);
                        streamWriter.WriteLine(this.refreshToken);
                        streamWriter.WriteLine(this.refreshTokenExpiryTime);

                        // Close and clean up the StreamReader
                        accessTokenResponseStream.Close();
                        return true;
                    }
                }
                catch (WebException we)
                {
                    string errorResponse = string.Empty;

                    try
                    {
                        using (StreamReader sr2 = new StreamReader(we.Response.GetResponseStream()))
                        {
                            errorResponse = sr2.ReadToEnd();
                            sr2.Close();
                        }
                    }
                    catch
                    {
                        errorResponse = "Unable to get response";
                    }

                    message = errorResponse + Environment.NewLine + we.ToString();
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    return false;
                }
                finally
                {
                    if (null != postStream)
                    {
                        postStream.Close();
                    }

                    if (null != streamWriter)
                    {
                        streamWriter.Close();
                    }

                    if (null != fileStream)
                    {
                        fileStream.Close();
                    }
                }
            }
            else if (type == AccessType.RefreshToken)
            {
                try
                {
                    DateTime currentServerTime = DateTime.UtcNow.ToLocalTime();

                    WebRequest accessTokenRequest = System.Net.HttpWebRequest.Create(string.Empty + this.fqdn + "/oauth/v4/token");
                    accessTokenRequest.Method = "POST";

                    string oauthParameters = "grant_type=refresh_token&client_id=" + this.apiKey + "&client_secret=" + this.secretKey + "&refresh_token=" + this.refreshToken;
                    accessTokenRequest.ContentType = "application/x-www-form-urlencoded";

                    UTF8Encoding encoding = new UTF8Encoding();
                    byte[] postBytes = encoding.GetBytes(oauthParameters);
                    accessTokenRequest.ContentLength = postBytes.Length;

                    postStream = accessTokenRequest.GetRequestStream();
                    postStream.Write(postBytes, 0, postBytes.Length);

                    WebResponse accessTokenResponse = accessTokenRequest.GetResponse();
                    using (StreamReader accessTokenResponseStream = new StreamReader(accessTokenResponse.GetResponseStream()))
                    {
                        string accessTokenJSon = accessTokenResponseStream.ReadToEnd().ToString();
                        JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();

                        AccessTokenResponse deserializedJsonObj = (AccessTokenResponse)deserializeJsonObject.Deserialize(accessTokenJSon, typeof(AccessTokenResponse));
                        this.accessToken = deserializedJsonObj.access_token.ToString();
                        DateTime accessTokenExpiryTime = currentServerTime.AddMilliseconds(Convert.ToDouble(deserializedJsonObj.expires_in.ToString()));
                        this.refreshToken = deserializedJsonObj.refresh_token.ToString();

                        fileStream = new FileStream(Request.MapPath(this.accessTokenFilePath), FileMode.OpenOrCreate, FileAccess.Write);
                        streamWriter = new StreamWriter(fileStream);
                        streamWriter.WriteLine(this.accessToken);
                        streamWriter.WriteLine(this.accessTokenExpiryTime);
                        streamWriter.WriteLine(this.refreshToken);

                        // Refresh token valids for 24 hours
                        DateTime refreshExpiry = currentServerTime.AddHours(24);
                        this.refreshTokenExpiryTime = refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString();
                        streamWriter.WriteLine(refreshExpiry.ToLongDateString() + " " + refreshExpiry.ToLongTimeString());

                        accessTokenResponseStream.Close();
                        return true;
                    }
                }
                catch (WebException we)
                {
                    string errorResponse = string.Empty;

                    try
                    {
                        using (StreamReader sr2 = new StreamReader(we.Response.GetResponseStream()))
                        {
                            errorResponse = sr2.ReadToEnd();
                            sr2.Close();
                        }
                    }
                    catch
                    {
                        errorResponse = "Unable to get response";
                    }

                    message = errorResponse + Environment.NewLine + we.ToString();
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    return false;
                }
                finally
                {
                    if (null != postStream)
                    {
                        postStream.Close();
                    }

                    if (null != streamWriter)
                    {
                        streamWriter.Close();
                    }

                    if (null != fileStream)
                    {
                        fileStream.Close();
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Neglect the ssl handshake error with authentication server 
        /// </summary>
        private static void BypassCertificateError()
        {
            string bypassSSL = ConfigurationManager.AppSettings["IgnoreSSL"];

            if ((!string.IsNullOrEmpty(bypassSSL))
                && (string.Equals(bypassSSL, "true", StringComparison.OrdinalIgnoreCase)))
            {
                ServicePointManager.ServerCertificateValidationCallback +=
                    delegate(Object sender1, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                    {
                        return true;
                    };
            }
        }


        /// <summary>
        /// This function is used to read access token file and validate the access token
        /// this function returns true if access token is valid, or else false is returned
        /// </summary>
        /// <param name="panelParam">Panel Details</param>
        /// <returns>Returns Boolean</returns>
        private bool ReadAndGetAccessToken(ref string responseString)
        {
            bool result = true;
            if (this.ReadAccessTokenFile(ref responseString) == false)
            {
                result = this.GetAccessToken(AccessType.ClientCredential, ref responseString);
            }
            else
            {
                string tokenValidity = this.IsTokenValid();
                if (tokenValidity == "REFRESH_TOKEN")
                {
                    result = this.GetAccessToken(AccessType.RefreshToken, ref responseString);
                }
                else if (string.Compare(tokenValidity, "INVALID_ACCESS_TOKEN") == 0)
                {
                    result = this.GetAccessToken(AccessType.ClientCredential, ref responseString);
                }
            }

            if (this.accessToken == null || this.accessToken.Length <= 0)
            {
                return false;
            }
            else
            {
                return result;
            }
        }

        #endregion








        #region Display status Functions

        private void DrawPanelForSuccess(HtmlGenericControl control)
        {
            control.Attributes.Add("class", "successWide");
            control.InnerHtml = "<strong>SUCCESS:</strong><br/>" + "Response parameters listed below.";
            control.Visible = true;
        }

        #endregion









        #region Speech Service Functions

        /// <summary>
        /// Content type based on the file extension.
        /// </summary>
        /// <param name="extension">file extension</param>
        /// <returns>the Content type mapped to the extension"/> summed memory stream</returns>
        private string MapContentTypeFromExtension(string extension)
        {
            Dictionary<string, string> extensionToContentTypeMapping = new Dictionary<string, string>()
            {
                { ".amr", "audio/amr" }, { ".wav", "audio/wav" }, {".awb", "audio/amr-wb"}, {".spx", "audio/x-speex"}
            };
            if (extensionToContentTypeMapping.ContainsKey(extension))
            {
                return extensionToContentTypeMapping[extension];
            }
            else
            {
                throw new ArgumentException("invalid attachment extension");
            }
        }


        /// <summary>
        /// This function invokes api SpeechToText to convert the given wav amr file and displays the result.
        /// </summary>
        private void ConvertToSpeech(string parEndPoint, string parAccessToken, string parXspeechContext, string parXArgs, string parSpeechFilePath, bool parChunked)
        {
            Stream postStream = null;
            FileStream audioFileStream = null;
            try
            {
                audioFileStream = new FileStream(parSpeechFilePath, FileMode.Open, FileAccess.Read);
                BinaryReader reader = new BinaryReader(audioFileStream);
                byte[] binaryData = reader.ReadBytes((int)audioFileStream.Length);
                reader.Close();
                audioFileStream.Close();
                if (null != binaryData)
                {
                    HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(string.Empty + parEndPoint);
                    httpRequest.Headers.Add("Authorization", "Bearer " + parAccessToken);
                    httpRequest.Headers.Add("X-SpeechContext", parXspeechContext);
                    if (!string.IsNullOrEmpty(parXArgs))
                    {
                        httpRequest.Headers.Add("X-Arg", parXArgs);
                    }
                    string contentType = this.MapContentTypeFromExtension(Path.GetExtension(parSpeechFilePath));
                    httpRequest.ContentLength = binaryData.Length;
                    httpRequest.ContentType = contentType;
                    httpRequest.Accept = "application/json";
                    httpRequest.Method = "POST";
                    httpRequest.KeepAlive = true;
                    httpRequest.SendChunked = parChunked;
                    postStream = httpRequest.GetRequestStream();
                    postStream.Write(binaryData, 0, binaryData.Length);
                    postStream.Close();

                    HttpWebResponse speechResponse = (HttpWebResponse)httpRequest.GetResponse();
                    using (StreamReader streamReader = new StreamReader(speechResponse.GetResponseStream()))
                    {
                        string speechRequestResponse = streamReader.ReadToEnd();
                       

                        if (!string.IsNullOrEmpty(speechRequestResponse))
                        {
                            JavaScriptSerializer deserializeJsonObject = new JavaScriptSerializer();
                            SpeechResponse deserializedJsonObj = (SpeechResponse)deserializeJsonObject.Deserialize(speechRequestResponse, typeof(SpeechResponse));
                            if (null != deserializedJsonObj)
                            {
                                speechResponseData = new SpeechResponse();
                                speechResponseData = deserializedJsonObj;
                                speechSuccessMessage = "true";
                                //speechErrorMessage = speechRequestResponse;
                            }
                            else
                            {
                                speechErrorMessage = "Empty speech to text response";
                            }
                        }
                        else
                        {
                            speechErrorMessage = "Empty speech to text response";
                        }

                        streamReader.Close();
                    }
                }
                else
                {
                    speechErrorMessage = "Empty speech to text response";
                }
            }
            catch (WebException we)
            {
                string errorResponse = string.Empty;

                try
                {
                    using (StreamReader sr2 = new StreamReader(we.Response.GetResponseStream()))
                    {
                        errorResponse = sr2.ReadToEnd();
                        sr2.Close();
                    }
                }
                catch
                {
                    errorResponse = "Unable to get response";
                }

                speechErrorMessage = errorResponse;
            }
            catch (Exception ex)
            {
                speechErrorMessage = ex.ToString();
            }
            finally
            {
                if (null != postStream)
                {
                    postStream.Close();
                }

                /*
                try
                {
                    System.IO.FileInfo file = new FileInfo(parSpeechFilePath);
                    if (file.Exists)
                    {
                        file.Delete();
                    }
                }
                catch (Exception ex)
                {
                }
                */
            }
        }



        #endregion







    }
}
