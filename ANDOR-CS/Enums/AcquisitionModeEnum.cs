//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

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
        [Description("Unknown")]
        [EnumMember]
        Unknown = 0,

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
