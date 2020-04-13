#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ANDOR_CS;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using DIPOL_UF.Enums;
using DIPOL_UF.Jobs;
using Newtonsoft.Json;
using Enumerable = System.Linq.Enumerable;

namespace Sandbox
{
    class Program
    {
        static async Task Main(string[] args)
        {
             await JsonSerializationTest();
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private static async Task JsonSerializationTest()
        {
            var cams = (await Task.WhenAll(Enumerable.Range(0, 3)
                    .Select(x => DebugCamera.CreateAsync(x))))
                .ToDictionary(
                    x => x.CameraIndex switch
                    {
                        0 => "B",
                        1 => "V",
                        2 => "R",
                        _ => "?"
                    }, x => x as IDevice);
            try
            {

                var sharedSetts = new SharedSettingsContainer
                {
                    VSSpeed = 3,
                    VSAmplitude = VSAmplitude.Normal,
                    ADConverter = 0,
                    OutputAmplifier = OutputAmplification.Conventional,
                    HSSpeed = 10,
                    PreAmpGain = "Gain1",
                    AcquisitionMode = AcquisitionMode.SingleScan,
                    TriggerMode = TriggerMode.Internal,
                    ImageArea = new Rectangle(1, 1, 64, 64),
                    ExposureTime = 3f
                };


                var settings = cams.ToDictionary(x => x.Key,
                    x =>
                    {
                        var (_, value) = x;
                        var s = sharedSetts.PrepareTemplateForCamera(value);
                        s.SetExposureTime(value.CameraIndex * 0.5f + 2f);
                        return s;
                    });

                var target = Target1.FromSettings(settings, "TestStar");



                //var newSetts = target.CreateTemplatesForCameras(cams);

                var str = JsonConvert.SerializeObject(target, Formatting.Indented);


                var tg = JsonConvert.DeserializeObject<Target1>(str);

                //using var fstr = new FileStream("test.json", FileMode.Create, FileAccess.ReadWrite);
                //using var writer = new StreamWriter(fstr);
                //await writer.WriteAsync(str);

                //if (newSetts is { })
                //    foreach (var (_, st) in newSetts)
                //        st?.Dispose();

                foreach (var (_, sett) in settings)
                    sett?.Dispose();
            }
            finally
            {
                if (cams is { })
                    foreach(var (_, cam) in cams)
                        cam?.Dispose();
            }


        }
    }
}
