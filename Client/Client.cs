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
                cam1.PropertyChanged += (sender, e) => Console.WriteLine($"{(sender as DIPOL_Remote.Classes.RemoteCamera).CameraIndex}\t{e.PropertyName}");
                var cam2 = client.CreateRemoteCamera(1);

                Console.WriteLine(cam1.CameraModel);
                Console.WriteLine(cam2.CameraModel);

                Console.ReadKey();
                Console.WriteLine(client.ActiveRemoteCameras().Length);
                Console.WriteLine(cam2.CameraModel);
                Console.WriteLine(cam2.CameraModel);
                cam2.Dispose();
                Console.WriteLine(client.ActiveRemoteCameras().Length);
                Console.WriteLine(cam1.CameraModel);
                Console.WriteLine(cam1.CameraModel);
                cam1.Dispose();
                Console.WriteLine(client.ActiveRemoteCameras().Length);


                Console.ReadKey();
                client.Disconnect();

            }
        }
    }
}
