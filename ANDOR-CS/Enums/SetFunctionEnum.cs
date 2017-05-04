using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    public enum SetFunction: int
    {
        Unknown = 0,
        VerticalReadoutSpeed = (int) SDK.AC_SETFUNCTION_VREADOUT,
        HorizontalReadoutSpeed = (int)SDK.AC_SETFUNCTION_HREADOUT,
        Temperature = (int)SDK.AC_SETFUNCTION_TEMPERATURE,
        MCPGain = (int)SDK.AC_SETFUNCTION_MCPGAIN,
        EMCCDGain = (int)SDK.AC_SETFUNCTION_EMCCDGAIN,
        BaselineClamp = (int) SDK.AC_SETFUNCTION_BASELINECLAMP,
        VerticalClockVoltage = (int)SDK.AC_SETFUNCTION_VSAMPLITUDE,
        HighCapacityMode = (int)SDK.AC_SETFUNCTION_HIGHCAPACITY,
        BaseLineOffset = (int)SDK.AC_SETFUNCTION_BASELINEOFFSET,
        PreAmpGain = (int)SDK.AC_SETFUNCTION_PREAMPGAIN,
        CropMode = (int)SDK.AC_SETFUNCTION_CROPMODE,
        DMAPArameters = (int)SDK.AC_SETFUNCTION_DMAPARAMETERS,
        HorizontalBinning = (int)SDK.AC_SETFUNCTION_HORIZONTALBIN,
        MultitrackHorizontalRange = (int)SDK.AC_SETFUNCTION_MULTITRACKHRANGE,
        RandomTrackNoGaps = (int)SDK.AC_SETFUNCTION_RANDOMTRACKNOGAPS,
        EMGainAdvanced = (int)SDK.AC_SETFUNCTION_EMADVANCED,
        GateMode = (int)SDK.AC_SETFUNCTION_GATEMODE,
        DDGTimes = (int)SDK.AC_SETFUNCTION_DDGTIMES,
        DDGIntegrateOnChip = (int)SDK.AC_SETFUNCTION_IOC,
        Intelligate = (int)SDK.AC_SETFUNCTION_INTELLIGATE,
        InsertionDelay = (int)SDK.AC_SETFUNCTION_INSERTION_DELAY,
        GateStep = (int)SDK.AC_SETFUNCTION_GATESTEP,
        TriggerTermination = (int)SDK.AC_SETFUNCTION_TRIGGERTERMINATION,
        ExtendedNIRMode = (int)SDK.AC_SETFUNCTION_EXTENDEDNIR,
        SpoolThreadCount = (int)SDK.AC_SETFUNCTION_SPOOLTHREADCOUNT,
        RegisterPack = (int)SDK.AC_SETFUNCTION_REGISTERPACK,
        Prescans = (int)SDK.AC_SETFUNCTION_PRESCANS,
        GateWidthStep = (int)SDK.AC_SETFUNCTION_GATEWIDTHSTEP,
        ExtendedCropMode = (int)SDK.AC_SETFUNCTION_EXTENDED_CROP_MODE
    }
}
