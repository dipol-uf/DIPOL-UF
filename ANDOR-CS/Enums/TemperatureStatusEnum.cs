using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    public enum TemperatureStatus : uint
    {
        UnknownOrBusy = 0,
        Off = SDK.DRV_TEMPERATURE_OFF,
        Stabilized = SDK.DRV_TEMPERATURE_STABILIZED,
        NotReached = SDK.DRV_TEMPERATURE_NOT_REACHED,
        Drift = SDK.DRV_TEMPERATURE_DRIFT,
        NotStabilized = SDK.DRV_TEMPERATURE_NOT_STABILIZED
    }
}
