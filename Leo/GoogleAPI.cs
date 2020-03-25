using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;


namespace Leo
{
    public class GoogleAPI
    {
        private string accessToken;
        protected ILogger log;

        public GoogleAPI(ILogger log, string accessToken)
        {
            this.log = log;
            this.accessToken = accessToken;
        }

        public dynamic Request(string url, Dictionary<string, string> query=null)
        {
            log.LogInformation($"Access Token: {accessToken}");
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {accessToken}" }
            };

            if (query != null)
            {
                url += "?";
                foreach (KeyValuePair<string, string> q in query)
                {
                    if (q.Value != null) url = $"{url}&{q.Key}={q.Value}";
                }
            }

            dynamic data_response = Leo.GetJSONResponse(log, url, headers: headers);
            if (data_response?.error != null)
            {
                log.LogError($"{data_response}");
            }
            return data_response;
        }

        public dynamic PostRequest(string url, dynamic data)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {accessToken}" }
            };
            return Leo.PostJSONResponse(log, url, data, headers);
        }

    }

    public class GmailAPI : GoogleAPI
    {
        private string pageToken = null;
        public Dictionary<string, string> Query { get; set; }

        public GmailAPI(ILogger log, string accessToken) : base(log, accessToken)
        {
        }

        public dynamic Profile()
        {
            string url = $"https://www.googleapis.com/gmail/v1/users/me/profile";
            dynamic response = Request(url);
            return response;
        }

        public dynamic ListMessages(int maxResults=0)
        {
            pageToken = null;
            return NextMessages(maxResults);
        }

        public dynamic NextMessages(int maxResults=0)
        {
            List<dynamic> messages = new List<dynamic>();
            string url = $"https://www.googleapis.com/gmail/v1/users/me/messages";
            Query["pageToken"] = pageToken != null ? pageToken : null;
            Query["maxResults"] = maxResults != 0 ? maxResults.ToString() : null;
            dynamic response = Request(url, Query);
            if (response?.messages == null)
            {
                throw new Exception(response);
            }
            this.pageToken = response.nextPageToken;
            messages.AddRange(response.messages);
            return messages;
        }

        public dynamic GetMessage(string messageID)
        {
            string url = $"https://www.googleapis.com/gmail/v1/users/me/messages/{messageID}";
            dynamic response = Request(url);
            return response;
        }

        public dynamic Watch(string label="INBOX")
        {
            string url = "https://www.googleapis.com/gmail/v1/users/me/watch";
            dynamic data = new JObject();
            data.topicName = Environment.GetEnvironmentVariable("GooglePubSubTopic");
            data.labelIds = new JArray(label);
            return PostRequest(url, data);
        }
    }
}
