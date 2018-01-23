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

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    [Flags]
    [DataContract]
    public enum SdkFeatures : UInt64
    {
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
        IrigSupport = (UInt64)1 << 31, //AC_FEATURES_IRIG_SUPPORT,
        [EnumMember]
        EsdEvent = (UInt64)1 << 32 //AC_FEATURES2_ESD_EVENTS
    }
}
