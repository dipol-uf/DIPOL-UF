using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Ports;

namespace ROTATOR_CS
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            Test2();

            Console.ReadKey();
        }

       
        private static void Test2()
        {
            var ports = SerialPort.GetPortNames();

            foreach (var port in ports)
                Console.WriteLine(port);

            List<(int, double, double)> data = new List<(int, double, double)>();

            using (var rot = new Rotator("COM2"))
            {
                //rot.DataRecieved += (sender, e) =>
                //   Console.WriteLine($"{ e.EventTime} {e.Reply}");

                //rot.ErrorRecieved += (sender, e) =>
                //    Console.WriteLine($"{ e.EventTime} {e.Reply}");

                for (int i = 1; i <= 50; i++)
                {
                    rot.SendCommand(Command.MoveToPosition, 0, (byte)CommandType.Absolute);
                    rot.WaitResponse();
                    rot.WaitPositionReached();

                    var t = System.Diagnostics.Stopwatch.StartNew();

                    var angle = 2000;
                    rot.SendCommand(Command.MoveToPosition, i * angle, (byte)CommandType.Absolute);
                    rot.WaitResponse();

                    rot.WaitPositionReached();

                    t.Stop();

                    data.Add((i * angle, t.ElapsedMilliseconds / 1000.0, i * angle * 1000.0 / t.ElapsedMilliseconds));

                    Console.WriteLine($"Rotated on {i * angle} over {t.ElapsedMilliseconds / 1000.0} with average speed " +
                        $"{(i * angle * 1000.0 / t.ElapsedMilliseconds).ToString("F1")} units per sec.");
                }

            }

            using (var str = new StreamWriter("log.dat"))
            {
                str.WriteLine("{0, 16}{1,15}{2,20}", "Angle", "Time", "Avg.Speed");
                foreach (var item in data)
                    str.WriteLine("{0, 16}{1,15:F3}{2,20:F3}", item.Item1, item.Item2, item.Item3);
            }
        }

        
    }
}
