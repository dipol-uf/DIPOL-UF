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
            var host = new DIPOL_Remote.Classes.DipolHost();
            host.Host();

            Console.ReadKey();
            Console.WriteLine($"Service instances: {DIPOL_Remote.Classes.RemoteControl.ActiveConnections.Count}");
          
            Console.ReadKey();
            host.Dispose();
        }
    }
}
