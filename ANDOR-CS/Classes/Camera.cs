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

namespace ANDOR_CS
{
    /// <summary>
    /// Represents an instance of a Camera device
    /// </summary>
    public class Camera :IDisposable
    {
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
        public CoolerMode CoolerMode
        {
            get;
            internal set;
        } = CoolerMode.Off;
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
        public int[] TemperatureRange
        {
            get;
            internal set;
        } = new int[] { 0, 0 };

        public Camera(int camIndex = 0)
        {
            try
            {
                if (camIndex < 0)
                    throw new ArgumentException($"Camera index is out of range; Cannot be less than 0 (provided {camIndex}).");
                if(camIndex >= GetNumberOfCameras())
                    throw new ArgumentException($"Camera index is out of range; Cannot be greater than {GetNumberOfCameras()-1} (provided {camIndex}).");

                int handle = 0;
                var result = SDKInit.SDKInstance.GetCameraHandle(camIndex, ref handle);
                if (result != SDK.DRV_SUCCESS)
                    throw new AndorSDKException($"{nameof(SDKInit.SDKInstance.GetCameraHandle)} returned error code.", result);

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
                if(result == SDK.DRV_SUCCESS)
                    IsInitialized = true;
                else
                    throw new AndorSDKException($"{nameof(SDKInit.SDKInstance.Initialize)} returned error code.", result);

            }
            catch (Exception e)
            {
                throw new AndorSDKException($"An exception was thrown while calling " +
                    $"{nameof(SDKInit.SDKInstance.Initialize)}", e);
            }

            GetCapabilities();

            FanControl(FanMode.Off);
            CoolerControl(CoolerMode.Off);
            GetCameraSerialNumber();
            GetHeadModel();
            GetTemperatureRange();
        }

        public void SetActive()
        {
            if (CameraHandlePtr == 0 || !IsInitialized)
                throw new AndorSDKException("Camera is not properly initialized.", new NullReferenceException());

            try
            {
                var result = SDKInit.SDKInstance.SetCurrentCamera(CameraHandlePtr);
                if (result != SDK.DRV_SUCCESS)
                    throw new AndorSDKException($"{nameof(SDKInit.SDKInstance.SetCurrentCamera)} returned error code.", result);
            }
            catch (Exception e)
            {
                throw new AndorSDKException($"An exception was thrown while calling " +
                    $"{nameof(SDKInit.SDKInstance.SetCurrentCamera)}", e);
            }
        }

        public void FanControl(FanMode mode)
        {
            var result = SDKInit.SDKInstance.SetFanMode((int)mode);

            if (result != SDK.DRV_SUCCESS)
                throw new AndorSDKException($"{nameof(SDKInit.SDKInstance.SetFanMode)} returned error code.",
                    result);
            else
                FanMode = mode;
        }

        public void CoolerControl(CoolerMode mode)
        {
            uint result = SDK.DRV_SUCCESS;

            if (mode == CoolerMode.On)
                result = SDKInit.SDKInstance.CoolerON();
            else if (mode == CoolerMode.Off)
                result = SDKInit.SDKInstance.CoolerOFF();

            if (result != SDK.DRV_SUCCESS)
                throw new AndorSDKException($"{nameof(SDKInit.SDKInstance.CoolerON)} or " +
                    $"{nameof(SDKInit.SDKInstance.CoolerON)} returned error code.",
                    result);
            else
            {
                CoolerMode = mode;
            }
        }

        public void SetTemperature(int temperature, bool startCooling = false, Nullable<FanMode> mode = null)
        {
            if ((TemperatureRange[0] != TemperatureRange[1]) &&
                (temperature > TemperatureRange[1] ||
                temperature < TemperatureRange[0]))
                throw new ArgumentOutOfRangeException($"Provided temperature ({temperature}) is out of valid range " +
                    $"({TemperatureRange[0]}, {TemperatureRange[1]}).");
            
            var result = SDKInit.SDKInstance.SetTemperature(temperature);

            if (result != SDK.DRV_SUCCESS)
                throw new AndorSDKException($"{nameof(SDKInit.SDKInstance.SetTemperature)} returned error code.",
                    result);

            if (startCooling)
            {
                FanControl(mode ?? FanMode);
                CoolerControl(CoolerMode.On);
            }
        }

        public TemperatureInfo GetCurrentTemperature()
        {
            float temp = float.NaN;

            TemperatureStatus status = TemperatureStatus.UnknownOrBusy;

            var result = SDKInit.SDKInstance.GetTemperatureF(ref temp);

            if(result == SDK.DRV_NOT_INITIALIZED || 
                result == SDK.DRV_ERROR_ACK)
                throw new AndorSDKException($"{nameof(SDKInit.SDKInstance.GetTemperatureF)} returned error code.",
                    result);

            status = (TemperatureStatus)result;

            return new TemperatureInfo()
            {
                Temperature = temp,
                Status = status
            };
        }
        
        public void Dispose()
        {
            if (CameraHandlePtr != 0 && IsInitialized)
            {
                try
                {
                    CoolerControl(CoolerMode.Off);
                    FanControl(FanMode.Off);
                    SetActive();

                    var result = SDKInit.SDKInstance.ShutDown();

                    if (result != SDK.DRV_SUCCESS)
                        throw new AndorSDKException($"{nameof(SDKInit.SDKInstance.ShutDown)} returned error code.", result);

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

            if (result != SDK.DRV_SUCCESS)
                throw new AndorSDKException($"{nameof(SDKInit.SDKInstance.GetHeadModel)} returned error code.",
                    result);
            else
                CameraModel = model;
        }

        private void GetCapabilities()
        {
            SDK.AndorCapabilities caps = default(SDK.AndorCapabilities);
            caps.ulSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(caps);

            var result = SDKInit.SDKInstance.GetCapabilities(ref caps);

            if (result != SDK.DRV_SUCCESS)
                throw new AndorSDKException($"{nameof(SDKInit.SDKInstance.GetCapabilities)} returned error code.",
                    result);
            else
                Capabilities = new DeviceCpabilities(caps);           
        }

        private void GetTemperatureRange()
        {
            int min = int.MinValue, max = int.MaxValue;

            var result = SDKInit.SDKInstance.GetTemperatureRange(ref min, ref max);

            if (result != SDK.DRV_SUCCESS)
                throw new AndorSDKException($"{nameof(SDKInit.SDKInstance.GetTemperatureRange)} returned error code.",
                    result);
            else
                TemperatureRange = new[] { min, max };
        }

        
    }

}
