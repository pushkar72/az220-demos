using System;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Azure.Devices.Client;

namespace AzCourse.IoT.Method
{
    public class MethodSample
    {
        private readonly DeviceClient _deviceClient;

        public MethodSample(DeviceClient deviceClient)
        {
            _deviceClient = deviceClient;
        }

        private class UserContext
        {
            public string TimeZone { get; set; }
        }

        public async Task RunMethod()
        {
            var ctx = new UserContext() { TimeZone = "Eastern Standard Time" };
            await _deviceClient.SetMethodHandlerAsync("GetDeviceTime", GetDeviceTimeAsync, ctx);

            Console.WriteLine($"Method 'GetDeviceTime' is ready for invocation");

            await _deviceClient.SetMethodHandlerAsync("SetFlashlightState", SetFlashlightStateAsync, null);

            Console.WriteLine($"Method 'SetFlashlightState' is ready for invocation");

            while (true)
            {
                await Task.Delay(1000);
            }
        }

        private Task<MethodResponse> GetDeviceTimeAsync(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t *** {methodRequest.Name} was called.");

            var ctx = (UserContext)userContext;
            var localDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, ctx.TimeZone);

            string result = JsonSerializer.Serialize(localDate);
            var retValue = new MethodResponse(Encoding.UTF8.GetBytes(result), 200);

            Console.WriteLine("Return value: {0}", result);

            return Task.FromResult(retValue);
        }

        private Task<MethodResponse> SetFlashlightStateAsync(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t *** {methodRequest.Name} was called.");

            string response;

            switch (methodRequest.DataAsJson)
            {
                case "\"On\"":
                    response = "Flashlight is On";
                    break;
                case "\"Off\"":
                    response = "Flashlight is Off";
                    break;
                default:
                    response = "Flashlight is Off";
                    break;
            }

            string result = JsonSerializer.Serialize(response);
            var retValue = new MethodResponse(Encoding.UTF8.GetBytes(result), 200);

            Console.WriteLine("Return value: {0}", result);

            return Task.FromResult(retValue);
        }
    }
}
