using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ATMCD64CS;
using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;

using SDKInit = ANDOR_CS.AndorSDKInitialization;
using SDK = ATMCD64CS.AndorSDK;

using static ANDOR_CS.AndorSDKException;

namespace ANDOR_CS
{
    /// <summary>
    /// Represents an instance of a Camera device
    /// </summary>
    public class Camera :IDisposable
    {
        private static List<Camera> CreatedCameras = new List<Camera>();
        private static Camera ActiveCamera = null;

               
        public bool IsActive => ActiveCamera == this;
        public int CameraHandlePtr
        {
            get;
            internal set;
        } = 0;
        public bool IsInitialized
        {
            get;
            internal set;
        } = false;
        public FanMode FanMode
        {
            get;
            internal set;
        } = FanMode.Off;
        public Switch CoolerMode
        {
            get;
            internal set;
        } = Switch.Disabled;
        public string SerialNumber
        {
            get;
            internal set;
        } = "Unavailable";
        public string CameraModel
        {
            get;
            internal set;
        } = "Unknown";
        public DeviceCpabilities Capabilities
        {
            get;
            internal set;

        } = default(DeviceCpabilities);
        public CameraProperties Properties
        {
            get;
            internal set;
        }

        private void GetCapabilities()
        {
            SDK.AndorCapabilities caps = default(SDK.AndorCapabilities);
            caps.ulSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(caps);

            var result = SDKInit.SDKInstance.GetCapabilities(ref caps);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetCapabilities));
            Capabilities = new DeviceCpabilities(caps);
        }
        private void GetCameraSerialNumber()
        {
            int number = -1;
            var result = SDKInit.SDKInstance.GetCameraSerialNumber(ref number);

            if (result == SDK.DRV_SUCCESS)
                SerialNumber = number.ToString();


        }
        private void GetHeadModel()
        {
            string model = "";

            var result = SDKInit.SDKInstance.GetHeadModel(ref model);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetHeadModel));
            CameraModel = model;
        }
        private void GetCameraProperties()
        {
            int min = 0;
            int max = 0;
            uint result = 0;

            if (Capabilities.GetFunctions.HasFlag(GetFunction.TemperatureRange))
            {

                result = SDKInit.SDKInstance.GetTemperatureRange(ref min, ref max);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetTemperatureRange);

            }

            int h = 0;
            int v = 0;

            if (Capabilities.GetFunctions.HasFlag(GetFunction.DetectorSize))
            {
                result = SDKInit.SDKInstance.GetDetector(ref h, ref v);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetDetector));
                
            }

            bool shutter = false;

            if (Capabilities.CameraType == CameraType.iXon | Capabilities.CameraType == CameraType.iXonUltra)
            {
                int shutterFlag = 0;
                result = SDKInit.SDKInstance.IsInternalMechanicalShutter(ref shutterFlag);

                if (result == SDK.DRV_SUCCESS)
                    shutter = shutterFlag == 1;

            }

            int ADChannels = -1;
            int amps = -1;
            int preAmpGainMaxNumber = -1;

            result = SDKInit.SDKInstance.GetNumberADChannels(ref ADChannels);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetNumberADChannels));

            result = SDKInit.SDKInstance.GetNumberAmp(ref amps);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetNumberAmp));

            result = SDKInit.SDKInstance.GetNumberPreAmpGains(ref preAmpGainMaxNumber);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetNumberPreAmpGains));

            int VSSpeedNumber = -1;
            float speed = 0;
            float[] speedArray = new float[VSSpeedNumber];

            result = SDKInit.SDKInstance.GetNumberVSSpeeds(ref VSSpeedNumber);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetNumberVSSpeeds));

            for(int i = 0; i < VSSpeedNumber; i++)
            {
                result = SDKInit.SDKInstance.GetVSSpeed(i, ref speed);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetVSSpeed));
            }


            Properties = new CameraProperties()
            {
                AllowedTemperatures = new TemperatureRange(min, max),
                DetectorSize = new DetectorSize(h, v),
                HasInternalMechanicalShutter = shutter,
                ADChannelNumber = ADChannels,
                AmpNumber = amps,
                PreAmpGainMaximumNumber = preAmpGainMaxNumber,
                VSSpeeds = speedArray
                
            };
        }
        
        

        public void SetActive()
        {
            if (CameraHandlePtr == 0 || !IsInitialized)
                throw new AndorSDKException("Camera is not properly initialized.", new NullReferenceException());

            try
            {
                var result = SDKInit.SDKInstance.SetCurrentCamera(CameraHandlePtr);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.SetCurrentCamera));

                ActiveCamera = this;
            }
            catch (Exception e)
            {
                throw new AndorSDKException($"An exception was thrown while calling " +
                    $"{nameof(SDKInit.SDKInstance.SetCurrentCamera)}", e);
            }
        }

        public void FanControl(FanMode mode)
        {
            if (!Capabilities.Features.HasFlag(SDKFeatures.FanControl))
                throw new AndorSDKException("Camera does not support fan controls.", new ArgumentException());

            if(mode == FanMode.LowSpeed && 
                !Capabilities.Features.HasFlag(SDKFeatures.LowFanMode))
                throw new AndorSDKException("Camera does not support low-speed fan mode.", new ArgumentException());

            var result = SDKInit.SDKInstance.SetFanMode((int)mode);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.SetFanMode));

            FanMode = mode;
        }

        public void CoolerControl(Switch mode)
        {
            if(!Capabilities.SetFunctions.HasFlag(SetFunction.Temperature))
                throw new AndorSDKException("Camera does not support cooler controls.", new ArgumentException());

            uint result = SDK.DRV_SUCCESS;

            if (mode == Switch.Enabled)
                result = SDKInit.SDKInstance.CoolerON();
            else if (mode == Switch.Disabled)
                result = SDKInit.SDKInstance.CoolerOFF();

          ThrowIfError(result, nameof(SDKInit.SDKInstance.CoolerON) + " or " + nameof(SDKInit.SDKInstance.CoolerOFF));
          CoolerMode = mode;
            
        }

        public void SetTemperature(int temperature, bool startCooling = false, FanMode? mode = null)
        {
            if(!Capabilities.SetFunctions.HasFlag(SetFunction.Temperature))
                throw new AndorSDKException("Camera does not support temperature controls.", new ArgumentException());

            if (Properties.AllowedTemperatures.Minimum >= Properties.AllowedTemperatures.Maximum)
                throw new AndorSDKException("Valid temperature range was not received from camera.", new ArgumentNullException());

            if (temperature > Properties.AllowedTemperatures.Maximum ||
                temperature < Properties.AllowedTemperatures.Minimum )
                throw new ArgumentOutOfRangeException($"Provided temperature ({temperature}) is out of valid range " +
                    $"({Properties.AllowedTemperatures.Maximum }, " +
                     $"{Properties.AllowedTemperatures.Minimum }).");
            
            var result = SDKInit.SDKInstance.SetTemperature(temperature);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.SetTemperature));

            if (startCooling)
            {
                FanControl(mode ?? FanMode);
                CoolerControl(Switch.Enabled);
            }
        }

        public TemperatureInfo GetCurrentTemperature()
        {
            if(!Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                throw new AndorSDKException("Camera does not support temperature inquires.", new ArgumentException());

            float temp = float.NaN;

            TemperatureStatus status = TemperatureStatus.UnknownOrBusy;

            var result = SDKInit.SDKInstance.GetTemperatureF(ref temp);

            if(result == SDK.DRV_NOT_INITIALIZED || 
                result == SDK.DRV_ERROR_ACK)
                throw new AndorSDKException($"{nameof(SDKInit.SDKInstance.GetTemperatureF)} returned error code.",
                    result);

            if(result == SDK.DRV_ACQUIRING)
                throw new AndorSDKException("Acquisition is in progress.", result);

            status = (TemperatureStatus)result;
            
            return new TemperatureInfo()
            {
                Temperature = temp,
                Status = status
            };
        }


        public Camera(int camIndex = 0)
        {
            try
            {
                if (camIndex < 0)
                    throw new ArgumentException($"Camera index is out of range; Cannot be less than 0 (provided {camIndex}).");
                if (camIndex >= GetNumberOfCameras())
                    throw new ArgumentException($"Camera index is out of range; Cannot be greater than {GetNumberOfCameras() - 1} (provided {camIndex}).");

                int handle = 0;
                var result = SDKInit.SDKInstance.GetCameraHandle(camIndex, ref handle);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetCameraHandle));

                CameraHandlePtr = handle;

            }
            catch (Exception e)
            {
                throw new AndorSDKException($"An exception was thrown while calling " +
                    $"{nameof(SDKInit.SDKInstance.GetCameraHandle)}", e);
            }


            try
            {
                var result = SDKInit.SDKInstance.Initialize(".\\");
                if (result == SDK.DRV_SUCCESS)
                {
                    IsInitialized = true;
                    CreatedCameras.Add(this);
                }
                else
                    throw new AndorSDKException($"{nameof(SDKInit.SDKInstance.Initialize)} returned error code.", result);

            }
            catch (Exception e)
            {
                throw new AndorSDKException($"An exception was thrown while calling " +
                    $"{nameof(SDKInit.SDKInstance.Initialize)}", e);
            }

            SetActive();
            GetCapabilities();
            GetCameraProperties();
            GetCameraSerialNumber();
            GetHeadModel();

            FanControl(FanMode.Off);
            CoolerControl(Switch.Disabled);

        }

        public void Dispose()
        {
            if (CameraHandlePtr != 0 && IsInitialized)
            {
                try
                {
                    var oldCamera = ActiveCamera;

                    SetActive();
                    CoolerControl(Switch.Disabled);
                    FanControl(FanMode.Off);
                   
                    var result = SDKInit.SDKInstance.ShutDown();
                    ThrowIfError(result, nameof(SDKInit.SDKInstance.ShutDown));

                    CreatedCameras.Remove(this);

                    if (CreatedCameras.Count == 0)
                        ActiveCamera = null;
                    else CreatedCameras.First().SetActive();

                }
                catch (Exception e)
                {
                    throw new AndorSDKException("An error occured while releasing Camera resources.", e);
                }
            }

                
        }



        public static int GetNumberOfCameras()
        {
            int cameraCount = -1;

            try
            {
                var result = SDKInit.SDKInstance.GetAvailableCameras(ref cameraCount);
                if (result != SDK.DRV_SUCCESS)
                {
                    cameraCount = -1;
                    throw new AndorSDKException($"{ nameof(SDKInit.SDKInstance.GetAvailableCameras) } returned error code.",
                        result);
                }
            }
            catch (Exception e)
            {
                throw new AndorSDKException($"An exception was thrown while calling " +
                    $"{nameof(SDKInit.SDKInstance.GetAvailableCameras)}", e);
            }


            return cameraCount;
        }
               
        
        
    }

}
