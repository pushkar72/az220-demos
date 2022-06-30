// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.PlugAndPlay;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class Program
    {
        // Set the DPS_ID_SCOPE and SAS_TOKEN VALUES from the Administration -> Device connection secion of IoT Central
        private const string DPS_ID_SCOPE = "0ne0065966A";
        private const string SAS_TOKEN="p+UaSzsoA1jO4/Lvp6QrSHjHmabpG7a7am+jJqDXzvCK2SEFsH3CslkYctot4+OdPuTcERhcbBS71v2Hal9tGQ==";

     
        private const string ModelId = "dtmi:com:example:TemperatureMonitor;2";
        private const string DEVICE_ID = "TemperatureMonitor";        
        private const string DPS_ENDPOINT = "global.azure-devices-provisioning.net";
        private static ILogger s_logger;
        //HostName=iot-dps-az220-pj130622.azure-devices-provisioning.net;SharedAccessKey=p+UaSzsoA1jO4/Lvp6QrSHjHmabpG7a7am+jJqDXzvCK2SEFsH3CslkYctot4+OdPuTcERhcbBS71v2Hal9tGQ==

        public static async Task Main(string[] args)
        {


            using var cts = new CancellationTokenSource();
            s_logger = InitializeConsoleDebugLogger();
            s_logger.LogDebug($"Set up the device client.");
            using DeviceClient deviceClient = await SetupDeviceClientAsync( cts.Token);
            var sample = new TemperatureControllerSample(deviceClient, s_logger);
            await sample.PerformOperationsAsync(cts.Token);

            await deviceClient.CloseAsync();
        }

        private static ILogger InitializeConsoleDebugLogger()
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                .AddFilter(level => level >= LogLevel.Debug)
                .AddConsole(options =>
                {
                    options.TimestampFormat = "[MM/dd/yyyy HH:mm:ss]";
                });
            });

            return loggerFactory.CreateLogger<TemperatureControllerSample>();
        }

        private static async Task<DeviceClient> SetupDeviceClientAsync(CancellationToken cancellationToken)
        {
            DeviceClient deviceClient;

            DeviceRegistrationResult dpsRegistrationResult = await ProvisionDeviceAsync(cancellationToken);
            var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(dpsRegistrationResult.DeviceId, ComputeDerivedSymmetricKey(Convert.FromBase64String(SAS_TOKEN), DEVICE_ID));
            deviceClient = InitializeDeviceClient(dpsRegistrationResult.AssignedHub, authMethod);

            return deviceClient;
        }

        // Provision a device via DPS, by sending the PnP model Id as DPS payload.
        private static async Task<DeviceRegistrationResult> ProvisionDeviceAsync(CancellationToken cancellationToken)
        {
            SecurityProvider symmetricKeyProvider = new SecurityProviderSymmetricKey(DEVICE_ID, ComputeDerivedSymmetricKey(Convert.FromBase64String(SAS_TOKEN), DEVICE_ID), null);
            ProvisioningTransportHandler mqttTransportHandler = new ProvisioningTransportHandlerMqtt();
            ProvisioningDeviceClient pdc = ProvisioningDeviceClient.Create(DPS_ENDPOINT, DPS_ID_SCOPE, symmetricKeyProvider, mqttTransportHandler);

            var pnpPayload = new ProvisioningRegistrationAdditionalData
            {
                JsonData = PnpConvention.CreateDpsPayload(ModelId),
            };
            return await pdc.RegisterAsync(pnpPayload, cancellationToken);
        }




//HostName=;SharedAccessKey=












        // Initialize the device client instance using connection string based authentication, over Mqtt protocol (TCP, with fallback over Websocket) and
        // setting the ModelId into ClientOptions.This method also sets a connection status change callback, that will get triggered any time the device's
        // connection status changes.
        private static DeviceClient InitializeDeviceClient(string deviceConnectionString)
        {
            var options = new ClientOptions
            {
                ModelId = ModelId,
            };

            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt, options);
            deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                s_logger.LogDebug($"Connection status change registered - status={status}, reason={reason}.");
            });

            return deviceClient;
        }

        // Initialize the device client instance using symmetric key based authentication, over Mqtt protocol (TCP, with fallback over Websocket)
        // and setting the ModelId into ClientOptions. This method also sets a connection status change callback, that will get triggered any time the device's connection status changes.
        private static DeviceClient InitializeDeviceClient(string hostname, IAuthenticationMethod authenticationMethod)
        {
            var options = new ClientOptions
            {
                ModelId = ModelId,
            };

            DeviceClient deviceClient = DeviceClient.Create(hostname, authenticationMethod, TransportType.Mqtt, options);
            deviceClient.SetConnectionStatusChangesHandler((status, reason) =>
            {
                s_logger.LogDebug($"Connection status change registered - status={status}, reason={reason}.");
            });

            return deviceClient;
        }

        private static string ComputeDerivedSymmetricKey(byte[] masterKey, string registrationId)
        {
            using (var hmac = new HMACSHA256(masterKey))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationId)));
            }
        }
    }
}
