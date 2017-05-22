using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    /// <summary>
    /// Acquisition modes
    /// </summary>
    [Flags]
    public enum AcquisitionMode : uint
    {
        /// <summary>
        /// Default mode
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Single Scan
        /// </summary>
        SingleScan = SDK.AC_ACQMODE_SINGLE,

        /// <summary>
        /// Run till abort or Video
        /// </summary>
        RunTillAbort = SDK.AC_ACQMODE_VIDEO,

        /// <summary>
        /// Accumulation
        /// </summary>
        Accumulation = SDK.AC_ACQMODE_ACCUMULATE,

        /// <summary>
        /// Kinetic series
        /// </summary>
        Kinetic = SDK.AC_ACQMODE_KINETIC,

        /// <summary>
        /// Frame transfer
        /// </summary>
        FrameTransfer = SDK.AC_ACQMODE_FRAMETRANSFER,

        /// <summary>
        /// Fast kinetics
        /// </summary>
        FastKinetics = SDK.AC_ACQMODE_FASTKINETICS,

        /// <summary>
        /// Overlap
        /// </summary>
        Overlap = SDK.AC_ACQMODE_OVERLAP,

        /// <summary>
        /// Undocumented
        /// </summary>
        UnspecifiedMode = 1 << 7
    }
}
