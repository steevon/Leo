using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;

namespace Leo
{
    public static class AuthenticateGoogle
    {
        [FunctionName("AuthenticateGoogle")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("HTTP trigger function processed Authenticate Google request.");
            string endpoint = "https://accounts.google.com/o/oauth2/v2/auth";
            string redirect = "http://localhost:7071/api/AuthenticateGoogle";
            
            string clientID = Environment.GetEnvironmentVariable("GoogleClientID");
            string clientSecret = Environment.GetEnvironmentVariable("GoogleClientSecret");
            string authUrl = endpoint + "?client_id=" + clientID + "&redirect_uri=" + redirect + "&access_type=offline" +
                "&scope=https://www.googleapis.com/auth/gmail.readonly" +
                "&response_type=code";

            string scope = req.Query["scope"];
            string code = req.Query["code"];
            if (code == null) return new RedirectResult(authUrl);

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientID },
                { "client_secret", clientSecret },
                { "redirect_uri", redirect },
                { "grant_type", "authorization_code" }
            };
            dynamic token_response = Leo.PostJSONResponse(log, "https://www.googleapis.com/oauth2/v4/token", data);

            return new OkObjectResult(
                $"Code: {code}\n" +
                $"Scope: {scope}\n" +
                $"Access Token: {token_response?.access_token}\n" +
                $"Refresh Token: {token_response?.refresh_token}\n" +
                $"Expires In: {token_response?.expires_in}");
        }

        public static async Task<string> RefreshAccessToken(ILogger log)
        {
            log.LogInformation("Refreshing Access Token");
            string clientID = Environment.GetEnvironmentVariable("GoogleClientID");
            string clientSecret = Environment.GetEnvironmentVariable("GoogleClientSecret");
            string refreshToken = Environment.GetEnvironmentVariable("GmailRefreshToken");
            
            // Get access token
            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "refresh_token", refreshToken },
                { "client_id", clientID },
                { "client_secret", clientSecret },
                { "grant_type", "refresh_token" }
            };
            dynamic token_response = await Leo.PostJSONResponse(log, "https://www.googleapis.com/oauth2/v4/token", data);
            return token_response?.access_token;
        }
    }
}
