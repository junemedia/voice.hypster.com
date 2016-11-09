using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Configuration;
using System.Web.Script.Serialization;
using hypster_voice.Code;


namespace hypster_voice.Controllers
{
    public class sSpeechController : Controller
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
        private string TTSPlainText = string.Empty;
        private string TTSSSML = string.Empty;

        /// <summary>
        /// Gets or sets the value of refreshTokenExpiresIn
        /// </summary>
        private int refreshTokenExpiresIn;

        string xArgsData = string.Empty;

        public string TTSErrorMessage = string.Empty;
        public string TTSSuccessMessage = string.Empty;
        public byte[] receivedBytes = null;



        



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

        public string CurrUser = "";


        //
        // GET: /senses/sVoice/
        //
        public ActionResult Index()
        {
            return View();
        }




        //[System.Web.Mvc.OutputCache(Duration = 1500, VaryByParam="qq")]
        public string getSpeech(string id)
        {
            System.Web.HttpBrowserCapabilitiesBase browser = Request.Browser;
            Curr_Client = browser.Browser;

            CurrUser = id;

            if (Request.QueryString["qq"] != null)
            {
                SpeechTest = Request.QueryString["qq"].ToString();
            }



            // check for standart speech
            //
            Curr_Return_Str = check_for_standart_speech();



            if (Curr_Return_Str == "")
            {
                BypassCertificateError();

                ReadConfigFile();

                SetContent();
            }



            return Curr_Return_Str;
        }



        private string check_for_standart_speech()
        {
            bool isStandartSpeech = false;
            string str_check = SpeechTest.ToLower().Replace(".", "");

            switch (str_check)
            {
                case "access my account":
                    isStandartSpeech = true;
                    break;
                case "my account":
                    isStandartSpeech = true;
                    break;
                case "press 1 to add to your default playlist":
                    isStandartSpeech = true;
                    break;
                case "added":
                    isStandartSpeech = true;
                    break;
                case "beep_01":
                    isStandartSpeech = true;
                    break;
                case "home":
                    isStandartSpeech = true;
                    break;
                case "listen to charts":
                    isStandartSpeech = true;
                    break;
                case "listen to radio":
                    isStandartSpeech = true;
                    break;
                case "signing you in please wait":
                    isStandartSpeech = true;
                    break;
                case "press 2 to remove from playlist":
                    isStandartSpeech = true;
                    break;
                case "removed":
                    isStandartSpeech = true;
                    break;
                case "search music":
                    isStandartSpeech = true;
                    break;
                case "access your playlists":
                    isStandartSpeech = true;
                    break;
                default:
                    break;
            }


            if (isStandartSpeech)
            {
                return getSpeechString(str_check);
            }

            return "";
        }


        private string getSpeechString(string p_fname)
        {
            return "<!DOCTYPE html><html><head><title> hyp Speach </title></head><body>     <audio controls='controls' src='/sound/" + p_fname + ".mp3' autobuffer='autobuffer' autoplay='autoplay' id='audioPlay'></audio>    </body></html>";
        }
        








        private void SetContent()
        {

            // send request to get voice
            //
            try
            {
                bool IsValid = true;

                IsValid = this.ReadAndGetAccessToken(ref TTSErrorMessage);
                if (IsValid == false)
                {
                    TTSErrorMessage = "Unable to get access token";
                    return;
                }


                string content = string.Empty;
                content = SpeechTest;


                this.TextToSpeech(this.fqdn, "/speech/v3/textToSpeech", this.accessToken, "", "text/plain", content);

            }
            catch (Exception ex)
            {
                TTSErrorMessage = ex.Message;
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
            this.accessTokenFilePath = ConfigurationManager.AppSettings["AccessTokenFilePath"];
            if (string.IsNullOrEmpty(this.accessTokenFilePath))
            {
                this.accessTokenFilePath = "~\\Token.txt";
            }

            this.fqdn = ConfigurationManager.AppSettings["FQDN"];
            if (string.IsNullOrEmpty(this.fqdn))
            {
                TTSErrorMessage = "FQDN is not defined in configuration file";
                return false;
            }

            this.apiKey = ConfigurationManager.AppSettings["api_key"];
            if (string.IsNullOrEmpty(this.apiKey))
            {
                TTSErrorMessage = "api_key is not defined in configuration file";
                return false;
            }

            this.secretKey = ConfigurationManager.AppSettings["secret_key"];
            if (string.IsNullOrEmpty(this.secretKey))
            {
                TTSErrorMessage = "secret_key is not defined in configuration file";
                return false;
            }

            this.scope = ConfigurationManager.AppSettings["scope"];
            if (string.IsNullOrEmpty(this.scope))
            {
                this.scope = "TTS";
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



        #region Speech Service Functions

        /// <summary>
        /// This function invokes api SpeechToText to convert the given wav amr file and displays the result.
        /// </summary>
        private void TextToSpeech(string parEndPoint, string parURI, string parAccessToken, string parXarg, string parContentType, string parContent)
        {
            try
            {

                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(string.Empty + parEndPoint + parURI);
                httpRequest.Headers.Add("Authorization", "Bearer " + parAccessToken);
                httpRequest.Headers.Add("X-SpeechContext", parXarg);
                httpRequest.Headers.Add("X-Arg", "Tempo=4,VoiceName=crystal");
                httpRequest.ContentLength = parContent.Length;
                httpRequest.ContentType = parContentType;
                httpRequest.Accept = "audio/x-wav";
                httpRequest.Method = "POST";
                httpRequest.KeepAlive = true;

                UTF8Encoding encoding = new UTF8Encoding();
                byte[] postBytes = encoding.GetBytes(parContent);
                httpRequest.ContentLength = postBytes.Length;

                using (Stream writeStream = httpRequest.GetRequestStream())
                {
                    writeStream.Write(postBytes, 0, postBytes.Length);
                    writeStream.Close();
                }
                HttpWebResponse speechResponse = (HttpWebResponse)httpRequest.GetResponse();
                int offset = 0;
                int remaining = Convert.ToInt32(speechResponse.ContentLength);
                using (var stream = speechResponse.GetResponseStream())
                {
                    receivedBytes = new byte[speechResponse.ContentLength];
                    while (remaining > 0)
                    {
                        int read = stream.Read(receivedBytes, offset, remaining);
                        if (read <= 0)
                        {
                            TTSErrorMessage = String.Format("End of stream reached with {0} bytes left to read", remaining);
                            return;
                        }

                        remaining -= read;
                        offset += read;
                    }

                    TTSSuccessMessage = "Success";








                    if (Curr_Client == "Firefox")
                    {
                        Curr_Return_Str = "<!DOCTYPE html><html><head><title></title></head><body>   <audio controls='controls' src='data:audio/wav;base64," + Convert.ToBase64String(receivedBytes, Base64FormattingOptions.None) + "' autobuffer='autobuffer' autoplay='autoplay' id='audioPlay'></audio>    </body></html>";
                        return;
                    }


                    if (Curr_Client == "InternetExplorer")
                    {
                        using (FileStream fs = System.IO.File.Create(ConfigurationManager.AppSettings["speechSavePath"] + CurrUser + ".wav"))
                        {
                            fs.Write(receivedBytes, 0, receivedBytes.Length);
                        }

                        Curr_Return_Str = "<!DOCTYPE html><html><head><title></title></head><body>    <embed id='speechContainerEm' src='/sound/speech/" + CurrUser + ".wav' autostart=true width=300 height=300 id='wavfile' enablejavascript='true'>    </body></html>";
                        return;
                    }


                    if (Curr_Client == "Safari")
                    {
                        using (FileStream fs = System.IO.File.Create(ConfigurationManager.AppSettings["speechSavePath"] + CurrUser + ".wav"))
                        {
                            fs.Write(receivedBytes, 0, receivedBytes.Length);
                        }

                        Curr_Return_Str = "<!DOCTYPE html><html><head><title></title></head><body>     <audio controls='controls' src='/sound/speech/" + CurrUser + ".wav' autobuffer='autobuffer' autoplay='autoplay' id='audioPlay'></audio>    </body></html>";
                        return;
                    }




                    //default
                    //
                    Curr_Return_Str = "<!DOCTYPE html><html><head><title></title></head><body>     <audio controls='controls' src='data:audio/wav;base64," + Convert.ToBase64String(receivedBytes, Base64FormattingOptions.None) + "' autobuffer='autobuffer' autoplay='autoplay' id='audioPlay'></audio>     </body></html>";

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
                    TTSErrorMessage = errorResponse;
                }
                catch
                {
                    errorResponse = "Unable to get response";
                    TTSErrorMessage = errorResponse;
                }
            }
            catch (Exception ex)
            {
                TTSErrorMessage = ex.Message;
                return;
            }




            if (Curr_Return_Str == "")
            {
                Curr_Return_Str = TTSErrorMessage;
            }
            else
            {
                Curr_Return_Str.Replace("<title></title>", "<title>" + TTSErrorMessage + "</title>");
            }

        }


        #endregion








    }


}
