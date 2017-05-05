using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    public enum GetFunction : uint
    {
        Unknown = 0,
        Temperature =  SDK.AC_GETFUNCTION_TEMPERATURE,
        TemperatureRange = SDK.AC_GETFUNCTION_TEMPERATURERANGE,
        DetectorSize = SDK.AC_GETFUNCTION_DETECTORSIZE,
        MCPGain = SDK.AC_GETFUNCTION_MCPGAIN,
        EMCCDGain = SDK.AC_GETFUNCTION_EMCCDGAIN,
        GateMode = SDK.AC_GETFUNCTION_GATEMODE,
        DDGTimes = SDK.AC_GETFUNCTION_DDGTIMES,
        DDGIntegrateOnChip = SDK.AC_GETFUNCTION_IOC,
        Intelligate = SDK.AC_GETFUNCTION_INTELLIGATE,
        InsertionDelay = SDK.AC_GETFUNCTION_INSERTION_DELAY,
        PhosphorStatus = SDK.AC_GETFUNCTION_PHOSPHORSTATUS,
        BaselineClamp = SDK.AC_GETFUNCTION_BASELINECLAMP
    }
}
