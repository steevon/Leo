using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Leo
{

    public static class Leo
    {
        public const string turnOffDeviceQueueName = "turn-off-device";

        [FunctionName("Leo")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name ??= data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        public static HttpWebResponse GetHttpResponse(ILogger log, string url, int retry = 0, Dictionary<string, string> headers=null)
        {
            int count = 0;
            log.LogInformation($"Getting response from {url}");
            HttpWebResponse httpResponse = null;
            do
            {
                // Wait before retry.
                if (count > 0)
                {
                    System.Threading.Thread.Sleep(500 * count);
                    log.LogInformation($"Retrying...{count}");
                }
                WebRequest request = WebRequest.Create(url);
                
                if (headers != null)
                {
                    foreach(KeyValuePair<string, string> header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                    log.LogInformation($"Headers: {request.Headers.AllKeys.ToString()}");
                    log.LogInformation($"Bearer: {request.Headers.Get("Authorization")}");
                }
                try
                {
                    WebResponse response = request.GetResponse();
                    httpResponse = (HttpWebResponse)response;
                    log.LogInformation($"HTTP Reponse: {httpResponse.StatusDescription}");
                }
                catch (WebException ex)
                {
                    httpResponse = (HttpWebResponse)ex.Response;
                    log.LogInformation($"Error: {ex.Status}");
                } 
                count += 1;
            }
            while (count <= retry && httpResponse.StatusCode != HttpStatusCode.OK);
            return httpResponse;
        }

        public static string GetStringResponse(ILogger log, string url, int retry = 0, Dictionary<string, string> headers = null)
        {

            HttpWebResponse response = GetHttpResponse(log, url, retry, headers);
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

        public static dynamic GetJSONResponse(ILogger log, string url, int retry = 0, Dictionary<string, string> headers = null)
        {
            string responseString = GetStringResponse(log, url, retry, headers);
            return JsonConvert.DeserializeObject(responseString);
        }

        public static async Task<dynamic> PostJSONResponse(ILogger log, string url, dynamic data, Dictionary<string, string> headers = null)
        {
            HttpClient client = new HttpClient();
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
                log.LogInformation($"Added headers {client.DefaultRequestHeaders}");
            }
            string content = JsonConvert.SerializeObject(data);
            var task = client.PostAsync(url, new StringContent(content, Encoding.UTF8, "application/json"));
            HttpResponseMessage response = task.Result;
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject(responseString);
        }

        public static async Task<DateTime?> SendMessageAsync(ILogger log, string queueName, string messageText, int delaySeconds = 0)
        {
            // Send Message to Service Bus
            string ServiceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
            IQueueClient queueClient = new QueueClient(ServiceBusConnectionString, queueName);
            log.LogInformation($"Sending message: {messageText}");
            DateTime? messageEnqueueTime = null;
            try
            {
                // Create a new message to send to the queue
                var message = new Message(Encoding.UTF8.GetBytes(messageText))
                {
                    ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddSeconds(delaySeconds)
                };

                // Send the message to the queue
                await queueClient.SendAsync(message);
                messageEnqueueTime = message.ScheduledEnqueueTimeUtc;
            }
            catch (Exception exception)
            {
                log.LogError($"Exception: {exception.Message}");
            }
            await queueClient.CloseAsync();
            return messageEnqueueTime;
        }

        public static bool AtNight(ILogger log)
        {
            // Get home coordinates from environment variable.
            string homeCoordinates = Environment.GetEnvironmentVariable("HomeCoordinates");
            // Use the coordinates of Liberty Island as default home coordinates.
            if (homeCoordinates == null) homeCoordinates = "lat=40.689428&lng=-74.044529";
            dynamic dict = GetJSONResponse(log, "https://api.sunrise-sunset.org/json?" + homeCoordinates + "&formatted=0", 3);
            JObject results = dict["results"];
            DateTime sunrise = Convert.ToDateTime(results["sunrise"]);
            log.LogInformation($"Sunrise time is {sunrise}.");
            DateTime sunset = Convert.ToDateTime(results["sunset"]);
            log.LogInformation($"Sunset time is {sunset}.");
            DateTime now = DateTime.Now;
            log.LogInformation($"Current time is {now}.");
            if (now > sunrise && now < sunset)
            {
                return false;
            }
            return true;
        }

        public static string MillisecondsToLocalTimeString(Int64 milliseconds)
        {
            DateTime utc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(milliseconds);
            DateTime localTime;
            string timezone = Environment.GetEnvironmentVariable("TimeZone");
            if (timezone != null)
            {
                try
                {
                    TimeZoneInfo tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                    localTime = TimeZoneInfo.ConvertTimeFromUtc(utc, tzInfo);
                }
                catch (Exception ex)
                {
                    if (ex.GetType() == typeof(TimeZoneNotFoundException))
                    {
                        string info = ex.Message + "\nAvailable Time Zones:\n";
                        ReadOnlyCollection<TimeZoneInfo> timeZones = TimeZoneInfo.GetSystemTimeZones();
                        foreach (TimeZoneInfo timeZone in timeZones)
                        {
                            info += $"{timeZone.Id}\n";
                        }
                        ex = new TimeZoneNotFoundException(info);
                    }
                    throw ex;
                }
            }
            else
            {
                localTime = utc;
            }

            return localTime.ToShortTimeString();
        }
    }
}
