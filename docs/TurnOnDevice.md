# Turn on a device for a certain amount of time
**Function Name**: TurnOnDevice

## Environment Variables
This function uses the following environment variables:
* HomeCoordinates

## Parameters
This function accepts both GET and POST requests, and the following parameters as query string in GET request or JSON body in POST request.
* device: the name of the device to be turned on, e.g. "FrontDoorLights".
* duration: the minimum duration in seconds for the device to be turned on.
* condition: either "day" or "night", indicates whether the device should be turned on during day or night only.
* sender: an identifier for the trigger. This is for logging purpose only.

## Example
The following example assumes:
* "FrontDoorLights" as device name
* `FrontDoorLightsOn` and `FrontDoorLightsOff` are configured as environment variables containing the URLs for turing on and off "FrontDoorLights"
* `HomeCoordinates` is configured as environment variables containing the coordinates of a desired location (e.g. home).

Visiting this URL will turn on "FrontDoorLights" for 1 minutes after sunset:
```
https://[YOUR_APP_NAME].azurewebsites.net/api/TurnOnDevice?code=[XXXXXXXXXXXXXXXXX]&device=FrontDoorLights&condition=night&duration=60&sender=[YOUR_NAME]
```
