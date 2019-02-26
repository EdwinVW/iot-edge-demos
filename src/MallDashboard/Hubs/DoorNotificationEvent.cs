namespace MallDashboard.Hubs
{
    public enum NotificationType
    {
        CustomerEntered,
        CustomerExited
    }

    public class DoorNotificationEvent
    {
        public string DoorId { get; set; }
        public NotificationType NotificationType { get; set; }
    }
}