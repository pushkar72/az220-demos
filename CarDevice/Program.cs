using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
namespace CarDevice
{
    class Program
    {
        // Contains methods that a device can use to send messages to and receive from an IoT Hub.
        private static DeviceClient deviceClient;

       // private readonly static string connectionString = "HostName=iot-hub-az220-pj130622.azure-devices.net;DeviceId=car-device;SharedAccessKey=Jw52z4DXxNooPKLhrpveojZUVKlkLNwdBzFO6yvM/Vk=";
       private readonly static string connectionString= "HostName=iot-hub-az220-pj130622.azure-devices.net;DeviceId=car-device-2;SharedAccessKey=J6BXbkiO7sIt29u/4GKRYwEncGZAOptwZd7BXgKeOHg=";
        private static void Main(string[] args)
        {
            Console.WriteLine("IoT Hub C# Simulated Car Device. Ctrl-C to exit.\n");
            //enroll with dps

            // Connect to the IoT hub using the MQTT protocol
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
            SendDeviceToCloudMessagesAsync();
           
            Console.ReadLine();
        }
        private static async void SendDeviceToCloudMessagesAsync()
        {
            // Create an instance of our sensor
            var sensor = new CarSensor();
            Console.ReadLine();
            //Jw52z4DXxNooPKLhrpveojZUVKlkLNwdBzFO6yvM/Vk=
            while (true)
            {

                var currentSpeed = sensor.ReadSpeed();
              

                var messageString = CreateMessageString(currentSpeed);


                var message = new Message(Encoding.ASCII.GetBytes(messageString));
                ///filter generated data with query         
                message.Properties.Add("speedAlert", (currentSpeed > 100) ? "true" : "false");
                message.Properties.Add("timestamp", DateTime.Now.ToString());
                message.ContentType = "application/json";
                message.ContentEncoding = "utf-8";

                // Send the telemetry message
                await deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now.ToString(), messageString);

                await Task.Delay(300);
            }
        }
        
        private static string CreateMessageString(double speed)
        {

            var telemetryDataPoint = new
            {
                speed = speed,
                time = DateTime.Now
            };
            //https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fazure%2Fiotedge-vm-deploy%2F1.2.0%2FedgeDeploy.json
            // Create a JSON string from the anonymous object
            return JsonConvert.SerializeObject(telemetryDataPoint);
        }
       
    }
     
    internal class CarSensor
    {
        string moduleconnectionString="HostName=iot-hub-az220-pj130622.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=nv5MneSzb9bUbZRdr7RajoT2fyba7vmdVtndBe8FtS4=";        
        Microsoft.Azure.Devices.RegistryManager registryManager;
        Microsoft.Azure.Devices.Shared.Twin twin;
        // Initial telemetry values
        double maxspeed = 0;
        Random rand = new Random();

        internal CarSensor()
        {
           
            registryManager=Microsoft.Azure.Devices.RegistryManager.CreateFromConnectionString(moduleconnectionString);
            twin=registryManager.GetTwinAsync("car-device", "speed-module").GetAwaiter().GetResult();
            var prope=twin.Properties.Desired["maxSpeed"];
            maxspeed=prope.Value;

        }

        internal double ReadSpeed()
        {
            return maxspeed + rand.NextDouble() * 15;
        }

 
    }
}