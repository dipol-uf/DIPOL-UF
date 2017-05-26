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
    [Flags]
    public enum SetFunction: uint
    {

        /// <summary>
        /// Supports changing of vertical reading speed
        /// </summary>
        VerticalReadoutSpeed =  SDK.AC_SETFUNCTION_VREADOUT,

        /// <summary>
        /// Supports changing of horizontal reading speed
        /// </summary>
        HorizontalReadoutSpeed = SDK.AC_SETFUNCTION_HREADOUT,

        /// <summary>
        /// Supports temperature control
        /// </summary>
        Temperature = SDK.AC_SETFUNCTION_TEMPERATURE,
        MCPGain = SDK.AC_SETFUNCTION_MCPGAIN,
        EMCCDGain = SDK.AC_SETFUNCTION_EMCCDGAIN,
        BaselineClamp =  SDK.AC_SETFUNCTION_BASELINECLAMP,

        /// <summary>
        /// Supports vertical clock voltage amplitude control
        /// </summary>
        VerticalClockVoltage = SDK.AC_SETFUNCTION_VSAMPLITUDE,
        HighCapacityMode = SDK.AC_SETFUNCTION_HIGHCAPACITY,
        BaseLineOffset = SDK.AC_SETFUNCTION_BASELINEOFFSET,

        /// <summary>
        /// Supports Pre Amp Gain control
        /// </summary>
        PreAmpGain = SDK.AC_SETFUNCTION_PREAMPGAIN,
        CropMode = SDK.AC_SETFUNCTION_CROPMODE,
        DMAPArameters = SDK.AC_SETFUNCTION_DMAPARAMETERS,
        HorizontalBinning = SDK.AC_SETFUNCTION_HORIZONTALBIN,
        MultitrackHorizontalRange = SDK.AC_SETFUNCTION_MULTITRACKHRANGE,
        RandomTrackNoGaps = SDK.AC_SETFUNCTION_RANDOMTRACKNOGAPS,
        EMGainAdvanced = SDK.AC_SETFUNCTION_EMADVANCED,
        GateMode = SDK.AC_SETFUNCTION_GATEMODE,
        DDGTimes = SDK.AC_SETFUNCTION_DDGTIMES,
        DDGIntegrateOnChip = SDK.AC_SETFUNCTION_IOC,
        Intelligate = SDK.AC_SETFUNCTION_INTELLIGATE,
        InsertionDelay = SDK.AC_SETFUNCTION_INSERTION_DELAY,
        GateStep = SDK.AC_SETFUNCTION_GATESTEP,
        TriggerTermination = SDK.AC_SETFUNCTION_TRIGGERTERMINATION,
        ExtendedNIRMode = SDK.AC_SETFUNCTION_EXTENDEDNIR,
        SpoolThreadCount = SDK.AC_SETFUNCTION_SPOOLTHREADCOUNT,
        RegisterPack = SDK.AC_SETFUNCTION_REGISTERPACK,
        Prescans = SDK.AC_SETFUNCTION_PRESCANS,
        GateWidthStep = SDK.AC_SETFUNCTION_GATEWIDTHSTEP,
        ExtendedCropMode = SDK.AC_SETFUNCTION_EXTENDED_CROP_MODE,
        SuperKinetics = 1 << 29,//SDK.AC_SETFUNCTION_SUPERKINETICS,
        TimeScan = 1 << 30, //SDK.AC_SETFUNCTION_TIMESCAN,
        CropModeType = (uint)1 << 31 //SDK.AC_SETFUNCTION_CROPMODETYPE
    }
}
