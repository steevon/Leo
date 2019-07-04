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
            Leo.GetHttpResponse(Environment.GetEnvironmentVariable("ChimeOn"), log, 3);

            // Send Message to Service Bus
            string ServiceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
            queueClient = new QueueClient(ServiceBusConnectionString, QueueName);
            log.LogInformation($"{DateTime.Now} :: Sending message: {name}");
            try
            {
                await SendMessagesAsync(name, RingDuration);
            }
            catch (Exception exception)
            {
                log.LogError($"{DateTime.Now} :: Exception: {exception.Message}");
            }
            await queueClient.CloseAsync();

            // Return the response
            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        static async Task SendMessagesAsync(string messageBody, int delaySeconds)
        {
            // Create a new message to send to the queue
            var message = new Message(Encoding.UTF8.GetBytes(messageBody));
            message.ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddSeconds(delaySeconds);

            // Send the message to the queue
            await queueClient.SendAsync(message);
        }


    }
}
