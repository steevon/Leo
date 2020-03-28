# Turn off a Device
This function receives a message from Azure Service Bus and turn off a device by sending out a HTTP request.

**Function Name**: TurnOffDevice

**Trigger**: Azure Service Bus

## Environment Variables
* **[DEVICE_NAME]Off**. URL for turning off a device, where [DEVICE_NAME] is the name of the device.

## Parameters
This function is triggered by a message from Azure Service Bus. The message should be a JSON serialized dictionary containing two keys:
* sender: The sender of the message, for logging purpose.
* device: The device to be turned off, e.g. "FrontDoorLights". A corresponding URL must be defined in the environment variables (settings) for turning off the device.
