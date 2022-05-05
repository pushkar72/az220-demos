

using System;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static CheeseCaveDevice.CheeseCaveSimulator;

namespace CheeseCaveDevice
{
    class Program
    {
        // Interval at which telemetry is sent to the cloud.
        const int intervalInMilliseconds = 5000;

        // Global variables.
        private static DeviceClient deviceClient;

        private static CheeseCaveSimulator cheeseCave;

        // The device connection string to authenticate the device with your IoT hub.
        private readonly static string deviceConnectionString = "HostName=iot-az-220-pj020522.azure-devices.net;DeviceId=cheesecave;SharedAccessKey=6aRI0xjmplOye2gsOyjwBSNC0s74r4vvhGqMxEIUDto=";

        private static async Task Main(string[] args)
        {
            ConsoleHelper.WriteColorMessage("Cheese Cave device app.\n", ConsoleColor.Yellow);

            // Connect to the IoT hub using the MQTT protocol.
            deviceClient = DeviceClient.CreateFromConnectionString(
                deviceConnectionString,
                TransportType.Mqtt);

            // Create an instance of the Cheese Cave Simulator
            cheeseCave = new CheeseCaveSimulator();


            // UNCOMMENT register direct method code below here
            await deviceClient.SetMethodHandlerAsync("SetFanState", SetFanState, null);

            // UNCOMMENT register desired property changed handler code below here
            // Get the device twin to report the initial desired properties.
            Twin deviceTwin = await deviceClient.GetTwinAsync();
            ConsoleHelper.WriteGreenMessage("Initial twin desired properties: " + deviceTwin.Properties.Desired.ToJson());

            // Set the device twin update callback.
            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, null);

            await SendDeviceToCloudMessagesAsync();
            Console.ReadLine();
        }
//
        // Async method to send simulated telemetry.
        private static async Task SendDeviceToCloudMessagesAsync()
        {
            while (true)
            {
                var currentTemperature = cheeseCave.ReadTemperature();
                var currentHumidity = cheeseCave.ReadHumidity();

                // Create JSON message.
                var telemetryDataPoint = new
                {
                    temperature = Math.Round(currentTemperature, 2),
                    humidity = Math.Round(currentHumidity, 2)
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                // Add custom application properties to the message.
                message.Properties.Add("sensorID", "S1");
                message.Properties.Add("fanAlert", (cheeseCave.FanState == StateEnum.Failed) ? "true" : "false");

                // Send temperature or humidity alerts, only if they occur.
                if (cheeseCave.IsTemperatureAlert)
                {
                    message.Properties.Add("temperatureAlert", "true");
                }
                if (cheeseCave.IsHumidityAlert)
                {
                    message.Properties.Add("humidityAlert", "true");
                }

                Console.WriteLine("Message data: {0}", messageString);

                // Send the telemetry message.
                await deviceClient.SendEventAsync(message);
                ConsoleHelper.WriteGreenMessage("Message sent\n");

                await Task.Delay(intervalInMilliseconds);
            }
        }

      
        private static Task<MethodResponse> SetFanState(MethodRequest methodRequest, object userContext)
        {
     
            if (cheeseCave.FanState == StateEnum.Failed)
            {
              
                string result = "{\"result\":\"Fan failed\"}";
               
                ConsoleHelper.WriteRedMessage("Direct method failed: " + result);
         
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
            else
            {
               
                try
                {
              
                    var data = Encoding.UTF8.GetString(methodRequest.Data);

                 
                    data = data.Replace("\"", "");

                
                    cheeseCave.UpdateFan((StateEnum)Enum.Parse(typeof(StateEnum), data));
                    ConsoleHelper.WriteGreenMessage("Fan set to: " + data);

           
                    string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
                }
                catch
                {
                    
                    string result = "{\"result\":\"Invalid parameter\"}";
                    ConsoleHelper.WriteRedMessage("Direct method failed: " + result);
                    return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
                }
            }
        }

       
        private static async Task OnDesiredPropertyChanged(
            TwinCollection desiredProperties,
            object userContext)
        {
            try
            {
                // Update the Cheese Cave Simulator properties
                cheeseCave.DesiredHumidity = desiredProperties["humidity"];
                cheeseCave.DesiredTemperature = desiredProperties["temperature"];
                ConsoleHelper.WriteGreenMessage("Setting desired humidity to " + desiredProperties["humidity"]);
                ConsoleHelper.WriteGreenMessage("Setting desired temperature to " + desiredProperties["temperature"]);

                // Report the properties back to the IoT Hub.
                var reportedProperties = new TwinCollection();
                reportedProperties["fanstate"] = cheeseCave.FanState.ToString();
                reportedProperties["humidity"] = cheeseCave.DesiredHumidity;
                reportedProperties["temperature"] = cheeseCave.DesiredTemperature;
                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);

                ConsoleHelper.WriteGreenMessage("\nTwin state reported: " + reportedProperties.ToJson());
            }
            catch
            {
                ConsoleHelper.WriteRedMessage("Failed to update device twin");
            }
        }

    }


    internal class CheeseCaveSimulator
    {
        internal enum StateEnum
        {
            Off,
            On,
            Failed
        }

       
        private const double ambientTemperature = 70;
        // Ambient humidity in relative percentage of air saturation.
        private const double ambientHumidity = 99;
        // Acceptable range above or below the desired temp, in degrees F.
        private const double desiredTempLimit = 5;
        // Acceptable range above or below the desired humidity, in percentages.
        private const double desiredHumidityLimit = 10;

        // state variables
        // initial value is set to the ambient value
        private double currentTemperature = ambientTemperature;
        // initial value is set to the ambient value
        private double currentHumidity = ambientHumidity;

        Random rand = new Random();

        internal StateEnum FanState { get; private set; } = StateEnum.Off;

        internal bool IsTemperatureAlert =>
            (currentTemperature > DesiredTemperature + desiredTempLimit) ||
            (currentTemperature < DesiredTemperature - desiredTempLimit);

        internal bool IsHumidityAlert =>
            (currentHumidity > DesiredHumidity + desiredHumidityLimit) ||
            (currentHumidity < DesiredHumidity - desiredHumidityLimit);

        // Initial desired temperature, in degrees F.
        public double DesiredTemperature { get; set; } = ambientTemperature - 10;
        // Initial desired humidity in relative percentage of air saturation.
        public double DesiredHumidity { get; set; } = ambientHumidity - 20;

        public double ReadTemperature()
        {
            // Simulate telemetry.
            double deltaTemperature = Math.Sign(DesiredTemperature - currentTemperature);

            if (FanState == StateEnum.On)
            {
                // If the fan is on the temperature and humidity will be nudged
                // towards the desired values most of the time.
                currentTemperature += (deltaTemperature * rand.NextDouble())
                                      + rand.NextDouble() - 0.5;

                // Randomly fail the fan.
                if (rand.NextDouble() < 0.01)
                {
                    FanState = StateEnum.Failed;
                    ConsoleHelper.WriteRedMessage("Fan has failed");
                }
            }
            else
            {
                // If the fan is off, or has failed, the temperature and humidity
                // will creep up until they reaches ambient values, thereafter
                // fluctuate randomly.
                if (currentTemperature < ambientTemperature - 1)
                {
                    currentTemperature += rand.NextDouble() / 10;
                }
                else
                {
                    currentTemperature += rand.NextDouble() - 0.5;
                }
            }

            return currentTemperature;
        }

        public double ReadHumidity()
        {
            // Simulate telemetry.
            double deltaHumidity = Math.Sign(DesiredHumidity - currentHumidity);

            if (FanState == StateEnum.On)
            {
                // If the fan is on the temperature and humidity will be nudged
                // towards the desired values most of the time.
                currentHumidity += (deltaHumidity * rand.NextDouble())
                                   + rand.NextDouble() - 0.5;

                // Randomly fail the fan.
                if (rand.NextDouble() < 0.01)
                {
                    FanState = StateEnum.Failed;
                    ConsoleHelper.WriteRedMessage("Fan has failed");
                }
            }
            else
            {
                // If the fan is off, or has failed, the temperature and humidity
                // will creep up until they reaches ambient values, thereafter
                // fluctuate randomly.
                if (currentHumidity < ambientHumidity - 1)
                {
                    currentHumidity += rand.NextDouble() / 10;
                }
                else
                {
                    currentHumidity += rand.NextDouble() - 0.5;
                }
            }

            // Check: humidity can never exceed 100%.
            currentHumidity = Math.Min(100, currentHumidity);

            return currentHumidity;
        }

        internal void UpdateFan(StateEnum newState)
        {
            // in a real device, this method would contain logic to start/stop
            // a fan and determine whether it was successful
            FanState = newState;
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