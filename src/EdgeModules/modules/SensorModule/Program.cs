namespace SensorModule
{
    class Program
    {
        public static int Main() => DoorSensor.RunAsync().Result;
    }
}
