using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Client
    {
        static void Main(string[] args)
        {
            var client = new DIPOL_Remote.Classes.DipolClient();
            client.Connect();

            Console.WriteLine("Session ID {0}", client.SessionID);

            Console.WriteLine($"Number of available cameras: {client.GetNumberOfCameras()}");

            var cam1 = client.CreateRemoteCamera(0);
            var cam2 = client.CreateRemoteCamera(1);

            Console.ReadKey();
            Console.WriteLine(client.ActiveRemoteCameras().Length);
            cam2.Dispose();
            Console.WriteLine(client.ActiveRemoteCameras().Length);
            cam1.Dispose();
            Console.WriteLine(client.ActiveRemoteCameras().Length);


            Console.ReadKey();
            client.Disconnect();

            client.Dispose();
        }
    }
}
