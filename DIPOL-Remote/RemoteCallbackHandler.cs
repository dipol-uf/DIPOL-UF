using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIPOL_Remote
{
    class RemoteCallbackHandler : IRemoteCallback
    {
        public void SendToClient(string m)
        {
            Console.WriteLine($"From service \"{m}\"");
        }
    }
}
