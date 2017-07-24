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

            Console.WriteLine("Session ID {0}", client);

            Console.WriteLine($"Number of available cameras: {client.GetNumberOfCameras()}");

            client.CreateRemoteCamera();

            Console.ReadKey();

            client.Disconnect();

            client.Dispose();
        }
    }
}
