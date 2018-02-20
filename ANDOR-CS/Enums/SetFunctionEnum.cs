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
