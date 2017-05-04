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
