using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICameraControl = ANDOR_CS.Interfaces.ICameraControl;

namespace DIPOL_Remote.Classes
{
    public class RemoteCamera //: ICameraControl
    {
        private RemoteControl session;
        private int SDKPtr;

        internal RemoteCamera(RemoteControl sessionInstance, int SDKPtr)
        {
            session = sessionInstance;
            this.SDKPtr = SDKPtr;
        }


    }
}
