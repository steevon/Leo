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
    public static class TimeZones
    {
        [FunctionName("TimeZones")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed Time Zones request.");

            string results = "";

            foreach (TimeZoneInfo z in TimeZoneInfo.GetSystemTimeZones())
                results += z.Id + "\n";

            return (ActionResult)new OkObjectResult($"{results}");
        }
    }
}
