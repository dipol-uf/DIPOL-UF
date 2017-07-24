using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;

namespace DIPOL_Remote
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple,
        IncludeExceptionDetailInFaults = true)]
    class RemoteCallbackHandler : IRemoteCallback
    {
        public RemoteCallbackHandler()
        {
            Console.WriteLine("Remote control handler created");
        }
        public void SendToClient(string m)
        {
            Console.WriteLine($"From service \"{m}\"");
        }
    }
}
