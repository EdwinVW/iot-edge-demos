using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using System.Threading.Tasks;
using System.Text;

namespace Functions
{
    public class DoorSensorNotificationPublisher
    {
        [FunctionName("PublishDoorSensorNotification")]
        public void Run(
            [IoTHubTrigger("messages/events", Connection = "IOTHUB_EVENTS")] EventData iotHubMessage,
            [SignalR(HubName = "DoorSensorNotificationsHub")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            string json = Encoding.UTF8.GetString(iotHubMessage.Body);

            log.LogInformation($"C# IoT Hub trigger function processed a message: {json}");


            signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "SendNotification",
                    Arguments = new[] { json }
                }).Wait();

            log.LogInformation($"Message broadcast using SignalR");
        }
    }
}