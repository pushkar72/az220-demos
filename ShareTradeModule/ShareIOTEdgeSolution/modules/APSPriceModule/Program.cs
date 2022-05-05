namespace APSPriceModule
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using System.Collections.Generic;     
    using Microsoft.Azure.Devices.Shared; 
    using Newtonsoft.Json;                

    class Trade
    {
        public float Price{get;set;}
        public string Name{get;set;}

        public string Time{get;set;}=DateTime.Now.ToString();
    }
    class Program
    {
        static int counter;
        static int TradePrice {get;set;}
        static readonly Random rnd=new Random();

        static void Main(string[] args)
        {
            
            MainAsync().GetAwaiter().GetResult();
        }


        static async Task MainAsync()
        {
                Console.WriteLine("Trade Started");
                var transportSettings=new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
                ITransportSettings[] settings={transportSettings};
                var mclient=await ModuleClient.CreateFromEnvironmentAsync(
                    settings
                );
                await mclient.OpenAsync();
                await SendMessage(mclient,"Trade Started");

                await SendTradeEvent(mclient);
                Console.WriteLine("Trade Completed");
        }

        static async Task SendMessage(ModuleClient mclient,string message)
        {
                var sevent=new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
                await mclient.SendEventAsync("tradeEvent",sevent);
        }
        static async Task SendTradeEvent(ModuleClient client)
        {
            while(true)
            {
                var tdata=new Trade();
                tdata.Name="APEX Inc";
                tdata.Price=rnd.Next(21,80);
                
                var trademessage=new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tdata)));
                await client.SendEventAsync("tradeEvent",trademessage);
                await Task.Delay(500);
            }
        }
      
    }
}
