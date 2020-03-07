# Leo
_Qiu's Home Automation Solution with the Azure Functions_

We can easily do something like "Turn on the porch light when someone is at the front door" with smart home devices. However, it is not easy to find a way to turn off the light automatically after a delay (say, after 1 minutes). This Function App starts with providing a solution to turn on the devices for a certain amount of time.

This App is intended to be used with [IFTTT webhooks](https://ifttt.com/maker_webhooks) to create applets like "Turn on the lights when there is motion at the front door".

## Features
This Function App compliments the smart home with the following features:
* [Turn on a device by HTTP trigger, and automatically turn it off after a certain amount of time](docs/TurnOnDevice.md)
* [Trigger actions base on Ring Alarm System status](docs/Ring.md)

## Azure Function App Deployment
This solution uses [Azure Functions](https://azure.microsoft.com/en-us/services/functions/) to run event-driven on-demand functions on the cloud. This solution can be deployed to Azure with a consumption plan using Visual Studio 2019. The free grant for consumption plan should allow well over 1,000 executions per day. The function for turning off devices uses Azure Service Bus, which may incur a small cost (within $0.05 USD per month).

Deployment includes the following steps:
1. Create an Azure Function App (through Azure Portal or Visual Studio).
2. Create an Azure Service Bus and a queue named "turn-off-device".
3. Configure the environment variables, including the Service Bus connection string, Storage Account connection string, and URLs for turning the devices on and off.

See also: [Develop Azure Functions using Visual Studio](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-vs#publish-to-azure)

## Azure Service Bus
A [Service Bus](https://docs.microsoft.com/en-us/azure/service-bus-messaging/) queue is used to keep messages for turning devices off. Azure Service Bus provides an option to schedule the enqueue time of a message. This feature is used in this app to schedule the devices to be turned off. This function app uses a queue named "turn-off-device". The name is hard coded in a constant variable named `turnOffDeviceQueueName` in `Leo.cs`. A queue with this name must be created in the Service Bus before running the function app.

See also: [Use Azure portal to create a Service Bus queue](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quickstart-portal)

## Environment Variables
Environment Variables can be configured through Azure Portal.

The Service Bus connection string must be stored as a variable named "ServiceBusConnectionString" in the application settings.

This app turns devices on and off by sending HTTP GET requests. In general, each device to be controlled should have two environment variables configured in the application settings of the Function App (via Azure Portal). The variables should contain the URLs for turning the device on and off. For example, if you would like to use "FrontDoorLights" as device name, then `FrontDoorLightsOn` and `FrontDoorLightsOff` should be set as environment variables.

See also: [How to manage a function app in the Azure portal](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings#settings)

## Local Development
This repository is a Visual Studio solution containing a single Visual Studio project. The solution is designed with Visual Studio 2019.

Local development requires:
* A Service Bus queue named "turn-off-device" on Azure is required for local development.
* A Storage Account connection string. This will be the value of "AzureWebJobsStorage" in local settings.
* A local setting file: `local.settings.json`.

The `local.settings.json` required for local development is not included in this repository. This file stores the environment variables for local development. The environment variables include credentials and personalized links for turning the devices on and off.

Here is an example of the file:
```
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "XXXXXXXXXXXXXXXX",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "ServiceBusConnectionString": "XXXXXXXXXXXXXXXX",
    "HomeCoordinates": "lat=40.689428&lng=-74.044529",
    "FrontDoorLightsOn": "XXXXXXXXXXXXXXXX",
    "FrontDoorLightsOff": "XXXXXXXXXXXXXXXX"
  }
}
```
* `AzureWebJobsStorage` is connection string for storage account. It can be obtained from the Azure Portal once the Function App is created. 
* `ServiceBusConnectionString` is connection string for Service Bus. It can be obtained once the Service Bus is created.
* `HomeCoordinates` contains the latitude and longitude of the location for determining sunrise and sunset time.
* `FrontDoorLightsOn` and `FrontDoorLightsOff` are URLs for turning device On/Off. Here "FrontDoorLights" is the device name.

See also: [Work with Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#local-settings-file)
