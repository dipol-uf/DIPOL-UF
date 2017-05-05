using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    public enum EMGain : uint
    {
        Unknown = 0,
        Bits8 = SDK.AC_EMGAIN_8BIT,
        Bits12 = SDK.AC_EMGAIN_12BIT,
        LinearBits12 = SDK.AC_EMGAIN_LINEAR12,
        RealBits12 = SDK.AC_EMGAIN_REAL12
    }
}
