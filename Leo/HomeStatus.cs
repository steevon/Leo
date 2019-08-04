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
            string access_token = await AuthenticateGoogle.RefreshAccessToken(log);
            
            log.LogInformation($"Obtained Access Token: {access_token.Substring(0, 12)}******");
            try
            {
                GmailAPI api = new GmailAPI(access_token, log);
                dynamic response = api.Profile();
                return new OkObjectResult($"Hello, {response?.emailAddress}\n" +
                    $"Total Messages: {response?.messagesTotal}\n" +
                    $"Total Threads: {response?.threadsTotal}"
                );
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult($"{ex.Message}");
            }
        }

        private static Dictionary<string, string> RingStatus(string access_token, ILogger log)
        {
            string user_id = Environment.GetEnvironmentVariable("GmailAddress");
            string url = $"https://www.googleapis.com/gmail/v1/users/{user_id}/messages";
            Dictionary<string, string> query = new Dictionary<string, string>
            {
                { "q", $"from:no-reply@rs.ring.com" }
            };
            GmailAPI api = new GmailAPI(access_token, log);
            api.Query = query;
            dynamic data_response = api.Request(url);
            Dictionary<string, string> status = new Dictionary<string, string>();
            return status;
        }

    }
}
