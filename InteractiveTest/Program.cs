using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ANDOR_CS.Classes;
using ANDOR_CS.Enums;

namespace InteractiveTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var factory = new DebugCamera.DebugCameraFactory();
            using var device = await factory.CreateAsync();

            var setts = device.GetAcquisitionSettingsTemplate();

            setts.SetExposureTime(10f);
            setts.SetReadoutMode(ReadMode.FullImage);
            setts.SetTriggerMode(TriggerMode.Internal);
            setts.SetAcquisitionMode(AcquisitionMode.SingleScan);

            using var str = new FileStream("test.json", FileMode.Create, FileAccess.Write);
            await setts.SerializeAsync(str, Encoding.ASCII, default);
        }
    }
}
