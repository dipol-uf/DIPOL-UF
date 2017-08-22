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
                    =>
                {
                    if (!(sender is ANDOR_CS.Classes.DebugCamera))
                    {
                        string senderString = "";
                        if (sender is ANDOR_CS.Classes.CameraBase cam)
                            senderString = $"{cam.CameraModel}/{cam.SerialNumber}";
                        else
                            senderString = sender.ToString();

                        Console.WriteLine($"[{{0,23:yyyy/MM/dd HH-mm-ss.fff}}] @ {senderString}: {message}", DateTime.Now);
                    }

                };
  
                //Console.WriteLine($"Service instances: {DIPOL_Remote.Classes.RemoteControl.ActiveConnections.Count}");
  
                Console.ReadKey();
            }
        }
    }
}
