using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    public enum SDKFeatures : UInt64
    {
        Unknown = 0,
        Polling = SDK.AC_FEATURES_POLLING,
        Events = SDK.AC_FEATURES_EVENTS,
        Spooling = SDK.AC_FEATURES_SPOOLING,
        Shutter = SDK.AC_FEATURES_SHUTTER,
        ShutterEx = SDK.AC_FEATURES_SHUTTEREX,
        I2CBus = SDK.AC_FEATURES_EXTERNAL_I2C,
        SaturationEvent = SDK.AC_FEATURES_SATURATIONEVENT,
        FanControl = SDK.AC_FEATURES_FANCONTROL,
        LowFanMode = SDK.AC_FEATURES_MIDFANCONTROL,
        ReadTemperatureDuringAcquisition = SDK.AC_FEATURES_TEMPERATUREDURINGACQUISITION,
        KeepCleanControl = SDK.AC_FEATURES_KEEPCLEANCONTROL,
        DDGLite = SDK.AC_FEATURES_DDGLITE,
        FramTransferAndExternalExposure = SDK.AC_FEATURES_FTEXTERNALEXPOSURE,
        KineticAndExternalExposure = SDK.AC_FEATURES_KINETICEXTERNALEXPOSURE,
        DACCntrol = SDK.AC_FEATURES_DACCONTROL,
        MetaData = SDK.AC_FEATURES_METADATA,
        IOControl = SDK.AC_FEATURES_IOCONTROL,
        PhotonCounting = SDK.AC_FEATURES_PHOTONCOUNTING,
        CountConvert = SDK.AC_FEATURES_COUNTCONVERT,
        DualMode = SDK.AC_FEATURES_DUALMODE,
        OptAcquire = SDK.AC_FEATURES_OPTACQUIRE,
        RealTimeNoiseFilter = SDK.AC_FEATURES_REALTIMESPURIOUSNOISEFILTER,
        PostProcessNoiseFilter = SDK.AC_FEATURES_POSTPROCESSSPURIOUSNOISEFILTER,
        DualPreAmpGain = SDK.AC_FEATURES_DUALPREAMPGAIN,
        DefectCorrection = SDK.AC_FEATURES_DEFECT_CORRECTION,
        StartOfExposureEvent = SDK.AC_FEATURES_STARTOFEXPOSURE_EVENT,
        EndOfExposureEvent = SDK.AC_FEATURES_ENDOFEXPOSURE_EVENT,
        CameraLink = SDK.AC_FEATURES_CAMERALINK,
        FIFOFullEvent = 1 << 28, //SDK.AC_FEATURES_FIFOFULL_EVENT,
        MultipleSensorPort = 1 << 29, // SDK.AC_FEATURES_SENSOR_PORT_CONFIGURATION,
        SensorCompensation = 1 << 30, // SDK.AC_FEATURES_SENSOR_COMPENSATION,
        IRIGSupport = (UInt64) 1<<31, //AC_FEATURES_IRIG_SUPPORT,
        ESDEvent = ((UInt64) 1 << 0) << 32 //AC_FEATURES2_ESD_EVENTS
    }
}
