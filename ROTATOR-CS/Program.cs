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

        static void Test()
        {
            var str = SerialPort.GetPortNames();
           
            using (var rot = new Rotator("COM2"))
            {
                bool isWaiting = false;

                rot.DataRecieved += (sender, e) =>
                {
                    Console.WriteLine("-----------------------");
                    foreach (var b in rot.LastRespond)
                        Console.Write("{0:X2} ", b);

                    byte[] subArr = new byte[4];

                    for (int i = 0; i < subArr.Length; i++)
                        subArr[i] = BitConverter.IsLittleEndian ? rot.LastRespond[4 + subArr.Length - 1 - i] : rot.LastRespond[4 + i];

                    var returnedVal = BitConverter.ToInt32(subArr, 0);

                    Console.WriteLine();
                    Console.WriteLine(returnedVal);
                    Console.WriteLine("-----------------------");
                    isWaiting = false;

                };

                while (true)
                {
                    while (isWaiting)
                        System.Threading.Thread.Sleep(100);

                    Console.Write("Input: ");
                    int angle = int.Parse(Console.ReadLine());

                    rot.SendCommand(Command.MoveToPosition, angle);
                    isWaiting = true;
                    
                    if (angle == 0)
                        break;

                    Console.WriteLine();
                }
            }

        }

        private static void Test2()
        {
            var ports = SerialPort.GetPortNames();

            foreach (var port in ports)
                Console.WriteLine(port);

            using (var rot = new Rotator("COM2"))
            {

            }
        }

        
    }
}
