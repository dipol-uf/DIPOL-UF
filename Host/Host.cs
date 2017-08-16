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


            using (var host = new DIPOL_Remote.Classes.DipolHost())
            {
                host.Host();
                host.EventReceived += (sender, message)
                    => Console.WriteLine($"{((ANDOR_CS.Classes.CameraBase)sender).SerialNumber}: {message}");
  
                //Console.WriteLine($"Service instances: {DIPOL_Remote.Classes.RemoteControl.ActiveConnections.Count}");
  
                Console.ReadKey();
            }
        }
    }
}
