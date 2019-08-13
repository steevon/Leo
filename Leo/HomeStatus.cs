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
using System.Text.RegularExpressions;

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
                Dictionary<string, Status> status = RingStatus(log, token);
                string responseBody = JsonConvert.SerializeObject(status);
                return new OkObjectResult($"{responseBody}");
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult($"{ex.Message}");
            }
        }

        public static Dictionary<string, Status> RingStatus(ILogger log, string token)
        {
            Dictionary<string, string> query = new Dictionary<string, string>
            {
                { "q", $"from:no-reply@rs.ring.com" }
            };
            GmailAPI api = new GmailAPI(log, token);
            api.Query = query;

            Dictionary<string, Status> status = new Dictionary<string, Status>();
            status["Alarm"] = AlarmStatus(api, log);
            
            return status;
        }

        private static Status AlarmStatus(GmailAPI api, ILogger log)
        {
            string alarmMode = null;
            string changedTime = null;
            string details = null;
            
            List<dynamic> messages = api.ListMessages();
            foreach (dynamic message in messages)
            {
                string messageID = message.id;
                dynamic fullMessage = api.GetMessage(messageID);
                dynamic headers = fullMessage.payload.headers;
                string subject = null;
                foreach (dynamic header in headers)
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
                    //string msg = JsonConvert.SerializeObject(fullMessage);
                    changedTime = Leo.MillisecondsToLocalTimeString(Convert.ToInt64(fullMessage.internalDate));
                    // Extract information from snippet
                    Match match = Regex.Match(Convert.ToString(fullMessage.snippet), @".*?(Ring\sAlarm\sin\s[A-Za-z]+\schanged\sto\s.*?)Still\shave\squestions\?");
                    details = match.Groups[1].Value;
                    break;
                }
            }
            alarmMode = alarmMode ?? "Unknown";
            return new Status
            {
                Mode = alarmMode,
                Time = changedTime,
                Details = details
            };
        }
    }

    public class Status
    {
        public string Mode { get; set; }
        public string Time { get; set; }
        public string Details { get; set; }
    }

}
