﻿using System;
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

        private readonly static string connectionString = "HostName=iot-az-220-pj020522.azure-devices.net;DeviceId=sensor-th-0001;SharedAccessKey=ImZvu/gxN1N7PlnmmXxnvNoMDR2U3+3Fcoj0i+8moYE=";
        private static void Main(string[] args)
        {
            Console.WriteLine("IoT Hub C# Simulated Cave Device. Ctrl-C to exit.\n");

            // Connect to the IoT hub using the MQTT protocol
            deviceClient = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
            SendDeviceToCloudMessagesAsync();
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

             
                message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");
                message.ContentType="application/json";
                message.ContentEncoding="utf-8";
               

                // Send the telemetry message
                await deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(500);
            }
        }
        private static string CreateMessageString(double temperature, double humidity)
        {
         
            var telemetryDataPoint = new
            {
                temperature = temperature,
                humidity = humidity
            };

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