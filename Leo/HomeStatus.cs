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
        private static readonly Dictionary<string, string> ringAlarm = new Dictionary<string, string>
        {
            { "Ring Alarm is Disarmed", "Disarmed" },
            { "Ring Alarm is in Away Mode", "Away" },
            { "Ring Alarm is in Home Mode", "Home" }
        };

        [FunctionName("HomeStatus")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("HTTP trigger function processed Home Status request.");
            string token = await AuthenticateGoogle.RefreshAccessToken(log);
            
            log.LogInformation($"Obtained Access Token: {token.Substring(0, 12)}******");
            try
            {
                Dictionary<string, string> status = RingStatus(token, log);
                string responseBody = JsonConvert.SerializeObject(status);
                return new OkObjectResult($"{responseBody}");
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult($"{ex.Message}");
            }
        }

        private static Dictionary<string, string> RingStatus(string access_token, ILogger log)
        {
            Dictionary<string, string> status = new Dictionary<string, string>();
            string alarmMode = null;
            Dictionary<string, string> query = new Dictionary<string, string>
            {
                { "q", $"from:no-reply@rs.ring.com" }
            };
            GmailAPI api = new GmailAPI(access_token, log);
            api.Query = query;
            List<dynamic> messages = api.ListMessages();
            foreach(dynamic message in messages)
            {
                string messageID = message.id;
                dynamic headers = api.GetMessage(messageID).payload.headers;
                string subject = null;
                foreach(dynamic header in headers)
                {
                    if (header.name == "Subject")
                    {
                        subject = header.value;
                        break;
                    }
                }
                
                ringAlarm.TryGetValue(subject, out alarmMode);
                if (alarmMode != null)
                {

                    break;
                }
            }
            status["Alarm"] = alarmMode ?? "Unknown";
            
            return status;
        }

    }
}
