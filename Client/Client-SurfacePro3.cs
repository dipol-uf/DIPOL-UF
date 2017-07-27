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

            Task.Factory.StartNew(() => 
            {
                for (int i = 0; i < 10; i++)
                {
                Console.WriteLine(client.Remote.SessionID);
                    Task.Delay(1000).Wait();
                }
            });

            Console.ReadKey();

            client.Remote.Disconnect();

            client.Dispose();
        }
    }
}
