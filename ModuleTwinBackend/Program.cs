using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace DeviceTwinBackend
{
    class Program
    {
        static RegistryManager registryManager;



        //module connection "HostName=iot-az-220-pj020522.azure-devices.net;DeviceId=sensor-th-0072;ModuleId=sensor-0072-device-module;SharedAccessKey=sS8d6S9/jqYFkPY4aIaD9JV80CetWKuDx6Pzr0N9DjY="
        // CHANGE THE CONNECTION STRING TO THE ACTUAL CONNETION STRING OF THE IOT HUB (SERVICE POLICY) 
        static string connectionString="HostName=iot-az-220-pj020522.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=ZuzI0VC6bxFjmcKmbhIL0kvoj14197d68G5F9YDUHso=";        
        
        public static async Task SetDeviceTags()  {
            var twin=await registryManager.GetTwinAsync("sensor-th-0072", "sensor-0072-device-module");
            var patch=
                @"{
                        properties: {
                            desired:  {
                                speedAlertFrom: 120
                            }
                        }
                    }";
            await registryManager.UpdateTwinAsync(twin.DeviceId, twin.ModuleId, patch, twin.ETag);                    
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Starting Device Twin backend...");
            registryManager=RegistryManager.CreateFromConnectionString(connectionString);
            SetDeviceTags().Wait();
            Console.WriteLine("Hit Enter to exit...");
            Console.ReadLine();
        }
    }
}
