using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace DeviceTwinBackend
{
    class Program
    {
        static RegistryManager registryManager;



        
        // CHANGE THE CONNECTION STRING TO THE ACTUAL CONNETION STRING OF THE IOT HUB (SERVICE POLICY) 
        static string connectionString="HostName=iot-hub-az220-pj130622.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=nv5MneSzb9bUbZRdr7RajoT2fyba7vmdVtndBe8FtS4=";        
        
        public static async Task SetDeviceTags()  {
            Microsoft.Azure.Devices.Shared.Twin twin=await registryManager.GetTwinAsync("car-device", "speed-module");
            
            //System.Console.WriteLine(twin);
            var patch=
                @"{
                        properties: {
                            desired:  {
                                speedAlertFrom: 85,
                                maxSpeed:100
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
