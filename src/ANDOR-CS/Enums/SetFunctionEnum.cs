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
using System.Runtime.Serialization;

#if X86
using SDK = ATMCD32CS.AndorSDK;
#endif
#if X64
using SDK = ATMCD64CS.AndorSDK;
#endif


namespace ANDOR_CS.Enums
{
    [Flags]
    [DataContract]
    public enum SetFunction: uint
    {

        /// <summary>
        /// Supports changing of vertical reading speed
        /// </summary>
        [EnumMember]
        VerticalReadoutSpeed =  SDK.AC_SETFUNCTION_VREADOUT,

        /// <summary>
        /// Supports changing of horizontal reading speed
        /// </summary>
        [EnumMember]
        HorizontalReadoutSpeed = SDK.AC_SETFUNCTION_HREADOUT,

        /// <summary>
        /// Supports temperature control
        /// </summary>
        [EnumMember]
        Temperature = SDK.AC_SETFUNCTION_TEMPERATURE,
        [EnumMember]
        MCPGain = SDK.AC_SETFUNCTION_MCPGAIN,
        [EnumMember]
        EMCCDGain = SDK.AC_SETFUNCTION_EMCCDGAIN,
        [EnumMember]
        BaselineClamp =  SDK.AC_SETFUNCTION_BASELINECLAMP,

        /// <summary>
        /// Supports vertical clock voltage amplitude control
        /// </summary>
        [EnumMember]
        VerticalClockVoltage = SDK.AC_SETFUNCTION_VSAMPLITUDE,
        [EnumMember]
        HighCapacityMode = SDK.AC_SETFUNCTION_HIGHCAPACITY,
        [EnumMember]
        BaseLineOffset = SDK.AC_SETFUNCTION_BASELINEOFFSET,


        /// <summary>
        /// Supports Pre Amp Gain control
        /// </summary>
        [EnumMember]
        PreAmpGain = SDK.AC_SETFUNCTION_PREAMPGAIN,
        [EnumMember]
        CropMode = SDK.AC_SETFUNCTION_CROPMODE,
        [EnumMember]
        DmapArameters = SDK.AC_SETFUNCTION_DMAPARAMETERS,
        [EnumMember]
        HorizontalBinning = SDK.AC_SETFUNCTION_HORIZONTALBIN,
        [EnumMember]
        MultitrackHorizontalRange = SDK.AC_SETFUNCTION_MULTITRACKHRANGE,
        [EnumMember]
        RandomTrackNoGaps = SDK.AC_SETFUNCTION_RANDOMTRACKNOGAPS,
        [EnumMember]
        EmGainAdvanced = SDK.AC_SETFUNCTION_EMADVANCED,
        [EnumMember]
        GateMode = SDK.AC_SETFUNCTION_GATEMODE,
        [EnumMember]
        DdgTimes = SDK.AC_SETFUNCTION_DDGTIMES,
        [EnumMember]
        DdgIntegrateOnChip = SDK.AC_SETFUNCTION_IOC,
        [EnumMember]
        Intelligate = SDK.AC_SETFUNCTION_INTELLIGATE,
        [EnumMember]
        InsertionDelay = SDK.AC_SETFUNCTION_INSERTION_DELAY,
        [EnumMember]
        GateStep = SDK.AC_SETFUNCTION_GATESTEP,
        [EnumMember]
        TriggerTermination = SDK.AC_SETFUNCTION_TRIGGERTERMINATION,
        [EnumMember]
        ExtendedNirMode = SDK.AC_SETFUNCTION_EXTENDEDNIR,
        [EnumMember]
        SpoolThreadCount = SDK.AC_SETFUNCTION_SPOOLTHREADCOUNT,
        [EnumMember]
        RegisterPack = SDK.AC_SETFUNCTION_REGISTERPACK,
        [EnumMember]
        Prescans = SDK.AC_SETFUNCTION_PRESCANS,
        [EnumMember]
        GateWidthStep = SDK.AC_SETFUNCTION_GATEWIDTHSTEP,
        [EnumMember]
        ExtendedCropMode = SDK.AC_SETFUNCTION_EXTENDED_CROP_MODE,
        [EnumMember]
        SuperKinetics = 1 << 29,//SDK.AC_SETFUNCTION_SUPERKINETICS,
        [EnumMember]
        TimeScan = 1 << 30, //SDK.AC_SETFUNCTION_TIMESCAN,
        [EnumMember]
        CropModeType = (uint)1 << 31 //SDK.AC_SETFUNCTION_CROPMODETYPE
    }
}
