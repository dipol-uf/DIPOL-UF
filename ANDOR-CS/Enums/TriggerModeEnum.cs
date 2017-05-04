using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{

    [Flags]
    public enum TriggerMode : int
    {
        Unknown = 0,
        Internal = (int)SDK.AC_TRIGGERMODE_INTERNAL,
        External = (int)SDK.AC_TRIGGERMODE_EXTERNAL,
        ExternalFVBEM = (int)SDK.AC_TRIGGERMODE_EXTERNAL_FVB_EM,
        Continuous = (int)SDK.AC_TRIGGERMODE_CONTINUOUS,
        ExternalStart = (int)SDK.AC_TRIGGERMODE_EXTERNALSTART,
        /// <summary>
        /// WARNING! Deprecated by <code>TriggerMode.ExternalExposure</code>
        /// </summary>
        Bulb = (int)SDK.AC_TRIGGERMODE_BULB,
        ExternalExposure = (int)SDK.AC_TRIGGERMODE_EXTERNALEXPOSURE,
        Inverted = (int)SDK.AC_TRIGGERMODE_INVERTED,
        ExternalChargeshifting = (int)SDK.AC_TRIGGERMODE_EXTERNAL_CHARGESHIFTING
    }

    
}
