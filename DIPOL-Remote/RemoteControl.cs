//    This file is part of Dipol-3 Camera Manager.

//    Dipol-3 Camera Manager is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    Dipol-3 Camera Manager is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with Dipol-3 Camera Manager.  If not, see<http://www.gnu.org/licenses/>.
//
//    Copyright 2017, Ilia Kosenkov, Tuorla Observatory, Finland

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;

namespace DIPOL_Remote
{
    [ServiceBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple, 
        InstanceContextMode = InstanceContextMode.PerSession,
        UseSynchronizationContext = true,
        IncludeExceptionDetailInFaults = true)]
    public class RemoteControl : IRemoteControl, IDisposable
    {
        private string sessionID = "";
        private bool IsDisposing = false;
        private OperationContext context;

        public static System.Collections.Concurrent.ConcurrentBag<RemoteControl> ServiceInstances = 
            new System.Collections.Concurrent.ConcurrentBag<RemoteControl>();

        public RemoteControl()
        {
            sessionID = Guid.NewGuid().ToString("N");
            Console.WriteLine("Constructor invoked.");

        }
                
        public string SessionID
        {
            [OperationBehavior]
            get => sessionID;
            
        }

        [OperationBehavior]
        public void Connect()
        {
            context = OperationContext.Current;
            ServiceInstances.Add(this);
            Console.WriteLine("Connection initialized.");
        }

        [OperationBehavior]
        public void Disconnect()
        {
            IsDisposing = true;
            Dispose();
        }

        public void Dispose()
        {
            Console.WriteLine("In dispose");
            if(IsDisposing)
                Console.WriteLine("Disposing...");
        }

        [OperationBehavior]
        public int GetNumberOfCameras()
        {
            return 3;
        }

        public void SendToClient()
        {
            Console.WriteLine(context == null);
            context.GetCallbackChannel<IRemoteCallback>().SendToClient("Hello from service");
        }

    }
}
