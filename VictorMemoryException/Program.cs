﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
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

            var hostname = "localhost";
            var username = "Operator";
            var password = "Milestone2023..";
            var clientName = "Milestone XProtect";
            var clientID = "06031FBF-E237-4602-995C-AB330E2D902C";
            var Uuid = "06031FBF-E237-4602-995C-AB330E2D902C";
            var clientVersion = "3.1";
            var waitTime = 3; // in seconds 

            //Console.Write("Enter hostname: ");
            //hostname = Console.ReadLine();
            //Console.Write("Enter Username: ");
            //username = Console.ReadLine();
            //Console.Write("Enter Password: ");
            //password = Console.ReadLine();
            //Console.Write("Enter clientName: ");
            //clientName = Console.ReadLine();
            //Console.Write("Enter clientID: ");
            //clientID = Console.ReadLine();
            //Console.Write("Enter clientVersion: ");
            //clientVersion = Console.ReadLine();

            bool isNumber;
            do
            {
                Console.Write("Enter wait time: ");
                var waitTimeStr = Console.ReadLine();
                isNumber = int.TryParse(waitTimeStr, out waitTime);
            }
            while (!isNumber);

            var useHttps = false;
            var port = 80;
            var builder = new UriBuilder();
            builder.Scheme = useHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
            builder.Port = port;
            builder.Host = hostname;
            _baseUri = builder.Uri;
            Uri baseUri = _baseUri;

            // Header Parameters 
            var kvpList = new List<KeyValuePair<string, string>>();
            kvpList.Add(new KeyValuePair<string, string>("userName", username));
            kvpList.Add(new KeyValuePair<string, string>("password", password));
            kvpList.Add(new KeyValuePair<string, string>("clientName", clientName));
            kvpList.Add(new KeyValuePair<string, string>("clientID", clientID));
            kvpList.Add(new KeyValuePair<string, string>("clientVersion", clientVersion));
            kvpList.Add(new KeyValuePair<string, string>("Uuid", Uuid)); 


            // Aux variables 
            bool fail = false;
            int count = 0;

            while (!fail)
            {

                using (var client = new HttpWebApiClient(baseUri))
                {
                    count++;
                    string errorMessage = "";
                    // Login 
                    var httpcontent = new FormUrlEncodedContent(kvpList);
                    HttpResponseMessage resp = LogIn(httpcontent, client);
                    LoginData loginData = resp.IsSuccessStatusCode ? GetLogInData(resp) : null;

                    errorMessage = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();


                    if (loginData != null)
                    {
                        // Login Succeed
                        Console.WriteLine($"[{count}] Login Succeed: {loginData}");

                        Console.WriteLine($"Wait {waitTime} sec.");
                        Thread.Sleep(waitTime*1000);

                        // Logout Succeed
                        var respLogout = LogOut(client, loginData);
                        if (respLogout.IsSuccessStatusCode)
                        {
                            // Logout Succeed
                            Console.WriteLine($"Logout Succeed: {loginData}");
                        }
                        else 
                        {
                            errorMessage = respLogout.Content.ToString();
                        }
                    }
                    else
                    {
                        // break loop 
                        fail = true;
                        Console.WriteLine($"Fail after {count} logins. With the error {errorMessage}");
                    }
                }
                Console.WriteLine($"Wait {waitTime} sec.");
                Thread.Sleep(waitTime * 1000);
            }
            Console.WriteLine("Press a key to exit");
            Console.ReadKey();
        }

        private static LoginData GetLogInData(HttpResponseMessage resp)
        {
            var session_id = resp.Headers.First(x => x.Key == "session-id").Value.FirstOrDefault().ToString();

            var response = resp.Content.ReadAsStringAsync().Result;
            var token = response.Trim('\"');

            var loginData = new LoginData() {
                SessionID = session_id,
                Token = token
            };
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
