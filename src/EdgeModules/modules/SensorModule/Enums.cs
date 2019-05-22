namespace SensorModule
{
    /// <summary>
    /// Notification type: [CustomerEntered | CustomerExited | StoreClosed]
    /// </summary>
    public enum NotificationType
    {
        CustomerEntered = 0,
        CustomerExited = 1,
        StoreClosed = 2
    }

    /// <summary>
    /// Status of the store: [Closed | Open]
    /// </summary>
    public enum StoreStatus
    {
        Closed = 0,
        Open = 1
    }
}