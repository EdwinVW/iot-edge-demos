# IoT Edge Demos
This repo contains several projects that are part of an IoT Edge demo solution. 

## Demo scenario
The demo scenario is built around a big Shopping Mall. The manager of the mall wants to know at all times how busy the mall is. In order to get the necessary information to determine this, each shop entry is fitted with a motion-sensor. This sensor can detect whether a customer is entering the shop or exiting the shop. Every sensor sends an event to a central system every time this happens. These events are used to build a nice graphical overview of the traffic going in and out of all the shops:

![](img/dashboard.png)

At the top of the dashboard you can see an overview of all the shops (on level 1). Every shop's sensor is indicated with a blue circle. If a customer enters or exists the shop, the sensor indicator will flash green or red accordingly. 

Directly below the map you can see a graph that shows how crowded every shop is. Every shop has a certain maximum capacity. The bar indicates how much of the this capacity is in use. 

The bottom graph will show the progression of the total amount of customers in the mall over time.

## High-level overview of the solution
The solution for the Mall Management system is created using the following Azure services:

| Azure Service | Description |
|:--|:--|
|Azure IoT Hub | This is the central management and communications hub. IoT Edge devices connect to this hub. | 
| Azure IoT Edge | This is used for running sensor software on IoT Edge devices and communicate events to the IoT Hub. |
| Azure Function | This is used to ingest all events sent to the IoT Hub and publish these to a SignalR hub. |
| Azure SignalR Service | This is used to publish all messages received from the Azure Function to all SignalR clients listening to the SignalR hub. |

The visual dashboard is built using ASP.NET Core 

![](img/solution-overview.png)

The solution works as follows:

1. Every door-sensor is simulated by a single IoT Edge module running on an IoT Edge device. Every sensor simulates customers going in or out of the shop at random intervals.
2. For every simulated customer, the Edge Module sends a message to the IoT Hub.
3. The event triggers an Azure Function app that reacts to events in the default IoT Hub event-queue.
4. The function app creates a message and sends it the SignalR hub.
5. The Visual Dashboard web-app gets the necessary credentials to connect to the SignalR service by calling the SignalRInfo Azure Function and connects to the hub.
6. The dashboard starts receiving messages from the SignalR hub.
7. With every incoming event, the dashboard updates the map and the graphs.

