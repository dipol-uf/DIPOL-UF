using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using DIPOL_UF.Jobs;
using Serializers;

namespace DIPOL_UF
{
    // WATCH : partial class that hides real [Main]
    public static partial class DipolUfApp
    {
        public static async Task Main()
        {
            Job job;
            using (var str = new FileStream(@"polarimetry_job.json", FileMode.Open, FileAccess.Read))
                job = Job.Create(str);


            await job.Run();

            Console.ReadKey();
        }
    }
}
