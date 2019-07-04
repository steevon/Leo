using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Leo
{
    public static class StopChime
    {
        [FunctionName("StopChime")]
        public static void Run([ServiceBusTrigger("chime", Connection = "ServiceBusConnectionString")]string myQueueItem, ILogger log)
        {
            // Stop Chime
            Leo.GetHttpResponse(log, Environment.GetEnvironmentVariable("ChimeOff"), 3);
            string log_message = $"{DateTime.Now} :: ServiceBus queue trigger function processed message: {myQueueItem}";
            log.LogInformation(log_message);
        }
    }
}
