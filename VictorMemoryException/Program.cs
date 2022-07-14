using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace VictorMemoryException
{
    internal class HttpWebApiClient : HttpClient
    {
        public HttpWebApiClient(Uri baseUri, string sessionID = null)
            : base()
        {
            BaseAddress = baseUri;
            DefaultRequestHeaders.Accept.Clear();
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!String.IsNullOrEmpty(sessionID))
            {
                DefaultRequestHeaders.Add("session-id", sessionID);
            }
            Timeout = TimeSpan.FromMinutes(10);
        }
    }

    internal class LoginData
    {
        public string Token { get; set; }
        public string SessionID { get; set; }

        public override string ToString()
        {
            return $"Token: {Token} - SessionID: {SessionID}";
        }
    }



    class Program
    {

        private const string _prefix = "victorWebService/";
        private static Uri _baseUri;

        static void Main(string[] args)
        {

            var username = "TESTOPERATOR";
            var password = "Milestone1$";
            var ClientName = "Milestone XProtect";
            var clientID = "06031FBF-E237-4602-995C-AB330E2D902C";
            var hostname = "SGIU-CCURE30";

            Console.Write("Enter hostname: ");
            hostname = Console.ReadLine();
            Console.Write("Enter Username: ");
            username = Console.ReadLine();
            Console.Write("Enter Password: ");
            password = Console.ReadLine();
            Console.Write("Enter clientID: ");
            clientID = Console.ReadLine();

            var useHttps = false;
            var port = 80;
            var builder = new UriBuilder();

            builder.Scheme = useHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
            builder.Port = port;
            builder.Host = hostname;
            _baseUri = builder.Uri;

            Uri baseUri = _baseUri;



            string clientName = ClientName;



            var kvpList = new List<KeyValuePair<string, string>>();
            kvpList.Add(new KeyValuePair<string, string>("userName", username));
            kvpList.Add(new KeyValuePair<string, string>("password", password));
            kvpList.Add(new KeyValuePair<string, string>("clientName", clientName));
            kvpList.Add(new KeyValuePair<string, string>("clientID", clientID));
            kvpList.Add(new KeyValuePair<string, string>("clientVersion", "3.1"));


            bool fail = false;
            int count = 0;
            while (!fail)
            {

                using (var client = new HttpWebApiClient(baseUri))
                {
                    count++;
                    // Login 
                    var httpcontent = new FormUrlEncodedContent(kvpList);
                    HttpResponseMessage resp = LogIn(httpcontent, client);
                    LoginData loginData = resp.IsSuccessStatusCode ? GetLogInData(resp) : null;

                    if (loginData != null)
                    {
                        Console.WriteLine($"Login Succeced: Username {username} - {loginData}");

                        //      Thread.Sleep(1000);

                        var respLogout = LogOut(client, loginData);
                        if (respLogout.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Logout Succeced: {loginData}");
                            //success = true;
                        }
                    }
                    else
                    {
                        fail = true;
                        Console.WriteLine($"Fail after {count} logins");
                    }
                }
                //Thread.Sleep(1000);
            }
            Console.WriteLine("Press a key to exit");
            Console.ReadKey();
        }

        private static LoginData GetLogInData(HttpResponseMessage resp)
        {
            var session_id = resp.Headers.First(x => x.Key == "session-id").Value;
            var loginData = new LoginData() { SessionID = session_id.FirstOrDefault().ToString() };
            return loginData;
        }

        private static HttpResponseMessage LogIn(FormUrlEncodedContent httpcontent, HttpWebApiClient client)
        {
            var query = CreateCCureRequestUri("api/Authenticate/Login", null, null);
            var resp = Call(query, httpcontent, client).Result;
            return resp;
        }

        private static HttpResponseMessage LogOut(HttpWebApiClient client, LoginData loginData)
        {
            HttpResponseMessage resp;
            var LogOutQuery = CreateCCureRequestUri("api/Authenticate/Logout", null, loginData);
            client.DefaultRequestHeaders.Add("session-id", loginData.SessionID);
            resp = Call(LogOutQuery, null, client).Result;
            return resp;
        }


        private static async Task<HttpResponseMessage> Call(string query, FormUrlEncodedContent httpcontent, HttpWebApiClient client)
        {
            return await client.PostAsync(query, httpcontent);
        }


        private static string CreateCCureRequestUri(string path, NameValueCollection queryParams, LoginData loginData, string tokenKeyName = "token")
        {
            if (queryParams == null)
            {
                queryParams = new NameValueCollection();
            }

            if (loginData != null && !string.IsNullOrEmpty(loginData.Token))
            {
                queryParams.Add(tokenKeyName, loginData.Token);
            }

            return CreateRequestUri(_prefix + path, queryParams);
        }

        private static string CreateRequestUri(string path, NameValueCollection queryParams)
        {
            var array = (from key in queryParams.AllKeys
                         from value in queryParams.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value))).ToArray();
            return (array.Length == 0) ? path : path + "?" + string.Join("&", array);
        }

    }
}
