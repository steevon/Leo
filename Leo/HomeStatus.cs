using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Leo
{
    public static class HomeStatus
    {
        [FunctionName("HomeStatus")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("HTTP trigger function processed Home Status request.");
            string refreshToken = Environment.GetEnvironmentVariable("GmailRefreshToken");
            string clientID = Environment.GetEnvironmentVariable("GoogleClientID");
            string clientSecret = Environment.GetEnvironmentVariable("GoogleClientSecret");
            // Get access token
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "refresh_token", refreshToken },
                { "client_id", clientID },
                { "client_secret", clientSecret },
                { "grant_type", "refresh_token" }
            };
            dynamic token_response = await Leo.PostJSONResponse(log, "https://www.googleapis.com/oauth2/v4/token", data);

            string access_token = token_response.access_token;
            log.LogInformation($"Obtained Access Token. {access_token}");
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {access_token}" }
            };
            string user_id = "qqngg2018%40gmail.com";
            string url = $"https://www.googleapis.com/gmail/v1/users/{user_id}/profile?access_token={access_token}";
            dynamic data_response = Leo.GetJSONResponse(log, url);
            if (data_response.error != null) return new BadRequestObjectResult($"{data_response.error}");

            return new OkObjectResult($"Hello, {data_response?.emailAddress}\n" +
                $"Total Messages: {data_response?.messagesTotal}\n" +
                $"Total Threads: {data_response?.threadsTotal}"
            );
        }
    }
}
