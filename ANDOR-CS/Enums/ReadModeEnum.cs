using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    public enum ReadMode : uint
    {
        Unknown = 0,
        FullImage = SDK.AC_READMODE_FULLIMAGE,
        SubImage = SDK.AC_READMODE_SUBIMAGE,
        SingleTrack = SDK.AC_READMODE_SINGLETRACK,
        FullVerticalBinning = SDK.AC_READMODE_FVB,
        MultiTrack = SDK.AC_READMODE_MULTITRACK,
        RandomTrack = SDK.AC_READMODE_RANDOMTRACK,
        MultiTrackScan = SDK.AC_READMODE_MULTITRACKSCAN
    }
}
