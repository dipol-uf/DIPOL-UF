﻿//    This file is part of Dipol-3 Camera Manager.

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

using System.Runtime.Serialization;
using ANDOR_CS.Enums;

#if X86
using SDK = ATMCD32CS.AndorSDK;
#endif
#if X64
using SDK = ATMCD64CS.AndorSDK;
#endif



namespace ANDOR_CS.DataStructures
{
    /// <summary>
    /// Contains information about capabilities of a device.
    /// </summary>
    [DataContract]
    public struct DeviceCapabilities
    {
        [DataMember(IsRequired = true)]
        public AcquisitionMode AcquisitionModes
        {
            get;
            internal set;
        }
        [DataMember(IsRequired = true)]
        public ReadMode ReadModes
        {
            get;
            internal set;
        }
        [DataMember(IsRequired = true)]
        public ReadMode FtReadModes
        {
            get;
            internal set;
        }
        [DataMember(IsRequired = true)]
        public TriggerMode TriggerModes
        {
            get;
            internal set;
        }
        [DataMember(IsRequired = true)]
        public CameraType CameraType
        {
            get;
            internal set;
        }
        [DataMember(IsRequired = true)]
        public PixelMode PixelModes
        {
            get;
            internal set;
        }
        [DataMember(IsRequired = true)]
        public SetFunction SetFunctions
        {
            get;
            internal set;
        }
        [DataMember(IsRequired = true)]
        public GetFunction GetFunctions
        {
            get;
            internal set;
        }
        [DataMember(IsRequired = true)]
        public SdkFeatures Features
        {
            get;
            // internal set;
            set;
        }
        [DataMember(IsRequired = true)]
        public uint PciCardMaxSpeed
        {
            get;
            internal set;
        }
        [DataMember(IsRequired = true)]
        public EmGain EmGainFeatures
        {
            get;
            internal set;
        }

        public DeviceCapabilities(SDK.AndorCapabilities capabilities)
        {
            AcquisitionModes = (AcquisitionMode)capabilities.ulAcqModes;
            ReadModes = (ReadMode)capabilities.ulReadModes;
            TriggerModes = (TriggerMode)capabilities.ulTriggerModes;
            CameraType = (CameraType)capabilities.ulCameraType;
            PixelModes = (PixelMode)capabilities.ulPixelMode;
            SetFunctions = (SetFunction)capabilities.ulSetFunctions;
            GetFunctions = (GetFunction)capabilities.ulGetFunctions;
            Features = (SdkFeatures)capabilities.ulFeatures;
            PciCardMaxSpeed = capabilities.ulPCICard;
            EmGainFeatures = (EmGain)capabilities.ulEMGainCapability;
            FtReadModes = (ReadMode)capabilities.ulFTReadModes;
        }

    }
}
