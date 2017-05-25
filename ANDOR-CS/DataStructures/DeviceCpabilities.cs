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

using ATMCD64CS;
using ANDOR_CS.Enums;



namespace ANDOR_CS.DataStructures
{
    public struct DeviceCpabilities
    {
        public AcquisitionMode AcquisitionModes
        {
            get;
            internal set;
        }
        public ReadMode ReadModes
        {
            get;
            internal set;
        }
        public ReadMode FTReadModes
        {
            get;
            internal set;
        }
        public TriggerMode TriggerModes
        {
            get;
            internal set;
        }
        public CameraType CameraType
        {
            get;
            internal set;
        }
        public PixelMode PixelModes
        {
            get;
            internal set;
        }
        public SetFunction SetFunctions
        {
            get;
            internal set;
        }
        public GetFunction GetFunctions
        {
            get;
            internal set;
        }
        public SDKFeatures Features
        {
            get;
            internal set;
        }
        public uint PCICardMaxSpeed
        {
            get;
            internal set;
        }
        public EMGain EMGainFeatures
        {
            get;
            internal set;
        }

        public DeviceCpabilities(AndorSDK.AndorCapabilities capabilities)
        {
            AcquisitionModes = (AcquisitionMode)capabilities.ulAcqModes;
            ReadModes = (ReadMode)capabilities.ulReadModes;
            TriggerModes = (TriggerMode)capabilities.ulTriggerModes;
            CameraType = (CameraType)capabilities.ulCameraType;
            PixelModes = (PixelMode)capabilities.ulPixelMode;
            SetFunctions = (SetFunction)capabilities.ulSetFunctions;
            GetFunctions = (GetFunction)capabilities.ulGetFunctions;
            Features = (SDKFeatures)capabilities.ulFeatures;
            PCICardMaxSpeed = capabilities.ulPCICard;
            EMGainFeatures = (EMGain)capabilities.ulEMGainCapability;
            FTReadModes = (ReadMode)capabilities.ulFTReadModes;
        }
    }
}
