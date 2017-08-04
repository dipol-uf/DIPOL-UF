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
          
            using (var client = new DIPOL_Remote.Classes.DipolClient())
            {
                client.Connect();

                Console.WriteLine("Session ID {0}", client.SessionID);

                Console.WriteLine($"Number of available cameras: {client.GetNumberOfCameras()}");

                var cam1 = client.CreateRemoteCamera(0);
                cam1.PropertyChanged += (sender, e) => Console.WriteLine($"Event {(sender as DIPOL_Remote.Classes.RemoteCamera).CameraIndex}\t{e.PropertyName}");
                cam1.TemperatureStatusChecked += (sender, e) => Console.WriteLine($"Event{ (sender as DIPOL_Remote.Classes.RemoteCamera).CameraIndex}\t{e.EventTime}");
                var cam2 = client.CreateRemoteCamera(1);
                cam1.GetCurrentTemperature();
                cam2.FanControl(ANDOR_CS.Enums.FanMode.FullSpeed);
                cam1.CoolerControl(ANDOR_CS.Enums.Switch.Enabled);
                cam2.SetTemperature(-12);
                cam1.TemperatureMonitor(ANDOR_CS.Enums.Switch.Enabled, 100);

                Console.WriteLine(cam1.CameraModel);
                Console.WriteLine(cam2.CameraModel);

                Console.ReadKey();
                cam1.TemperatureMonitor(ANDOR_CS.Enums.Switch.Disabled);
                Console.WriteLine(client.ActiveRemoteCameras().Length);
                Console.WriteLine(cam2.CameraModel);
                cam2.Dispose();
                Console.WriteLine(client.ActiveRemoteCameras().Length);
                Console.WriteLine(cam1.CameraModel);
                cam1.Dispose();
                Console.WriteLine(client.ActiveRemoteCameras().Length);


                Console.ReadKey();
                client.Disconnect();

            }
        }

    }
}
