using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Serializers;

namespace DIPOL_UF
{
    // WATCH : partial class that hides real [Main]
    public static partial class DipolUfApp
    {
        public static async Task Main()
        {
            ReadOnlyDictionary<string, object> json;
            using (var str = new StreamReader(@"polarimetry_job.json"))
                json = JsonParser.ReadJson(str);

            var job = new Jobs.Job(json);

            await job.Run();

            Console.ReadKey();
        }
    }
}
