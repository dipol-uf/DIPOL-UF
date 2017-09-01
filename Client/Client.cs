using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.DataStructures;

namespace Client
{
    class Client
    {
        static void Main(string[] args)
        {
          
            using (var client = new DIPOL_Remote.Classes.DipolClient("dipol-2"))
            {
                client.Connect();

                Console.WriteLine("Session ID {0}", client.SessionID);

                Console.ReadKey();
                TestSettings(client);
                Console.ReadKey();
                client.Disconnect();

            }

            
        }

        private static void TestCamera(DIPOL_Remote.Classes.DipolClient client)
        {

            Console.WriteLine($"Number of available cameras: {client.GetNumberOfCameras()}");

            var cam1 = client.CreateRemoteCamera(0);
            cam1.PropertyChanged += (sender, e) => Console.WriteLine($"Event {(sender as DIPOL_Remote.Classes.RemoteCamera).CameraIndex}\t{e.PropertyName}");
            cam1.TemperatureStatusChecked += (sender, e) => Console.WriteLine($"Event{ (sender as DIPOL_Remote.Classes.RemoteCamera).CameraIndex}\t{e.EventTime}\t{e.Temperature}");
            //var cam2 = client.CreateRemoteCamera(1);
            cam1.GetCurrentTemperature();
            cam1.FanControl(ANDOR_CS.Enums.FanMode.FullSpeed);
            // cam2.FanControl(ANDOR_CS.Enums.FanMode.FullSpeed);
            cam1.CoolerControl(ANDOR_CS.Enums.Switch.Disabled);
            //cam2.SetTemperature(-12);
            cam1.TemperatureMonitor(ANDOR_CS.Enums.Switch.Enabled, 100);

            Console.WriteLine(cam1.CameraModel);
            //Console.WriteLine(cam2.CameraModel);
            Console.ReadKey();
            cam1.SetTemperature(-5);
            cam1.FanControl(ANDOR_CS.Enums.FanMode.FullSpeed);
            cam1.CoolerControl(ANDOR_CS.Enums.Switch.Enabled);

            Console.ReadKey();
            cam1.CoolerControl(ANDOR_CS.Enums.Switch.Disabled);
            Console.ReadKey();
            cam1.TemperatureMonitor(ANDOR_CS.Enums.Switch.Disabled);
            //cam1.FanControl(ANDOR_CS.Enums.FanMode.FullSpeed);
            Console.WriteLine(cam1.GetCurrentTemperature());
            //Console.ReadKey();
            //Console.WriteLine(client.ActiveRemoteCameras().Length);
            //Console.WriteLine(cam2.CameraModel);
            // cam2.Dispose();
            //Console.WriteLine(client.ActiveRemoteCameras().Length);
            //Console.WriteLine(cam1.CameraModel);
            cam1.Dispose();
            //Console.WriteLine(client.ActiveRemoteCameras().Length);
        }

        private static void TestSettings(DIPOL_Remote.Classes.DipolClient client)
        {
            using (var camera = client.CreateRemoteCamera())
            {

                var sets = camera.GetAcquisitionSettingsTemplate();

                sets.SetADConverter(0);
                sets.SetOutputAmplifier(ANDOR_CS.Enums.OutputAmplification.Conventional);

                var speeds = sets.GetAvailableHSSpeeds();

                foreach (var speed in speeds)
                    Console.WriteLine(speed);

                sets.SetHSSpeed(1);

                var preAmps = sets.GetAvailablePreAmpGain();

                Console.WriteLine();

                foreach (var amp in preAmps)
                    Console.WriteLine(amp);

                sets.SetPreAmpGain(0);
            }
        }


    }
}
