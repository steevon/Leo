using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Leo
{
    public static class TurnOffDevice
    {
        [FunctionName("TurnOffDevice")]
        public static void Run([QueueTrigger("turn_off_device", Connection = "ServiceBusConnectionString")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"{DateTime.Now} :: Turn Off Device Queue trigger function processing: {myQueueItem}");
            Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(myQueueItem);
            string deviceName = dict["device"];
            string envVariable = deviceName + "Off";
            Leo.GetHttpResponse(log, Environment.GetEnvironmentVariable(envVariable), 3);
        }
    }
}
