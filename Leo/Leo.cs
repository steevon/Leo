using System;
using System.Net;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Azure.ServiceBus;

namespace Leo
{

    public static class Leo
    {
        [FunctionName("Leo")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        public static string GetStringResponse(string url, ILogger log, int retry = 0)
        {

            HttpWebResponse response = GetHttpResponse(url, log, retry);
            string responseString;
            // Get the stream containing content returned by the server. 
            // The using block ensures the stream is automatically closed. 
            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.  
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.  
                responseString = reader.ReadToEnd();
            }
            // Close the response.  
            response.Close();
            return responseString;
        }

        public static HttpWebResponse GetHttpResponse(string url, ILogger log, int retry = 0)
        {
            int count = 0;
            HttpWebResponse httpResponse;
            do
            {
                // Wait 200ms before retry.
                if (count > 0)
                {
                    System.Threading.Thread.Sleep(200);
                    log.LogInformation($"Retrying...{count}");
                }
                WebRequest request = WebRequest.Create(url);
                WebResponse response = request.GetResponse();
                httpResponse = (HttpWebResponse)response;
                count += 1;
                log.LogInformation($"HTTP Reponse: {httpResponse.StatusDescription}");
            }
            while (count <= retry && httpResponse.StatusCode != HttpStatusCode.OK);
            return httpResponse;
        }

        static IQueueClient queueClient;
        public static async Task ScheduleOff(string queueName, string message, int seconds, ILogger log)
        {
            // Send Message to Service Bus
            string ServiceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
            queueClient = new QueueClient(ServiceBusConnectionString, queueName);
            log.LogInformation($"{DateTime.Now} :: Sending message: {message}");
            try
            {
                await SendMessagesAsync(message, seconds);
            }
            catch (Exception exception)
            {
                log.LogError($"{DateTime.Now} :: Exception: {exception.Message}");
            }
            await queueClient.CloseAsync();
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
