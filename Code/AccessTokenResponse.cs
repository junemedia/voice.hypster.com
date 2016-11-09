using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI.HtmlControls;



namespace hypster_voice.Code
{

    public class AccessTokenResponse
    {
        /// <summary>
        /// Gets or sets Access Token ID
        /// </summary>
        public string access_token
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Refresh Token ID
        /// </summary>
        public string refresh_token
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets Expires in milli seconds
        /// </summary>
        public string expires_in
        {
            get;
            set;
        }
    }

}