using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
namespace CaveDeviceReceiver
{
    class Program
    {
        private static DeviceClient deviceClient;
        private readonly static string connectionString = "HostName=iot-az-220-pj020522.azure-devices.net;DeviceId=sensor-th-0001;SharedAccessKey=ImZvu/gxN1N7PlnmmXxnvNoMDR2U3+3Fcoj0i+8moYE=";
        private readonly static string connectionString2 = "HostName=iot-az-220-pj020522.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=ZuzI0VC6bxFjmcKmbhIL0kvoj14197d68G5F9YDUHso=";
        private static void Main(string[] args)
        {
            Console.WriteLine("IoT Hub C# Simulated Cave Device. Ctrl-C to exit.\n");

            // Connect to the IoT hub using the MQTT protocol
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            ReceiveC2dAsync();
            
            SendCloudToDeviceMessageAsync();
            Console.ReadLine();
        }
        private static async Task ReceiveC2dAsync()
        {
            Console.WriteLine("\nReceiving cloud to device messages from service");
            while (true)
            {
                Microsoft.Azure.Devices.Client.Message receivedMessage = await deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Received message: {0}",
                Encoding.ASCII.GetString(receivedMessage.GetBytes()));
                Console.ResetColor();

                await deviceClient.CompleteAsync(receivedMessage);

            }
        }
        private async static Task SendCloudToDeviceMessageAsync()
        {
            Console.WriteLine("Press Enter to Send Message");
            Console.ReadLine();
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(connectionString2);
            string targetDevice = "sensor-th-0001";
            var commandMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes("Cloud to device message."));
            await serviceClient.SendAsync(targetDevice, commandMessage);
        }
    }
}