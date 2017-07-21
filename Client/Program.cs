using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new DIPOL_Remote.DipolClient();

            Console.WriteLine("Session ID {0}", client.Remote.Connect());

            Console.ReadKey();

            client.Dispose();
        }
    }
}
