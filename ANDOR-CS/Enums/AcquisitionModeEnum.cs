using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    public enum AcquisitionMode : int
    {
        Unknown = 0,
        SingleScan = (int)SDK.AC_ACQMODE_SINGLE,
        RunTillAbort = (int)SDK.AC_ACQMODE_VIDEO,
        Accumulation = (int)SDK.AC_ACQMODE_ACCUMULATE,
        Kinetic = (int)SDK.AC_ACQMODE_KINETIC,
        FrameTransfer = (int)SDK.AC_ACQMODE_FRAMETRANSFER,
        FastKinetics = (int)SDK.AC_ACQMODE_FASTKINETICS,
        Overlap = (int)SDK.AC_ACQMODE_OVERLAP
    }
}
