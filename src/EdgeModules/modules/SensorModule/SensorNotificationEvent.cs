namespace SensorModule
{  
    public class SensorNotification
    {
        public bool IsInitialized { get; set; } = true;
        public string DeviceId { get; set; }
        public string ModuleId { get; set; }
        public int SensorId { get; set; }
        public int MaxCapacity { get; set; }
        public NotificationType NotificationType { get; set; }
        public int CustomerCount { get; set; }
        public StoreStatus StoreStatus { get; set; }
    }
}