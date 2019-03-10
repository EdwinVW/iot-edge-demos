using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text;

using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace Functions
{
    public class DoorSensorNotificationPublisher
    {
        [FunctionName("PublishDoorSensorNotification")]
        public async Task Run(
            [IoTHubTrigger("messages/events", Connection = "IOTHUB_EVENTS", ConsumerGroup = "MallManagement")] EventData iotHubMessage,
            [SignalR(HubName = "DoorSensorNotificationsHub")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            string json = Encoding.UTF8.GetString(iotHubMessage.Body);

            log.LogInformation($"C# IoT Hub trigger function processed a message: {json}");

            await signalRMessages.AddAsync(
                new SignalRMessage
                {
                    Target = "SendNotification",
                    Arguments = new[] { json }
                });
        }
    }
}