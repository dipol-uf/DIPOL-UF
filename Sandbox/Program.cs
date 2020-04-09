#nullable enable
using System.Linq;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using Enumerable = System.Linq.Enumerable;

namespace Sandbox
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await JsonSerializationTest();
        }

        private static async Task JsonSerializationTest()
        {
            var cams = await Task.WhenAll(Enumerable.Range(0, 3)
                .Select(x => DebugCamera.CreateAsync(x)));
            try
            {
            }
            finally
            {
                if (cams is { })
                    foreach(var cam in cams)
                        cam?.Dispose();
            }


        }
    }
}
