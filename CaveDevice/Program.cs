using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
namespace CaveDevice
{
    class Program
    {
        // Contains methods that a device can use to send messages to and receive from an IoT Hub.
        private static DeviceClient deviceClient;

        private readonly static string connectionString = "HostName=iot-hub-az220-pj130622.azure-devices.net;DeviceId=cavetemp-device5454;SharedAccessKey=OSWvB/xkXYcpEdC38Y7l9E+hsLL/UEmY5O3cTpJhzMs=";
        private static void Main(string[] args)
        {
            Console.WriteLine("IoT Hub C# Simulated Cave Device. Ctrl-C to exit.\n");
            //enroll with dps

            // Connect to the IoT hub using the MQTT protocol
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
            SendDeviceToCloudMessagesAsync();
            ReceiveC2dAsync();
            Console.ReadLine();
        }
        private static async void SendDeviceToCloudMessagesAsync()
        {
            // Create an instance of our sensor
            var sensor = new EnvironmentSensor();

            while (true)
            {

                var currentTemperature = sensor.ReadTemperature();
                var currentHumidity = sensor.ReadHumidity();

                var messageString = CreateMessageString(currentTemperature, currentHumidity);


                var message = new Message(Encoding.ASCII.GetBytes(messageString));
                ///filter generated data with query         
                message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");
                if (currentTemperature > 30)
                {
                    message.Properties.Add("path", "warm");
                    message.Properties.Add("severity", "5");
                }
                message.Properties.Add("timestamp", GetTimestamp(DateTime.Now));
                message.ContentType = "application/json";
                message.ContentEncoding = "utf-8";

                // Send the telemetry message
                await deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", GetTimestamp(DateTime.Now), messageString);

                await Task.Delay(300);
            }
        }
        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }
        private static string CreateMessageString(double temperature, double humidity)
        {

            var telemetryDataPoint = new
            {
                temperature = temperature,
                humidity = humidity,
                time = DateTime.Now
            };
            //https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fazure%2Fiotedge-vm-deploy%2F1.2.0%2FedgeDeploy.json
            // Create a JSON string from the anonymous object
            return JsonConvert.SerializeObject(telemetryDataPoint);
        }
        private static async void ReceiveC2dAsync()
        {
            Console.WriteLine("\nReceiving cloud to device messages from service");
            while (true)
            {
                Message receivedMessage = await deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Received message: {0}",
                Encoding.ASCII.GetString(receivedMessage.GetBytes()));
                Console.ResetColor();

                await deviceClient.CompleteAsync(receivedMessage);
            }
        }
    }
    internal class EnvironmentSensor
    {
        // Initial telemetry values
        double minTemperature = 20;
        double minHumidity = 60;
        Random rand = new Random();

        internal EnvironmentSensor()
        {
            // device initialization could occur here
        }

        internal double ReadTemperature()
        {
            return minTemperature + rand.NextDouble() * 15;
        }

        internal double ReadHumidity()
        {
            return minHumidity + rand.NextDouble() * 20;
        }
    }
}