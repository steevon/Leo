using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Leo
{
    public static class TurnOnDevice
    {
        

        [FunctionName("TurnOnDevice")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"{DateTime.Now} :: HTTP trigger function processing a \"Turn On Device\" request.");

            // Get parameters from GET request
            string deviceName = req.Query["device"];
            string senderName = req.Query["sender"];
            Int32.TryParse(req.Query["duration"], out int duration);

            // Parse request body for POST reqeust
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            // Get parameters from POST request, if parameter is not valid in GET request.
            deviceName = deviceName ?? data?.device;
            senderName = senderName ?? data?.sender;
            if (duration == -1)
            {
                Int32.TryParse(data?.duration, out duration);
            }

            log.LogInformation($"Device: {deviceName}, Sender: {senderName}, Duration: {duration}");

            // Turn on the device
            if (deviceName != null)
            {
                string envVariable = deviceName + "On";
                string deviceOnUrl = Environment.GetEnvironmentVariable(envVariable);
                if (deviceOnUrl != null) Leo.GetHttpResponse(log, deviceOnUrl, 3);
            }


            // Schedule the device to be turned off
            if (duration > 0)
            {
                await ScheduleOff(log, senderName, deviceName, duration);
            }
            
            // Return response
            return deviceName != null
                ? (ActionResult)new OkObjectResult($"Turned on {deviceName}")
                : new BadRequestObjectResult("Please pass a \"device\" parameter on the query string or in the request body");
        }

        public static async Task ScheduleOff(ILogger log, string senderName, string deviceName, int seconds)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>()
            {
                { "sender", senderName },
                { "device", deviceName },
            };
            string messageText = JsonConvert.SerializeObject(dict);
            await Leo.SendMessageAsync(log, Leo.turnOffDeviceQueueName, messageText, seconds);
        }
    }
}
