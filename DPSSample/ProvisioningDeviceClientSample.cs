using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AzCourse.DPS.Sample
{
    internal class DPSGroupSample
    {
        // Change the following consts to connect to the DPS
        const string PRIMARY_KEY = @"47y3qLUoAxC3dMH7O6mwwdbbhkgCcwmxN8f0fbW38D90gMRmBzpYPE/CEAlBdXsRFgK7fUl3y8jkjhbFtQsJoQ==";
        const string ID_SCOPE = "0ne006756DC";

        // The following consts should not be changed
        const string DESIRED_DEVICE_ID = "weather-eastus-0121";
        const string GLOBAL_ENDPOINT = "global.azure-devices-provisioning.net";

        public async Task RunSampleAsync()
        {
            Console.WriteLine($"Initializing the device provisioning client...");

            var derivedKey = ComputeDerivedSymmetricKey(Convert.FromBase64String(PRIMARY_KEY), DESIRED_DEVICE_ID);

            using var security = new SecurityProviderSymmetricKey(
                DESIRED_DEVICE_ID,
                derivedKey,
                null);

            using var transportHandler = GetTransportHandler();

            ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                GLOBAL_ENDPOINT,
                ID_SCOPE,
                security,
                transportHandler);

            Console.WriteLine($"Initialized for device Id {security.GetRegistrationID()}.");

            Console.WriteLine("Registering with the device provisioning service...");
            try
            {
                DeviceRegistrationResult result = await provClient.RegisterAsync();


                if (result.Status != ProvisioningRegistrationStatusType.Assigned)
                {
                    Console.WriteLine($"Registration failed. Error: " + result.ErrorMessage);
                    return;
                }

                Console.WriteLine($"Device {result.DeviceId} registered to {result.AssignedHub}.");

                Console.WriteLine("Creating symmetric key authentication for IoT Hub...");
                IAuthenticationMethod auth = new DeviceAuthenticationWithRegistrySymmetricKey(
                    result.DeviceId,
                    security.GetPrimaryKey());

                Console.WriteLine($"Testing the provisioned device with IoT Hub...");
                using DeviceClient iotClient = DeviceClient.Create(result.AssignedHub, auth, TransportType.Mqtt);

                Console.WriteLine("Sending a telemetry message...");
                int i=1;
                while (true)
                {
                    using var message = new Message(Encoding.UTF8.GetBytes($"TestMessage "+i));
                    await iotClient.SendEventAsync(message);
                    i++;
                    await Task.Delay(1000);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Finished.");
        }

        private ProvisioningTransportHandler GetTransportHandler()
        {
            return new ProvisioningTransportHandlerHttp();
        }

        private string ComputeDerivedSymmetricKey(byte[] masterKey, string registrationId)
        {
            using (var hmac = new HMACSHA256(masterKey))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationId)));
            }
        }
    }
}
