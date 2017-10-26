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
                rot.DataRecieved += (sender, e) 
                    => Console.WriteLine(new Reply(rot.LastRespond));
                rot.ErrorRecieved += (sender, e)
                    => Console.WriteLine(new Reply(rot.LastRespond));
                rot.SendCommand(Command.MoveToPosition, 2000);

                rot.WaitResponse();
            }
        }

        
    }
}
