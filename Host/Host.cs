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

                Console.ReadKey();
                Console.WriteLine($"Service instances: {DIPOL_Remote.Classes.RemoteControl.ActiveConnections.Count}");
                DIPOL_Remote.Classes.RemoteControl.ActiveCameras.TryGetValue(0, out (string, ANDOR_CS.Classes.CameraBase) result);
                (result.Item2 as ANDOR_CS.Classes.DebugCamera).CameraModel = "123";
                DIPOL_Remote.Classes.RemoteControl.ActiveCameras.TryGetValue(1, out result);
                (result.Item2 as ANDOR_CS.Classes.DebugCamera).CameraModel = "456";
                Console.ReadKey();
            }
        }
    }
}
