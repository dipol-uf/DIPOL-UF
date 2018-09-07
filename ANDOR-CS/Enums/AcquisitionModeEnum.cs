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
using System.ComponentModel;
using System.Runtime.Serialization;

#if X86
using SDK = ATMCD32CS.AndorSDK;
#endif
#if X64
using SDK = ATMCD64CS.AndorSDK;
#endif

namespace ANDOR_CS.Enums
{
    /// <summary>
    /// Acquisition modes
    /// </summary>
    [Flags]
    [DataContract]
    public enum AcquisitionMode : uint
    {

        /// <summary>
        /// Single Scan
        /// </summary>
        [Description("Single scan")]
        [EnumMember]
        SingleScan = SDK.AC_ACQMODE_SINGLE,

        /// <summary>
        /// Run till abort or Video
        /// </summary>
        [EnumMember]
        [Description("Video mode")]
        RunTillAbort = SDK.AC_ACQMODE_VIDEO,

        /// <summary>
        /// Accumulation
        /// </summary>
        [EnumMember]
        [Description("Accumulate")]
        Accumulation = SDK.AC_ACQMODE_ACCUMULATE,

        /// <summary>
        /// Kinetic series
        /// </summary>
        [EnumMember]
        [Description("Kinetic cycle")]
        Kinetic = SDK.AC_ACQMODE_KINETIC,

        /// <summary>
        /// Frame transfer
        /// </summary>
        [EnumMember]
        [Description("Frame transfer")]
        FrameTransfer = SDK.AC_ACQMODE_FRAMETRANSFER,

        /// <summary>
        /// Fast kinetics
        /// </summary>
        [EnumMember]
        [Description("Fast kinetic cycle")]
        FastKinetics = SDK.AC_ACQMODE_FASTKINETICS,

        /// <summary>
        /// Overlap
        /// </summary>
        [EnumMember]
        [Description("Overlap")]
        Overlap = SDK.AC_ACQMODE_OVERLAP,

        /// <summary>
        /// Undocumented
        /// </summary>
        [EnumMember]
        [Description("Unknown")]
        UnspecifiedMode = 1 << 7
    }
}
