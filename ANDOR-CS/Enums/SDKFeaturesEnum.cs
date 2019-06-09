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
using ANDOR_CS.Attributes;
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
    public enum SdkFeatures : ulong
    {
        [IgnoreDefault]
        [EnumMember]
        Unknown = 0,
        [EnumMember]
        Polling = SDK.AC_FEATURES_POLLING,
        [EnumMember]
        Events = SDK.AC_FEATURES_EVENTS,
        [EnumMember]
        Spooling = SDK.AC_FEATURES_SPOOLING,
        [EnumMember]
        Shutter = SDK.AC_FEATURES_SHUTTER,
        [EnumMember]
        ShutterEx = SDK.AC_FEATURES_SHUTTEREX,
        [EnumMember]
        I2CBus = SDK.AC_FEATURES_EXTERNAL_I2C,
        [EnumMember]
        SaturationEvent = SDK.AC_FEATURES_SATURATIONEVENT,
        [EnumMember]
        FanControl = SDK.AC_FEATURES_FANCONTROL,
        [EnumMember]
        LowFanMode = SDK.AC_FEATURES_MIDFANCONTROL,
        [EnumMember]
        ReadTemperatureDuringAcquisition = SDK.AC_FEATURES_TEMPERATUREDURINGACQUISITION,
        [EnumMember]
        KeepCleanControl = SDK.AC_FEATURES_KEEPCLEANCONTROL,
        [EnumMember]
        DdgLite = SDK.AC_FEATURES_DDGLITE,
        [EnumMember]
        FrameTransferAndExternalExposure = SDK.AC_FEATURES_FTEXTERNALEXPOSURE,
        [EnumMember]
        KineticAndExternalExposure = SDK.AC_FEATURES_KINETICEXTERNALEXPOSURE,
        [EnumMember]
        DacCntrol = SDK.AC_FEATURES_DACCONTROL,
        [EnumMember]
        MetaData = SDK.AC_FEATURES_METADATA,
        [EnumMember]
        IoControl = SDK.AC_FEATURES_IOCONTROL,
        [EnumMember]
        PhotonCounting = SDK.AC_FEATURES_PHOTONCOUNTING,
        [EnumMember]
        CountConvert = SDK.AC_FEATURES_COUNTCONVERT,
        [EnumMember]
        DualMode = SDK.AC_FEATURES_DUALMODE,
        [EnumMember]
        OptAcquire = SDK.AC_FEATURES_OPTACQUIRE,
        [EnumMember]
        RealTimeNoiseFilter = SDK.AC_FEATURES_REALTIMESPURIOUSNOISEFILTER,
        [EnumMember]
        PostProcessNoiseFilter = SDK.AC_FEATURES_POSTPROCESSSPURIOUSNOISEFILTER,
        [EnumMember]
        DualPreAmpGain = SDK.AC_FEATURES_DUALPREAMPGAIN,
        [EnumMember]
        DefectCorrection = SDK.AC_FEATURES_DEFECT_CORRECTION,
        [EnumMember]
        StartOfExposureEvent = SDK.AC_FEATURES_STARTOFEXPOSURE_EVENT,
        [EnumMember]
        EndOfExposureEvent = SDK.AC_FEATURES_ENDOFEXPOSURE_EVENT,
        [EnumMember]
        CameraLink = SDK.AC_FEATURES_CAMERALINK,
        [EnumMember]
        FifoFullEvent = 1 << 28, //SDK.AC_FEATURES_FIFOFULL_EVENT,
        [EnumMember]
        MultipleSensorPort = 1 << 29, // SDK.AC_FEATURES_SENSOR_PORT_CONFIGURATION,
        [EnumMember]
        SensorCompensation = 1 << 30, // SDK.AC_FEATURES_SENSOR_COMPENSATION,
        [EnumMember]
        IrigSupport = (ulong)1 << 31, //AC_FEATURES_IRIG_SUPPORT,
        [EnumMember]
        EsdEvent = 4294967296UL //AC_FEATURES2_ESD_EVENTS
    }
}
