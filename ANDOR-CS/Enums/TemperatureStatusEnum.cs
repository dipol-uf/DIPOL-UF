using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    public enum TemperatureStatus : int
    {
        UnknownOrBusy = 0,
        Off = (int)SDK.DRV_TEMPERATURE_OFF,
        Stabilized = (int)SDK.DRV_TEMPERATURE_STABILIZED,
        NotReached = (int)SDK.DRV_TEMPERATURE_NOT_REACHED,
        Drift = (int)SDK.DRV_TEMPERATURE_DRIFT,
        NotStabilized = (int)SDK.DRV_TEMPERATURE_NOT_STABILIZED
    }
}
