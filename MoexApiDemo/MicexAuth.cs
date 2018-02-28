using System;
using System.Net;

namespace WPF_ISS_Demo
{
    public class MicexAuth
    {
    
        // MICEX Passport username
        private readonly string _username;
        // MICEX Passport password
        private readonly string _password;

        // Storage for all cookies which will be submitted with further requests
        public CookieContainer Cookiejar = new CookieContainer();
        //Separate storage for a passport cookie, just for convenience
        public Cookie Passport = new Cookie();


        // HTTP status
        // will be set to BadRequest for non-protocol errors
        public HttpStatusCode LastStatus;

        public string LastStatusText;

        //Public headers As New WebHeaderCollection()

        // URL for authorization in order to get a passport cookie
        public string UrlAuth = "https://passport.moex.com/authenticate";

        // URL that will be used as URI to find a cookie for the correct domain
        public string UrlUri = "http://moex.com";

        public MicexAuth(string userName, string passwd)
        {
            _username = userName;
            _password = passwd;
            Auth();
        }

        public void Auth()
        {
            try
            {
                var authReq = (HttpWebRequest) WebRequest.Create(UrlAuth);
                authReq.CookieContainer = new CookieContainer();

                // use the Basic authorization mechanism
                var binData = System.Text.Encoding.UTF8.GetBytes(_username + ":" + _password);
                var sAuth64 = Convert.ToBase64String(binData, Base64FormattingOptions.None);
                authReq.Headers.Add(HttpRequestHeader.Authorization, "Basic " + sAuth64);

                var authResponse = (HttpWebResponse) authReq.GetResponse();
                authResponse.Close();
                LastStatus = authResponse.StatusCode;

                Cookiejar = new CookieContainer();
                Cookiejar.Add(authResponse.Cookies);

                // find the Passport cookie for a given
                //domain URI
                var myuri = new Uri(UrlUri);
                Passport = new Cookie();
                foreach (Cookie cook in Cookiejar.GetCookies(myuri))
                {
                    if (cook.Name == "MicexPassportCert")
                    {
                        Passport = cook;
                    }
                }
                
                LastStatusText = Passport.Name != "MicexPassportCert"
                    ? "Passport cookie not found"
                    : "OK";
            }
            catch (WebException e)
            {
                Console.WriteLine(e.Message);
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    var statusCode = ((HttpWebResponse) e.Response).StatusCode;
                    Console.WriteLine($@"Status Code : {statusCode}");
                    var statusDescription = ((HttpWebResponse) e.Response).StatusDescription;
                    Console.WriteLine($@"Status Description : {statusDescription}");
                    LastStatus = ((HttpWebResponse) e.Response).StatusCode;
                    LastStatusText = ((HttpWebResponse) e.Response).StatusDescription;
                }
                else
                {
                    LastStatus = HttpStatusCode.BadRequest;
                    LastStatusText = e.Message;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                LastStatus = HttpStatusCode.BadRequest;
                LastStatusText = e.Message;
            }
        }

        /// <summary>
        /// repeat authorization request if failed last time or if the Passport has expired
        /// </summary>
        public bool IsRealTime()
        {
            if (Passport == null || Passport != null && Passport.Expired)
                Auth();
            return Passport != null && !Passport.Expired && Passport.Name == "MicexPassportCert";
        }
    }
}