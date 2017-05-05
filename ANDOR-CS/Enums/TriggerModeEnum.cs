using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{

    [Flags]
    public enum TriggerMode : uint
    {
        Unknown = 0,
        Internal = SDK.AC_TRIGGERMODE_INTERNAL,
        External = SDK.AC_TRIGGERMODE_EXTERNAL,
        ExternalFVBEM = SDK.AC_TRIGGERMODE_EXTERNAL_FVB_EM,
        Continuous = SDK.AC_TRIGGERMODE_CONTINUOUS,
        ExternalStart = SDK.AC_TRIGGERMODE_EXTERNALSTART,
        /// <summary>
        /// WARNING! Deprecated by <see cref="TriggerMode.ExternalExposure"/>
        /// </summary>
        Bulb = SDK.AC_TRIGGERMODE_BULB,
        ExternalExposure = SDK.AC_TRIGGERMODE_EXTERNALEXPOSURE,
        Inverted = SDK.AC_TRIGGERMODE_INVERTED,
        ExternalChargeshifting = SDK.AC_TRIGGERMODE_EXTERNAL_CHARGESHIFTING
    }

    
}
