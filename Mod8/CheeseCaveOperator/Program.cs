// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// New Features:
//
// * New style of application - a back-end app that can connect to IoT Hub and
//   "listen" for telemetry via the EventHub endpoint.
//
// The app will be used to automate the control of the temperature in the cheese
// cave.

using System;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;

using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace CheeseCaveOperator
{
    class Program
    {
        
        private readonly static string eventHubsCompatibleEndpoint = "Endpoint=sb://iothub-ns-iot-az-220-18870097-ffdb8c031a.servicebus.windows.net/;SharedAccessKeyName=iothubowner;SharedAccessKey=7jVuhRWkaKuxnN/JWFepN2Ck3utAgc7eW+HWoQopHRs=;EntityPath=iot-az-220-pj020522";
        private readonly static string eventHubsCompatiblePath = "iot-az-220-pj020522";
               private readonly static string iotHubSasKey = "ZuzI0VC6bxFjmcKmbhIL0kvoj14197d68G5F9YDUHso=";
           private readonly static string iotHubSasKeyName = "service";
              private static EventHubConsumerClient consumer;
        private static ServiceClient serviceClient;
        private static RegistryManager registryManager;


        private readonly static string serviceConnectionString = "HostName=iot-az-220-pj020522.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=7jVuhRWkaKuxnN/JWFepN2Ck3utAgc7eW+HWoQopHRs=";

        private readonly static string deviceId = "cheesecave";

        public static async Task Main(string[] args)
        {
            ConsoleHelper.WriteColorMessage("Cheese Cave Operator\n", ConsoleColor.Yellow);

            // Combine the values into a Connection String
            var connectionString = $"{eventHubsCompatibleEndpoint};" +
                $"SharedAccessKeyName={iotHubSasKeyName};" +
                $"SharedAccessKey={iotHubSasKey};" +
                $"EntityPath={eventHubsCompatiblePath}";

            // Assigns the value "$Default"
            var consumerGroup = "streamanalyticspj";
            Console.WriteLine(connectionString);

            consumer = new EventHubConsumerClient(
                consumerGroup,
                connectionString);

            var d2cPartitions = await consumer.GetPartitionIdsAsync();

 
             registryManager = RegistryManager
                 .CreateFromConnectionString(serviceConnectionString);
             await SetTwinProperties();


             serviceClient = ServiceClient
                .CreateFromConnectionString(serviceConnectionString);
            
             await InvokeMethod();

            
            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(partition));
            }

            Task.WaitAll(tasks.ToArray());
        }

       
        private static async Task ReceiveMessagesFromDeviceAsync(string partition)
        {
            EventPosition startingPosition = EventPosition.Earliest;

           
            await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync(
                partition,
                startingPosition))
            {
                string readFromPartition = partitionEvent.Partition.PartitionId;

               
                ReadOnlyMemory<byte> eventBodyBytes = partitionEvent.Data.EventBody.ToMemory();
                string data = Encoding.UTF8.GetString(eventBodyBytes.ToArray());
                ConsoleHelper.WriteGreenMessage("Telemetry received: " + data);

                foreach (var prop in partitionEvent.Data.Properties)
                {
                    if (prop.Value.ToString() == "true")
                    {
                        ConsoleHelper.WriteRedMessage(prop.Key);
                    }
                }
                Console.WriteLine();
            }
        }


        private static async Task InvokeMethod()
        {
            try
            {
                var methodInvocation = new CloudToDeviceMethod("SetFanState") { ResponseTimeout = TimeSpan.FromSeconds(30) };
                string payload = JsonConvert.SerializeObject("On");

                methodInvocation.SetPayloadJson(payload);

                // Invoke the direct method asynchronously and get the response from the simulated device.
                var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);

                if (response.Status == 200)
                {
                    ConsoleHelper.WriteGreenMessage("Direct method invoked: " + response.GetPayloadAsJson());
                }
                else
                {
                    ConsoleHelper.WriteRedMessage("Direct method failed: " + response.GetPayloadAsJson());
                }
            }
            catch
            {
                ConsoleHelper.WriteRedMessage("Direct method failed: timed-out");
            }
        }


        private static async Task SetTwinProperties()
        {
            var twin = await registryManager.GetTwinAsync(deviceId);
            var patch =
                @"{
                tags: {
                    customerID: 'Customer1',
                    cheeseCave: 'CheeseCave1'
                },
                properties: {
                    desired: {
                        patchId: 'set values',
                        temperature: '50',
                        humidity: '85'
                    }
                }
            }";
            await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);

            var query = registryManager.CreateQuery(
                "SELECT * FROM devices WHERE tags.cheeseCave = 'CheeseCave1'", 100);
            var twinsInCheeseCave1 = await query.GetNextAsTwinAsync();
            Console.WriteLine("Devices in CheeseCave1: {0}",
                string.Join(", ", twinsInCheeseCave1.Select(t => t.DeviceId)));
        }
    }

    internal static class ConsoleHelper
    {
        internal static void WriteColorMessage(string text, ConsoleColor clr)
        {
            Console.ForegroundColor = clr;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        internal static void WriteGreenMessage(string text)
        {
            WriteColorMessage(text, ConsoleColor.Green);
        }

        internal static void WriteRedMessage(string text)
        {
            WriteColorMessage(text, ConsoleColor.Red);
        }
    }
}