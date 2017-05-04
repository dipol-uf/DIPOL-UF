using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    public enum ReadMode : int
    {
        Unknown = 0,
        FullImage = (int)SDK.AC_READMODE_FULLIMAGE,
        SubImage = (int)SDK.AC_READMODE_SUBIMAGE,
        SingleTrack = (int)SDK.AC_READMODE_SINGLETRACK,
        FullVerticalBinning = (int)SDK.AC_READMODE_FVB,
        MultiTrack = (int)SDK.AC_READMODE_MULTITRACK,
        RandomTrack = (int)SDK.AC_READMODE_RANDOMTRACK,
        MultiTrackScan = (int)SDK.AC_READMODE_MULTITRACKSCAN
    }
}
