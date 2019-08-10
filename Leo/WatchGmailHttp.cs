using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Leo
{
    public static class WatchGmailHttp
    {
        [FunctionName("WatchGmailHttp")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a Watch Gmail request.");
            string token = await AuthenticateGoogle.RefreshAccessToken(log);
            GmailAPI api = new GmailAPI(log, token);
            dynamic response = api.Watch();
            string responseBody = JsonConvert.SerializeObject(response);

            return new OkObjectResult($"{responseBody}");
        }
    }
}
