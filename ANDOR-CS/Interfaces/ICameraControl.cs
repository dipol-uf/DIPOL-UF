using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;

namespace ANDOR_CS.Interfaces
{
    public interface ICameraControl : IDisposable
    {
        DeviceCpabilities Capabilities { get; }

        CameraProperties Properties { get; }

        bool IsActive { get; }
      
        bool IsInitialized { get; }
        
        FanMode FanMode { get; }
         
        Switch CoolerMode { get; }

        CameraStatus GetStatus();

    }
}
