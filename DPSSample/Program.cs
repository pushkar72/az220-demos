using System;
using System.Threading.Tasks;

namespace AzCourse.DPS.Sample
{
       internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var sample = new DPSGroupSample();
            await sample.RunSampleAsync();

            return 0;
        }
    }
}
