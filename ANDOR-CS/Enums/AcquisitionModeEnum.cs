//    This file is part of Dipol-3 Camera Manager.

//    Dipol-3 Camera Manager is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    Dipol-3 Camera Manager is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with Dipol-3 Camera Manager.  If not, see<http://www.gnu.org/licenses/>.
//
//    Copyright 2017, Ilia Kosenkov, Tuorla Observatory, Finland

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
