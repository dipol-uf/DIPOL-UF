using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Host
{
    class Host
    {
        static void Main(string[] args)
        {
            var host = new DIPOL_Remote.DipolHost();
            host.Host();

            Console.ReadKey();
            Console.WriteLine($"Service instances: {DIPOL_Remote.RemoteControl.ActiveConnections.Count}");
            for(int i = 0; i < 5; i++)
                DIPOL_Remote.RemoteControl.ActiveConnections.First().Value.SendToClient();

            Console.ReadKey();
            host.Dispose();
        }
    }
}
