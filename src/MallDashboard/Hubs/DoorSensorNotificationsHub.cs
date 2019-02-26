using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace MallDashboard.Hubs
{
    public class DoorSensorNotificationsHub : Hub
    {
        public async Task SendNotification(string doorNotification)
        {
            DoorNotificationEvent e = JsonConvert.DeserializeObject<DoorNotificationEvent>(doorNotification);
            await Clients.All.SendAsync("DoorSensorNotification", e.DoorId);
        }
    }
}