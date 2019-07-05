using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Leo
{
    public static class TurnOffDevice
    {
        [FunctionName("TurnOffDevice")]
        public static void Run([ServiceBusTrigger(Leo.turnOffDeviceQueueName, Connection = "ServiceBusConnectionString")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"{DateTime.Now} :: Turn Off Device Queue trigger function processing: {myQueueItem}");
            Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(myQueueItem);
            dict.TryGetValue("device", out string deviceName);
            if (deviceName != null)
            {
                string envVariable = deviceName + "Off";
                string deviceOffUrl = Environment.GetEnvironmentVariable(envVariable);
                if (deviceOffUrl != null) Leo.GetHttpResponse(log, deviceOffUrl, 3);
                else log.LogError($"Trigger for turning off {deviceName} not found.");
            } else
            {
                log.LogError($"\"device\" parameter not found.");
            }
        }
    }
}
