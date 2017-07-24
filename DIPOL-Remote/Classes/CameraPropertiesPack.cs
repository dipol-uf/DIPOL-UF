using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Enums;

namespace DIPOL_Remote.Classes
{
    public struct CameraPropertiesPack
    {
        bool IsActive;
        bool IsInitialized;
        string SerialNumber;
        string CameraModel;
        FanMode FanMode;
        Switch CoolerMode;
        int CameraIndex;
    }
}
