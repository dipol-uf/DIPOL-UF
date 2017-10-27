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
            var angle = 4000;

            using (var rot = new Rotator("COM2"))
            {
                //rot.DataRecieved += (sender, e) =>
                //   Console.WriteLine($"{ e.EventTime} {e.Reply}");

                //rot.ErrorRecieved += (sender, e) =>
                //    Console.WriteLine($"{ e.EventTime} {e.Reply}");

                rot.SendCommand(Command.MoveToPosition, 0, (byte)CommandType.Absolute);
                rot.WaitResponse();
                rot.WaitPositionReached();

                for (int i = 1; i <= 50; i++)
                {

                    var t = System.Diagnostics.Stopwatch.StartNew();

                    rot.SendCommand(Command.MoveToPosition, i * angle, (byte)CommandType.Absolute);
                    rot.WaitResponse();

                    rot.WaitPositionReached(checkIntervalMS: 50);

                    t.Stop();

                    data.Add((i * angle, t.ElapsedMilliseconds / 1000.0, i * angle * 1000.0 / t.ElapsedMilliseconds));

                    Console.WriteLine($"Rotated on {{0, 9}} over {(t.ElapsedMilliseconds / 1000.0).ToString("F3")} with average speed " +
                        $"{(angle * 1000.0 / t.ElapsedMilliseconds).ToString("F1")} units per sec.", i * angle);
                }

            }

            Console.WriteLine("Average time: {0:F3}  speed: {1:F3}", data.Select(x => x.Item2).Average(),  data.Select(x => angle / x.Item2).Average());

            using (var str = new StreamWriter("log.dat"))
            {
                str.WriteLine("{0, 16}{1,15}{2,20}", "Angle", "Time", "Avg.Speed");
                foreach (var item in data)
                    str.WriteLine("{0, 16}{1,15:F3}{2,20:F3}", item.Item1, item.Item2, item.Item3);
            }
        }

        
    }
}
