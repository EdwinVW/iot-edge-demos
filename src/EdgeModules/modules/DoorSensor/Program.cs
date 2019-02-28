using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace DoorSensor
{
    class Program
    {
        private static string _deviceId;
        private static string _moduleId;
        private static int? _sensorId = null;
        private static int? _maxCapacity;
        private static StoreStatus _storeStatus = StoreStatus.Closed;
        private static int _customerCount = 0;
        private static Random _random = new Random();

        public static int Main() => MainAsync().Result;

        private static async Task<int> MainAsync()
        {
            try
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:ffffff")} - Starting module initialization ...");

                ModuleClient moduleClient = await InitAsync();

                // read module twin's desired properties
                var moduleTwin = await moduleClient.GetTwinAsync();
                await OnDesiredPropertiesUpdate(moduleTwin.Properties.Desired, moduleClient);

                // attach a callback for updates to the module twin's desired properties
                await moduleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

                // start message loop
                Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:ffffff")} - Starting message loop ...");
                var cts = new CancellationTokenSource();
                AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
                Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
                await MessageLoopAsync(moduleClient, cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Message loop that simulates people entering the store.
        /// </summary>
        /// <param name="moduleClient">The IoT Edge communication client.</param>
        /// <param name="cancellationToken">The cancellation,token for gracefully handling cancellation.</param>
        private static async Task MessageLoopAsync(ModuleClient moduleClient, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                // simulate delay between customer movements
                await SimulateDelay(cancellationToken);

                // prevent event from being sent if properties have not been initialized (desired property)
                if (!_sensorId.HasValue || !_maxCapacity.HasValue)
                {
                    Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:ffffff")} - Sensor Id and/or MaxCapacity not set. Skipping event publication.");
                    continue;
                }

                try
                {
                    Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:ffffff")} - Customer entered. Sending message ...");

                    // determine notificationtype
                    NotificationType? notificationType = DetermineNotificationType();
                    if (notificationType == null)
                    {
                        continue;
                    }

                    // update customercount
                    switch(notificationType)
                    {
                        case NotificationType.CustomerEntered:
                            _customerCount += 1;
                            break;
                        case NotificationType.CustomerExited:
                            _customerCount -= 1;
                            break;
                    }

                    //create event
                    DoorNotificationEvent e = new DoorNotificationEvent
                    {
                        DeviceId = _deviceId,
                        ModuleId = _moduleId,
                        SensorId = _sensorId.Value,
                        MaxCapacity = _maxCapacity.Value,
                        NotificationType = notificationType.Value,
                        CustomerCount = _customerCount,
                        StoreStatus = _storeStatus
                    };
                    string messageString = JsonConvert.SerializeObject(e);
                    var message = new Message(Encoding.UTF8.GetBytes(messageString));

                    // send event
                    await moduleClient.SendEventAsync("doorevents", message);

                    Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:ffffff")} - Message sent: {messageString}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    retryCount++;
                    if (retryCount == 10)
                    {
                        break;
                    }
                }
            }
        }

        private static async Task SimulateDelay(CancellationToken cancellationToken)
        {
            int delay;
            if (_storeStatus == StoreStatus.Open)
            {
                delay = _random.Next(500, 10000);
            }
            else
            {
                delay = _random.Next(500, 3000);
            }
            await Task.Delay(delay, cancellationToken);
        }

        private static NotificationType? DetermineNotificationType()
        {
            var notificationType = NotificationType.CustomerEntered;
            if (_storeStatus == StoreStatus.Closed)
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
                if (_customerCount == _maxCapacity)
                {
                    notificationType = NotificationType.CustomerExited;
                }
            }
            return notificationType;
        }

        private static async Task<ModuleClient> InitAsync()
        {
            // store Ids
            _deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            _moduleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");

            ITransportSettings[] transportSettings = { new AmqpTransportSettings(TransportType.Amqp_Tcp_Only) };

            // Open a connection to the Edge runtime
            ModuleClient client = await ModuleClient.CreateFromEnvironmentAsync(transportSettings);
            await client.OpenAsync();
            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:ffffff")} - IoT Hub module client initialized.");

            return client;
        }

        static Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Console.WriteLine("Desired property change:");
                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));

                if (desiredProperties["SensorId"] != null)
                {
                    _sensorId = desiredProperties["SensorId"];
                }

                if (desiredProperties["MaxCapacity"] != null)
                {
                    _maxCapacity = desiredProperties["MaxCapacity"];
                }

                if (desiredProperties["StoreStatus"] != null)
                {
                    _storeStatus = (StoreStatus)desiredProperties["StoreStatus"];
                }
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when receiving desired property: {0}", exception);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
            }
            return Task.CompletedTask;
        }
    }
}
