#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        private static async Task JsonSerializationTest()
        {
            var cams = await Task.WhenAll(Enumerable.Range(0, 3)
                .Select(x => DebugCamera.CreateAsync(x)));
            try
            {
                var setts = cams.InvokeAll<DebugCamera, SettingsBase>(
                    nameof(DebugCamera.GetAcquisitionSettingsTemplate));
                try
                {
                    setts.SetAll(nameof(SettingsBase.SetVSSpeed), 0);
                    setts.SetAll(nameof(SettingsBase.SetVSAmplitude), VSAmplitude.Normal);
                    setts.SetAll(nameof(SettingsBase.SetADConverter), 0);
                    setts.SetAll(nameof(SettingsBase.SetOutputAmplifier), OutputAmplification.Conventional);
                    setts.SetAll(nameof(SettingsBase.SetHSSpeed), 0);
                    setts.SetAll(nameof(SettingsBase.SetPreAmpGain), 0);
                    setts.SetAll(nameof(SettingsBase.SetAcquisitionMode), AcquisitionMode.SingleScan);
                    setts.SetAll(nameof(SettingsBase.SetAcquisitionMode), AcquisitionMode.SingleScan);
                    setts.SetAll(nameof(SettingsBase.SetTriggerMode), TriggerMode.Internal);
                    setts.SetAll(nameof(SettingsBase.SetReadoutMode), ReadMode.FullImage);
                    setts.SetAll(nameof(SettingsBase.SetImageArea), new Rectangle(1, 1, 64, 64));
                    setts.SetAll(nameof(SettingsBase.SetExposureTime), 3f);

                    var target = new Target1()
                    {
                        StarName = @"TestStar",
                        SharedParameters = setts[0],
                        CycleType = CycleType.Photometric,
                        PerCameraParameters = new Dictionary<string, Dictionary<string, object?>?>
                        {
                            {nameof(SettingsBase.ExposureTime), 
                                cams.ToDictionary(
                                    x => x.CameraIndex switch
                                    {
                                        0 => "B",
                                        1 => "V",
                                        2 => "R",
                                        _ => "?"
                                    }, 
                                    x => (object?)(x.CameraIndex * 0.5f)) }
                        }
                    };

                    var str = JsonConvert.SerializeObject(target, Formatting.Indented);
                    //using var fstr = new FileStream("test.json", FileMode.Create, FileAccess.ReadWrite);
                    //using var writer = new StreamWriter(fstr);
                    //await writer.WriteAsync(str);

                }
                finally
                {
                    if (setts is { })
                        foreach (var sett in setts)
                            sett?.Dispose();
                }

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
