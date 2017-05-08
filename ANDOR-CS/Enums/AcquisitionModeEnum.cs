using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    public enum AcquisitionMode : uint
    {
        Unknown = 0,
        SingleScan = SDK.AC_ACQMODE_SINGLE,
        RunTillAbort = SDK.AC_ACQMODE_VIDEO,
        Accumulation = SDK.AC_ACQMODE_ACCUMULATE,
        Kinetic = SDK.AC_ACQMODE_KINETIC,
        FrameTransfer = SDK.AC_ACQMODE_FRAMETRANSFER,
        FastKinetics = SDK.AC_ACQMODE_FASTKINETICS,
        Overlap = SDK.AC_ACQMODE_OVERLAP,
        UnspecifiedMode = 1 << 7
    }
}
