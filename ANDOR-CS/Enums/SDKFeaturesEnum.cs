using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    public enum SDKFeatures : int
    {
        Unknown = 0,
        Polling = (int)SDK.AC_FEATURES_POLLING,
        Events = (int)SDK.AC_FEATURES_EVENTS,
        Spooling = (int)SDK.AC_FEATURES_SPOOLING,
        Shutter = (int)SDK.AC_FEATURES_SHUTTER,
        ShutterEx = (int)SDK.AC_FEATURES_SHUTTEREX,
        I2CBus = (int)SDK.AC_FEATURES_EXTERNAL_I2C,
        SaturationEvent = (int)SDK.AC_FEATURES_SATURATIONEVENT,
        FanControl = (int)SDK.AC_FEATURES_FANCONTROL,
        LowFanMode = (int)SDK.AC_FEATURES_MIDFANCONTROL,
        ReadTemperatureDuringAcquisition = (int)SDK.AC_FEATURES_TEMPERATUREDURINGACQUISITION,
        KeepCleanControl = (int)SDK.AC_FEATURES_KEEPCLEANCONTROL,
        DDGLite = (int)SDK.AC_FEATURES_DDGLITE,
        FramTransferAndExternalExposure = (int)SDK.AC_FEATURES_FTEXTERNALEXPOSURE,
        KineticAndExternalExposure = (int)SDK.AC_FEATURES_KINETICEXTERNALEXPOSURE,
        DACCntrol = (int)SDK.AC_FEATURES_DACCONTROL,
        MetaData = (int)SDK.AC_FEATURES_METADATA,
        IOControl = (int)SDK.AC_FEATURES_IOCONTROL,
        PhotonCounting = (int)SDK.AC_FEATURES_PHOTONCOUNTING,
        CountConvert = (int)SDK.AC_FEATURES_COUNTCONVERT,
        DualMode = (int)SDK.AC_FEATURES_DUALMODE,
        OptAcquire = (int)SDK.AC_FEATURES_OPTACQUIRE,
        RealTimeNoiseFilter = (int)SDK.AC_FEATURES_REALTIMESPURIOUSNOISEFILTER,
        PostProcessNoiseFilter = (int)SDK.AC_FEATURES_POSTPROCESSSPURIOUSNOISEFILTER,
        DualPreAmpGain = (int)SDK.AC_FEATURES_DUALPREAMPGAIN,
        DefectCorrection = (int)SDK.AC_FEATURES_DEFECT_CORRECTION,
        StartOfExposureEvent = (int)SDK.AC_FEATURES_STARTOFEXPOSURE_EVENT,
        EndOfExposureEvent = (int)SDK.AC_FEATURES_ENDOFEXPOSURE_EVENT,
        CameraLink = (int)SDK.AC_FEATURES_CAMERALINK
    }
}
