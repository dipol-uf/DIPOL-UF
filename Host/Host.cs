using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;


namespace Host
{
    class Host
    {
        private static object locker = new object();

        static void Main(string[] args)
        {
            Console.WindowWidth = 180;
            Console.WindowHeight = 60;

            //Debug();

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

                        lock (locker)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write($"[{{0,23:yyyy/MM/dd HH-mm-ss.fff}}] @", DateTime.Now);
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(" {0, 16}", senderString);
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($": { message}");
                        }
                    }

                };

                //Console.WriteLine($"Service instances: {DIPOL_Remote.Classes.RemoteControl.ActiveConnections.Count}");

                ConsoleKeyInfo key = default(ConsoleKeyInfo);

                while (!((key = Console.ReadKey()).Key == ConsoleKey.Escape))
                { }
            }
        }

        private static void Debug()
        {
            var t = System.Diagnostics.Stopwatch.StartNew();
            using(var cam = new ANDOR_CS.Classes.Camera())
            {
                t.Stop();
                Console.WriteLine(cam.CameraModel + $"\t{t.ElapsedMilliseconds / 1000.0}");
                Console.ReadKey();
            }
        }
    }
}
