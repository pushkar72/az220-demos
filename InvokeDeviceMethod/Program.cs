using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace InvokeDeviceMethod
{
    internal class Program
    {
        private static ServiceClient serviceClient;

        private static string connectionString = "PASTE IOT HUB SERVICE CONNECTION STRING";
        private static string deviceId = "DEVICE ID";

        private static async Task Main(string[] args)
        {
            Console.WriteLine("Invoking direct method...");

            serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

            await InvokeDirectMethodAsync();

            serviceClient.Dispose();

            Console.WriteLine("\nPress Enter to exit.");
            Console.ReadLine();
        }

        // Invoke the direct method on the device, passing the payload
        private static async Task InvokeDirectMethodAsync()
        {
            var methodName = "SetFlashlightState";

            var methodInvocation = new CloudToDeviceMethod(methodName)
            {
                ResponseTimeout = TimeSpan.FromSeconds(30),
            };
            methodInvocation.SetPayloadJson("\"On\"");

            Console.WriteLine("Invoking direct method '{0}'", methodName);

            var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);

            Console.WriteLine($"\nResponse status: {response.Status}, payload:\n\t{response.GetPayloadAsJson()}");
        }
    }
}
