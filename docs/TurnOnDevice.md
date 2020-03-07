## Turn On a device by HTTP trigger, and Turn it Off Automatically
Once deployed, URL for triggering the "Turn On Device" function can be obtain from Azure Portal (See [Test the function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function#test-the-function)).

The URL should look like:
```
https://[YOUR_APP_NAME].azurewebsites.net/api/TurnOnDevice?code=[XXXXXXXXXXXXXXXXX]
```

The function accepts both GET and POST requests, and the following parameters as query string in GET request or JSON body in POST request.
* device: the name of the device to be turned on, e.g. "FrontDoorLights".
* duration: the minimum duration in seconds for the device to be turned on.
* condition: either "day" or "night", indicates whether the device should be turned on during day or night only.
* sender: an identifier for the trigger. This is for logging purpose only.

The following example assumes:
* "FrontDoorLights" as device name
* `FrontDoorLightsOn` and `FrontDoorLightsOff` are configured as environment variables containing the URLs for turing on and off "FrontDoorLights"
* `HomeCoordinates` is configured as environment variables containing the coordinates of a desired location (e.g. home).

Visiting this URL will turn on "FrontDoorLights" for 1 minutes after sunset:
```
https://[YOUR_APP_NAME].azurewebsites.net/api/TurnOnDevice?code=[XXXXXXXXXXXXXXXXX]&device=FrontDoorLights&condition=night&duration=60&sender=[YOUR_NAME]
```

You can test the function by visiting the link using the web browser on your computer or your phone. Once it is working, you can integrate this with [IFTTT webhooks](https://ifttt.com/maker_webhooks) to build your awesome automation project.
