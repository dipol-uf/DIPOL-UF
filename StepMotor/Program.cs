using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Ports;

namespace StepMotor
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
            var angle = 2*4000;

            using (var rot = new StepMotorHandler("COM2"))
            {
                //rot.DataRecieved += (sender, e) =>
                //   Console.WriteLine($"{ e.EventTime} {e.Reply}");

                //rot.ErrorRecieved += (sender, e) =>
                //    Console.WriteLine($"{ e.EventTime} {e.Reply}");

                rot.SendCommand(Command.MoveToPosition, 0, (byte)CommandType.Absolute);
                rot.WaitPositionReached(checkIntervalMS: 100);

                for (int i = 1; i <= 16; i++)
                {

                    var t = System.Diagnostics.Stopwatch.StartNew();

                    rot.SendCommand(Command.MoveToPosition, i * angle, (byte)CommandType.Absolute);

                    rot.WaitPositionReached(checkIntervalMS: 0);

                    t.Stop();

                    data.Add((i * angle, t.ElapsedMilliseconds / 1000.0, i * angle * 1000.0 / t.ElapsedMilliseconds));

                    Console.WriteLine($"Rotated on {{0, 9}} over {(t.ElapsedMilliseconds / 1000.0).ToString("F3")} with average speed " +
                        $"{(angle * 1000.0 / t.ElapsedMilliseconds).ToString("F1")} units per sec.", i * angle);
                }

                int oldSpeed = rot.SendCommand(Command.GetAxisParameter, 0, (byte)AxisParameter.MaximumSpeed).ReturnValue;
                Console.WriteLine(oldSpeed);
                if (rot.SendCommand(Command.SetAxisParameter, 2000, (byte)AxisParameter.MaximumSpeed).Status != ReturnStatus.Success)
                    throw new Exception();

                var t2 = System.Diagnostics.Stopwatch.StartNew();
                rot.SendCommand(Command.MoveToPosition, 0, (byte)CommandType.Absolute);

                Console.WriteLine("Average time: {0:F3}  speed: {1:F3}", data.Select(x => x.Item2).Average(), data.Select(x => angle / x.Item2).Average());

                System.Threading.Thread.Sleep(200);

                var status = rot.GetStatus();

                foreach (var item in status)
                    Console.WriteLine($"{item.Key}\t {item.Value}");

                rot.WaitPositionReached(checkIntervalMS: 100);

                t2.Stop();



                Console.WriteLine("Rotation back took {0:f3} second.", t2.ElapsedMilliseconds / 1000.0);
                Console.WriteLine("Total cycle took {0:f3} second.", data.Select(x => x.Item2).Sum() + t2.ElapsedMilliseconds / 1000.0);

                rot.SendCommand(Command.SetAxisParameter, oldSpeed, (byte)AxisParameter.MaximumSpeed);
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
