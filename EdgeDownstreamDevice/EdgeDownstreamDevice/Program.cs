// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Samples.EdgeDownstreamDevice
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;

    class Program
    {
        //HostName=iot-hub-az220-pj130622.azure-devices.net;DeviceId=leaft-device;SharedAccessKey=SxOuz0I62U62QDrIURW3jmPf2Gyi6nUDaupJ0rEqXB8=
        const int TemperatureThreshold = 30;

        // 1) Obtain the connection string for your downstream device and to it
        //HostName=iot-hub-az220-pj130622.azure-devices.net;DeviceId=leaft-device;SharedAccessKey=SxOuz0I62U62QDrIURW3jmPf2Gyi6nUDaupJ0rEqXB8=;GatewayHostName=<edge device hostname>;
        // 2) The edge device hostname is the hostname set in the config.yaml of the Edge device
        //    to which this sample will connect to.
        //
        // The resulting string should have this format:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>;GatewayHostName=<edge device hostname>"
        //
        // Either set the DEVICE_CONNECTION_STRING environment variable with this connection string
        // or set it in the Properties/launchSettings.json.
        static readonly string DeviceConnectionString = "HostName=iot-hub-pj270622.azure-devices.net;DeviceId=leaf-temp-device;SharedAccessKey=lEf8UROfAtKi6mbYh34XFL5RKHnKSNOpsaCsVMMgCJY=;GatewayHostName=edgepushkar220.eastus.cloudapp.azure.com";
        static readonly string MessageCountEnv = "500000";

        static int messageCount = 10;
  
        static void Main()
        {
            InstallCACert();

            var amqpSettings = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            amqpSettings.RemoteCertificateValidationCallback = RemoteCertificateValidationCallback;

            System.Net.ServicePointManager.ServerCertificateValidationCallback=RemoteCertificateValidationCallback;

            if (!string.IsNullOrWhiteSpace(MessageCountEnv))
            {
                if (!int.TryParse(MessageCountEnv, out messageCount))
                {
                    Console.WriteLine("Invalid number of messages in env variable MESSAGE_COUNT. MESSAGE_COUNT set to {0}\n", messageCount);
                }
            }

            Console.WriteLine("Creating device client from connection string\n");

            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, new ITransportSettings[] { amqpSettings });

            if (deviceClient == null)
            {
                Console.WriteLine("Failed to create DeviceClient!");
            }
            else
            {
                SendEvents(deviceClient, messageCount).Wait();
            }

            Console.WriteLine("Exiting!\n");
            //FROM /messages/* WHERE NOT IS_DEFINED({$ConnectionModuleId}) INTO $upstream
        }

       
        static void InstallCACert()
        {
            string trustedCACertPath = Environment.GetEnvironmentVariable("IOTEDGE_TRUSTED_CA_CERTIFICATE_PEM_PATH");
            if (!string.IsNullOrWhiteSpace(trustedCACertPath))
            {
                Console.WriteLine("User configured CA certificate path: {0}", trustedCACertPath);
                if (!File.Exists(trustedCACertPath))
                {
                    // cannot proceed further without a proper cert file
                    Console.WriteLine("Certificate file not found: {0}", trustedCACertPath);
                    throw new InvalidOperationException("Invalid certificate file.");
                }
                else
                {
                    Console.WriteLine("Attempting to install CA certificate: {0}", trustedCACertPath);
                    X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(new X509Certificate2(X509Certificate.CreateFromCertFile(trustedCACertPath)));
                    Console.WriteLine("Successfully added certificate: {0}", trustedCACertPath);
                    store.Close();
                }
            }
            else
            {
                Console.WriteLine("CA_CERTIFICATE_PATH was not set or null, not installing any CA certificate");
            }
        }

        /// <summary>
        /// Send telemetry data, (random temperature and humidity data samples).
        /// to the IoT Edge runtime. The number of messages to be sent is determined
        /// by environment variable MESSAGE_COUNT.
        /// </summary>
        static async Task SendEvents(DeviceClient deviceClient, int messageCount)
        {
            Random rnd = new Random();
            Console.WriteLine("Edge downstream device attempting to send {0} messages to Edge Hub...\n", messageCount);

            for (int count = 0; count < messageCount; count++)
            {
                float temperature = rnd.Next(20, 35);
                float humidity = rnd.Next(60, 80);
                string dataBuffer = string.Format(new CultureInfo("en-US"), "{{MyFirstDownstreamDevice \"messageId\":{0},\"temperature\":{1},\"humidity\":{2}}}", count, temperature, humidity);
                Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                eventMessage.Properties.Add("temperatureAlert", (temperature > TemperatureThreshold) ? "true" : "false");
                Console.WriteLine("\t{0}> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), count, dataBuffer);

                await deviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);
            }
        }

        static bool RemoteCertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }    
}
