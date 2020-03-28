# Turn on a Device
This function sends out a HTTP request to turn on a device, and a message to Azure Service Bus to turn off the device after a certain amount of time.

**Function Name**: TurnOnDevice

**Trigger**: HTTP Request

## Environment Variables
This function uses the following environment variables:
* **ServiceBusConnectionString**. An Azure Service Bus must be created with a queue named "turn-off-device" in order to schedule the triggers for turning off the device.
* **HomeCoordinates**. Latitude and longitude coordinates used for determining whether it is day or night when the function is triggered (based on the sunrise/sunset time). They should be in the format of `lat=XX.XXXX&lng=XX.XXXX`. For example, the coordinates for New York City is `lat=40.7128&lng=-74.0060`.
* **[DEVICE_NAME]On**. URL for turning on a device, where [DEVICE_NAME] is the name of the device.
* **[DEVICE_NAME]Off**. URL for turning off a device, where [DEVICE_NAME] is the name of the device.

## Parameters
This function accepts both GET and POST requests, and the following parameters as query string in GET request or JSON body in POST request.
* device: the name of the device to be turned on, e.g. "FrontDoorLights".
* duration: the minimum duration in seconds for the device to be turned on.
* condition: either "day" or "night", indicates whether the device should be turned on during day or night only.
* sender: an identifier for the trigger. This is for logging purpose only.
* skip: Indicate whether to skip turning on the device. Set skip to a value if you would like to schedule turning off the device only.

## Example
The following example assumes:
* "FrontDoorLights" as device name
* `FrontDoorLightsOn` and `FrontDoorLightsOff` are configured as environment variables containing the URLs for turing on and off "FrontDoorLights"
* `HomeCoordinates` is configured as environment variables containing the coordinates of a desired location (e.g. home).

Visiting this URL will turn on "FrontDoorLights" for 1 minutes after sunset:
```
https://[YOUR_APP_NAME].azurewebsites.net/api/TurnOnDevice?code=[XXXXXXXXXXXXXXXXX]&device=FrontDoorLights&condition=night&duration=60&sender=[YOUR_NAME]
```
## Scheduling the device to be turned off
When the `duration` parameter is specified in the HTTP request, the device will be scheduled to turned off after the duration. This is achieved by sending a JSON serialized dictionary to the Azure Service Bus queue. The dictionary contains two keys, sender and device, which are the same as the ones in the HTTP request.

See also: [Turn off Device](TurnOffDevice.md)
