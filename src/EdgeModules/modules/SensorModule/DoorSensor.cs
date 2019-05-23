using System;
using System.Net;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace SensorModule
{
    public static class DoorSensor
    {
        private static CustomersSimulation _customersSimulation;

        public static async Task<int> RunAsync()
        {
            try
            {
                // get Ids
                string deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
                string moduleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");

                // init customers simulation
                _customersSimulation = new CustomersSimulation(deviceId, moduleId);

                // initialize module
                Log("Starting module initialization ...");
                ModuleClient moduleClient = await InitAsync();

                // read module twin's desired properties
                var moduleTwin = await moduleClient.GetTwinAsync();
                await OnDesiredPropertiesUpdate(moduleTwin.Properties.Desired, moduleClient);

                // attach a callback for updates to the module twin's desired properties
                await moduleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

                // attach a callback for direct method 'SetStoreStatus'
                await moduleClient.SetMethodHandlerAsync("SetCustomerCount", OnSetCustomerCount, null);

                // start message loop
                Log("Starting message loop ...");
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
                // simulate customer
                SensorNotification notification = await _customersSimulation.SimulateCustomerAsync(cancellationToken);

                // prevent event from being sent if module has not yet been initialized properly (desired property)
                if (!notification.IsInitialized)
                {
                    Log("Sensor module not yet initialized properly. Skipping event publication.");
                    continue;
                }

                try
                {
                    Log("Customer entered. Sending message ...");

                    string messageString = JsonConvert.SerializeObject(notification);
                    var message = new Message(Encoding.UTF8.GetBytes(messageString));

                    // send event
                    await moduleClient.SendEventAsync("doorevents", message);

                    Log($"Message sent: {messageString}");
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

        /// <summary>
        /// Initialize the edge module.
        /// </summary>
        /// <returns>An initialized client for communicating with IoT Hub.</returns>
        private static async Task<ModuleClient> InitAsync()
        {
            ITransportSettings[] transportSettings = { new AmqpTransportSettings(TransportType.Amqp_Tcp_Only) };

            // Open a connection to the Edge runtime
            ModuleClient client = await ModuleClient.CreateFromEnvironmentAsync(transportSettings);
            await client.OpenAsync();
            Log("IoT Hub module client initialized.");

            return client;
        }

        /// <summary>
        /// Handle updates on the desired properties.
        /// </summary>
        static Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Log($"Desired property change:\n{JsonConvert.SerializeObject(desiredProperties)}");

                if (desiredProperties["SensorId"] != null)
                {
                    _customersSimulation.SensorId = desiredProperties["SensorId"];
                }

                if (desiredProperties["MaxCapacity"] != null)
                {
                    _customersSimulation.MaxCapacity = desiredProperties["MaxCapacity"];
                }

                if (desiredProperties["StoreStatus"] != null)
                {
                    _customersSimulation.StoreStatus = (StoreStatus)desiredProperties["StoreStatus"];
                }
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Log($"Error when receiving desired property: {exception.ToString()}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error when receiving desired property: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handle SetCustomerCount method.
        /// </summary>
        private static async Task<MethodResponse> OnSetCustomerCount(MethodRequest methodRequest, object userContext)
        {
            Log($"Direct Method received. Payload: {methodRequest.DataAsJson}");

            try
            {
                var payload = new { CustomerCount = 0 };
                var input = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, payload);
                await _customersSimulation.SetCustomerCountAsync(input.CustomerCount);
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                return new MethodResponse((int)HttpStatusCode.InternalServerError);
            }
            return new MethodResponse((int)HttpStatusCode.OK);
        }        

        /// <summary>
        /// Log to the console with a timestamp.
        /// </summary>
        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd hh:mm:ss:ffffff")} - {message}");
        }
    }
}