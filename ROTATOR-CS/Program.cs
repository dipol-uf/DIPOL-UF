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
            Test2();

            Console.ReadKey();
        }

       
        private static void Test2()
        {
            var ports = SerialPort.GetPortNames();

            foreach (var port in ports)
                Console.WriteLine(port);

            using (var rot = new Rotator("COM2"))
            {
                rot.DataRecieved += (sender, e) =>
                   Console.WriteLine(new Reply(rot.LastResponse));
                 
                rot.ErrorRecieved += (sender, e)
                    => Console.WriteLine(new Reply(rot.LastResponse));


                int N = 250;

                rot.SendCommand(Command.MoveToPosition, 0000000, (byte) CommandType.Absolute);
                //System.Threading.Thread.Sleep(300);

                //var t = System.Diagnostics.Stopwatch.StartNew();

                //for (int i = 0; i < N; i++)
                //{

                rot.WaitPositionReached();
                Console.WriteLine("Position reached");

                //for (int i = 0; i < N; i++)
                //{
                //    var statusDictionary = rot.GetStatus();
                //    if (i % 10 == 0)
                //        Console.Clear();
                //    Console.WriteLine("----------------------------------");
                //    foreach (var status in statusDictionary)
                //        Console.WriteLine("{0,30}\t{1}", status.Key,  
                //            (byte)status.Key < 8 ? status.Value.ToString() : (status.Value > 0).ToString());
                //    //if (N % 100 == 0)
                //    //    Console.Clear();
                //    System.Threading.Thread.Sleep(150);
                //    Console.CursorLeft = 0;
                //    Console.CursorTop = 0;
                //}
                    //rot.WaitResponse();
                //}

                //t.Stop();
                //Console.WriteLine("Total time: {0:F3}; Per rotation on {1} units: {2:e3}",
                    //t.ElapsedMilliseconds / 1000.0, step, t.ElapsedMilliseconds / 1000.0 / N);
            }
        }

        
    }
}
