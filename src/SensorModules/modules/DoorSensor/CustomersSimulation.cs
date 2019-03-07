using System;
using System.Threading;
using System.Threading.Tasks;

namespace DoorSensor
{
    public class CustomersSimulation
    {
        public int? SensorId { get; set; }
        public int? MaxCapacity { get; set; }
        public StoreStatus StoreStatus { get; set; } = StoreStatus.Closed;

        private string _deviceId;
        private string _moduleId;
        private int _customerCount { get; set; } = 0;

        private Mutex _mutex = new Mutex();

        private static Random _random = new Random();

        public CustomersSimulation(string deviceId, string moduleId)
        {
            _deviceId = deviceId;
            _moduleId = moduleId;
        }

        public async Task<DoorNotificationEvent> SimulateCustomerAsync(CancellationToken cancellationToken)
        {
            // simulate delay between customer movements
            await SimulateDelay(cancellationToken);

            // prevent event from being sent if properties have not been initialized (desired property)
            if (!SensorId.HasValue || !MaxCapacity.HasValue)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:ffffff")} - Sensor Id and/or MaxCapacity not set. Skipping event publication.");
                return new DoorNotificationEvent { IsInitialized = false };
            }

            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:ffffff")} - Customer detected. Sending message ...");

            // determine notificationtype
            NotificationType? notificationType = DetermineNotificationType();
            if (notificationType == null)
            {
                return new DoorNotificationEvent { IsInitialized = false };
            }

            //create event
            DoorNotificationEvent e = new DoorNotificationEvent
            {
                DeviceId = _deviceId,
                ModuleId = _moduleId,
                SensorId = SensorId.Value,
                MaxCapacity = MaxCapacity.Value,
                NotificationType = notificationType.Value,
                CustomerCount = _customerCount,
                StoreStatus = StoreStatus
            };

            return e;
        }

        public Task SetCustomerCountAsync(int newCustomerCount)
        {
            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:ffffff")} - Resetting CustomerCount to {newCustomerCount}");

            if (MaxCapacity.HasValue)
            {
                _mutex.WaitOne();
                _customerCount = newCustomerCount <= MaxCapacity.Value ? newCustomerCount : MaxCapacity.Value;
                _mutex.ReleaseMutex();
            }

            return Task.CompletedTask;
        }

        private async Task SimulateDelay(CancellationToken cancellationToken)
        {
            int delay;
            if (StoreStatus == StoreStatus.Open)
            {
                delay = _random.Next(500, 10000);
            }
            else
            {
                delay = _random.Next(500, 3000);
            }
            await Task.Delay(delay, cancellationToken);
        }

        private NotificationType? DetermineNotificationType()
        {
            _mutex.WaitOne();

            var notificationType = NotificationType.CustomerEntered;
            if (StoreStatus == StoreStatus.Closed)
            {
                if (_customerCount == 0)
                {
                    return NotificationType.StoreClosed;
                }
                notificationType = NotificationType.CustomerExited;
            }
            else if (_customerCount > 0)
            {
                notificationType = (NotificationType)_random.Next(2);
                if (_customerCount == MaxCapacity)
                {
                    notificationType = NotificationType.CustomerExited;
                }
            }

            // update customercount
            switch (notificationType)
            {
                case NotificationType.CustomerEntered:
                    _customerCount += 1;
                    break;
                case NotificationType.CustomerExited:
                    _customerCount -= 1;
                    break;
            }

            _mutex.ReleaseMutex();

            return notificationType;
        }
    }
}