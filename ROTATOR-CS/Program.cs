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


                int N = 500;

                rot.SendCommand(Command.MoveToPosition, 0000, (byte) CommandType.Absolute);
                //System.Threading.Thread.Sleep(300);

                //var t = System.Diagnostics.Stopwatch.StartNew();

                //for (int i = 0; i < N; i++)
                //{

                for (int i = 0; i < N; i++)
                {
                    rot.SendCommand(Command.GetAxisParameter, 0, 1);
                    //System.Threading.Thread.Sleep(1000);
                }
                    //rot.WaitResponse();
                //}

                //t.Stop();
                //Console.WriteLine("Total time: {0:F3}; Per rotation on {1} units: {2:e3}",
                    //t.ElapsedMilliseconds / 1000.0, step, t.ElapsedMilliseconds / 1000.0 / N);
            }
        }

        
    }
}
