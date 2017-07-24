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
            var client = new DIPOL_Remote.DipolClient();
            client.Remote.Connect();

            Console.WriteLine("Session ID {0}", client.Remote.SessionID);

            Console.WriteLine($"Number of available cameras: {client.Remote.GetNumberOfCameras()}");

            client.Remote.CreateCamera();

            Console.ReadKey();

            client.Remote.Disconnect();

            client.Dispose();
        }
    }
}
