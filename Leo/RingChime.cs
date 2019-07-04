using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.ServiceBus;
using System.Text;

namespace Leo
{
    public static class RingChime
    {
        const string QueueName = "chime";
        const int RingDuration = 20;
        static IQueueClient queueClient;

        [FunctionName("RingChime")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            
            log.LogInformation("HTTP trigger function processing RingChime request.");

            // Get the name parameter
            string name = req.Query["name"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            // Ring Chime
            Leo.GetHttpResponse(log, Environment.GetEnvironmentVariable("ChimeOn"), 3);
            // Schedule it to be turned off
            await Leo.ScheduleOff(log, name, "Chime", RingDuration);

            // Return the response
            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
