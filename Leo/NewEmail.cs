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
    public static class NewEmail
    {
        [FunctionName("NewEmail")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("HTTP trigger function processed a New Email request.");
            string token = await AuthenticateGoogle.RefreshAccessToken(log);

            try
            {
                Status alarmStatus = ConfigBaseOnRingAlarm(log, token);
                return new OkObjectResult($"{alarmStatus}");
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult($"{ex.Message}");
            }
        }

        private static Status ConfigBaseOnRingAlarm(ILogger log, string token)
        {
            Dictionary<string, Status> status = HomeStatus.RingStatus(log, token);
            switch (status["Alarm"].Mode)
            {
                case "Home":
                    TurnOnDevice.TurnOnDeviceByHttpRequest(log, "LivingRoomCamera");
                    break;
                case "Away":
                    TurnOnDevice.TurnOnDeviceByHttpRequest(log, "OfficeCamera");
                    TurnOnDevice.TurnOnDeviceByHttpRequest(log, "LivingRoomCamera");
                    break;
                default:
                    TurnOffDevice.TurnOffDeviceByHttpRequest(log, "OfficeCamera");
                    TurnOffDevice.TurnOffDeviceByHttpRequest(log, "LivingRoomCamera");
                    break;
            }
            return status["Alarm"];
        }
    }
}
