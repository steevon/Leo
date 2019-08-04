using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;


namespace Leo
{
    public class GoogleAPI
    {
        private string accessToken;
        private string userID;
        private ILogger log;

        public GoogleAPI(string accessToken, ILogger log=null)
        {
            this.accessToken = accessToken;
            this.log = log;
        }

        public dynamic Request(string url, Dictionary<string, string> query=null)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {accessToken}" }
            };
            url += "?";
            if (query != null)
            {
                foreach (KeyValuePair<string, string> q in query)
                {
                    url = $"{url}&{q.Key}={q.Key}";
                }
            }

            dynamic data_response = Leo.GetJSONResponse(log, url, headers: headers);
            return data_response;
        }

        public dynamic GmailProfile()
        {
            string user_id = Environment.GetEnvironmentVariable("GmailAddress");
            string url = $"https://www.googleapis.com/gmail/v1/users/{user_id}/profile";
            dynamic data_response = Request(url);
            return data_response;
        }
    }
}
