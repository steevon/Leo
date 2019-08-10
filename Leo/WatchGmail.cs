using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Leo
{
    public static class WatchGmail
    {
        [FunctionName("WatchGmail")]
        public static async Task Run([TimerTrigger("0 0 1 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a Watch Gmail request.");
            string token = await AuthenticateGoogle.RefreshAccessToken(log);
            GmailAPI api = new GmailAPI(log, token);
            dynamic response = api.Watch();
            string responseBody = JsonConvert.SerializeObject(response);
            log.LogInformation($"{responseBody}");
        }
    }
}
