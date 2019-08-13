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
        /// <summary>
        /// When triggered by HTTP GET or POST request, turns on a device by sending out a HTTP GET request.
        /// This function uses 4 parameters from either the query string in GET request or the JSON body in the POST request.
        /// device: The name of the device([Device Name]) to be turned on.
        ///     The URL for turning on the device by HTTP GET request must be stored as an environment variable with the name "[Device Name]On".
        ///     For example, if device=light, then the environment variable should be named "lightOn".
        /// sender: Optional. The name of the device triggering the function. This is used in logging only.
        /// condition: Optional, either "day" or "night". Turn on the device only if it is day or night.
        ///     By default the device will be turned on regardless of the time.
        /// duration: Optional, specify in seconds. Turn on the device for a certain duration only. Device will be turned of after the duration.
        ///     If duration is specified,
        ///     the URL for turning off the device by HTTP GET request must be stored as an environment variable with the name "[Device Name]Off".
        /// 
        /// </summary>
        /// <param name="req">HTTP Request</param>
        /// <param name="log">Logger</param>
        /// <returns></returns>
        [FunctionName("TurnOnDevice")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"{DateTime.Now} :: HTTP trigger function processing a \"Turn On Device\" request.");

            // Get parameters from GET request
            string deviceName = req.Query["device"];
            string senderName = req.Query["sender"];
            string condition = req.Query["condition"];
            // duration will have the value of -1 if it is not specified.
            Int32.TryParse(req.Query["duration"], out int duration);

            // Parse request body for POST reqeust
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            // Get parameters from POST request, if parameter is not valid in GET request.
            deviceName = deviceName ?? data?.device;
            senderName = senderName ?? data?.sender;
            condition = condition ?? data?.condition;
            // duration will have the value of -1 if it is not specified.
            if (duration == -1)
            {
                Int32.TryParse(data?.duration, out duration);
            }

            log.LogInformation($"Device: {deviceName}, Sender: {senderName}, Duration: {duration}");
            string responseMessage;

            if (deviceName != null)
            {
                switch (condition)
                {
                    // Turn on the device during daytime only.
                    case "day":
                        if (Leo.AtNight(log))
                        {
                            responseMessage = $"It is nighttime now. {deviceName} not turned on.";
                            log.LogInformation(responseMessage);
                            return new OkObjectResult($"It is nighttime now. {deviceName} not turned on.");
                        }
                        break;
                    // Turn on the device during nighttime only.
                    case "night":
                        if (!Leo.AtNight(log))
                        {
                            responseMessage = $"It is daytime now. {deviceName} not turned on.";
                            log.LogInformation(responseMessage);
                            return new OkObjectResult(responseMessage);
                        }
                        break;
                }
                // Turn on the device
                TurnOnDeviceByHttpRequest(log, deviceName);
                // Schedule the device to be turned off
                if (duration > 0)
                {
                    await ScheduleOff(log, senderName, deviceName, duration);
                    
                }
                // Return Response
                responseMessage = $"Turned on {deviceName}";
                log.LogInformation(responseMessage);
                return new OkObjectResult(responseMessage);
            }

            // Return bad request if the device parameter is null
            log.LogError("Parameter \"device\" not found on the query string or in the request body.");
            return new BadRequestObjectResult("Please pass a \"device\" parameter on the query string or in the request body.");
        }

        public static void TurnOnDeviceByHttpRequest(ILogger log, string deviceName)
        {
            string envVariable = deviceName + "On";
            string deviceOnUrl = Environment.GetEnvironmentVariable(envVariable);
            if (deviceOnUrl != null) Leo.GetHttpResponse(log, deviceOnUrl, 3);
            else log.LogError($"Trigger for turning on {deviceName} not found.");
        }

        /// <summary>
        /// Schedules turning off a device by sending a message to the service bus queue.
        /// 
        /// </summary>
        /// <param name="log"></param>
        /// <param name="senderName"></param>
        /// <param name="deviceName"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static async Task ScheduleOff(ILogger log, string senderName, string deviceName, int seconds)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>()
            {
                { "sender", senderName },
                { "device", deviceName },
            };
            string messageText = JsonConvert.SerializeObject(dict);
            DateTime? turnOffTime = await Leo.SendMessageAsync(log, Leo.turnOffDeviceQueueName, messageText, seconds);
            if (turnOffTime != null)
            {
                log.LogInformation($"Device is scheduled to be turned off at {turnOffTime}");
            }
            else
            {
                log.LogError($"Failed to schedule turning off device {deviceName}.");
            }
        }
    }
}
