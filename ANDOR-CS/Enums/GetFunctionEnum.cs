using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    public enum GetFunction : int
    {
        Unknown = 0,
        Temperature = (int) SDK.AC_GETFUNCTION_TEMPERATURE,
        TemperatureRange = (int)SDK.AC_GETFUNCTION_TEMPERATURERANGE,
        DetectorSize = (int)SDK.AC_GETFUNCTION_DETECTORSIZE,
        MCPGain = (int)SDK.AC_GETFUNCTION_MCPGAIN,
        EMCCDGain = (int)SDK.AC_GETFUNCTION_EMCCDGAIN,
        GateMode = (int)SDK.AC_GETFUNCTION_GATEMODE,
        DDGTimes = (int)SDK.AC_GETFUNCTION_DDGTIMES,
        DDGIntegrateOnChip = (int)SDK.AC_GETFUNCTION_IOC,
        Intelligate = (int)SDK.AC_GETFUNCTION_INTELLIGATE,
        InsertionDelay = (int)SDK.AC_GETFUNCTION_INSERTION_DELAY,
        PhosphorStatus = (int)SDK.AC_GETFUNCTION_PHOSPHORSTATUS,
        BaselineClamp = (int)SDK.AC_GETFUNCTION_BASELINECLAMP
    }
}
