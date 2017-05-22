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

namespace ANDOR_CS.Classes
{
    /// <summary>
    /// Represents an instance of a Camera device
    /// </summary>
    public class Camera :IDisposable
    {
        private static readonly int AmpDescriptorMaxLength = 21;
        private static readonly int PreAmpGainDescriptorMaxLength = 30;

        private static List<Camera> CreatedCameras = new List<Camera>();
        private static Camera ActiveCamera = null;

        
        /// <summary>
        /// Indicates if this camera is currently active
        /// </summary>
        public bool IsActive => ActiveCamera.CameraHandle.SDKPtr == this.CameraHandle.SDKPtr;
        //public int CameraHandlePtr
        public SafeSDKCameraHandle CameraHandle
        {
            get;
            private set;
        } = null;
        public bool IsInitialized
        {
            get;
            private set;
        } = false;
        public FanMode FanMode
        {
            get;
            private set;
        } = FanMode.Off;
        public Switch CoolerMode
        {
            get;
            private set;
        } = Switch.Disabled;
        public string SerialNumber
        {
            get;
            private set;
        } = "Unavailable";
        public string CameraModel
        {
            get;
            private set;
        } = "Unknown";
        public DeviceCpabilities Capabilities
        {
            get;
            private set;

        } = default(DeviceCpabilities);
        public CameraProperties Properties
        {
            get;
            private set;
        }
        public SoftwareVersion Software
        {
            get;
            private set;
        }
        public HardwareVersion Hardware
        {
            get;
            private set;
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

        /// <summary>
        /// Determines properties of currently active camera and sets respective Camera.Properties field.
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        private void GetCameraProperties()
        {
            // To call SDK methods, current camera should be active (this.IsActive == true).
            // If it is not the case, then either it was not set active (wrong design of program) or an error happened 
            // while switching control to this camera (and thus behaviour is undefined)
            if (!IsActive)
                throw new AndorSDKException("Camera is not active. Cannnot perform this operation.", null);

            // Stores return codes of ANDOR functions
            uint result = 0;

            // Variables used to retrieve minimum and maximum temperature range (if applicable)
            int min = 0;
            int max = 0;
            
            // Checks if current camera supports temperature queries
            if (Capabilities.GetFunctions.HasFlag(GetFunction.TemperatureRange))
            {
                // Native call to SDK
                result = SDKInit.SDKInstance.GetTemperatureRange(ref min, ref max);
                // If return code is not DRV_SUCCESS = (uint) 20002, throws standard AndorSDKException 
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetTemperatureRange));

                // Check if returned temperatures are valid (min <= max)
                if (min > max)
                    throw new AndorSDKException($"SDK function {nameof(SDKInit.SDKInstance.GetTemperatureRange)} returned invalid temperature range (should be {min} <= {max})", null);
            }

            // Variable used to retrieve horizotal and vertical (maximum?) detector size in pixels (if applicable)
            int h = 0;
            int v = 0;

            // Checks if current camera supports detector size queries
            if (Capabilities.GetFunctions.HasFlag(GetFunction.DetectorSize))
            {
                result = SDKInit.SDKInstance.GetDetector(ref h, ref v);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetDetector));                

                // Checks if detector size is valid (h > 0, v > 0)
                if ((h <= 0) | (v <= 0))
                    throw new AndorSDKException($"SDK function {nameof(SDKInit.SDKInstance.GetDetector)} returned invalid detector size (should be {h} > 0 and {v} > 0)", null);
            }

            // Variable used to store retrieved infromation about presence of private mechanical shutter (if applicable)
            bool shutter = false;

            // private shutters are only present in these cameras (according to documentation)
            if (Capabilities.CameraType == CameraType.iXon | Capabilities.CameraType == CameraType.iXonUltra)
            {
                // Local variable for passing as parameter to native call

                int shutterFlag = 0;
                
                result = SDKInit.SDKInstance.IsInternalMechanicalShutter(ref shutterFlag);
                // Here result can be DRV_NOT_AVAILABLE = (uint) 20992, which means that camera is not iXon.
                // If this code is returned, then something went wrong while camera was initialized and camera type is incorrect
                ThrowIfError(result, nameof(GetCameraProperties));
                
                // Converts int value to bool
                shutter = shutterFlag == 1;

            }



            // Stores the number of different Analogue-Digital Converters onboard a camera
            int ADChannels = 0;
                        
            result = SDKInit.SDKInstance.GetNumberADChannels(ref ADChannels);
            // According to documentation, this call returns always DRV_SUCCESS = (uint) 20002, 
            // so there is no need for error-check
            // However, it is checked that the number of AD-converters is a valid number (> 0)
            if (ADChannels <= 0)
                throw new AndorSDKException($"Function {nameof(SDKInit.SDKInstance.GetNumberADChannels)} returned invalid number of AD converters (returned {ADChannels} should be greater than 0).", null);

            // An array of bit ranges for each available AD converter
            int[] ADsBitRange = new int[ADChannels];

            for (int ADCIndex = 0; ADCIndex < ADsBitRange.Length; ADCIndex++)
            {
                // Local variable that is is passed to SDK function
                int localBitDepth = 0;

                result = SDKInit.SDKInstance.GetBitDepth(ADCIndex, ref localBitDepth);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetBitDepth));

                // If it is successful, asssign obtained bit depth to an element of an array
                ADsBitRange[ADCIndex] = localBitDepth;
            }

            // Stores the number of different amplifiers installed
            int amps = 0;

            result = SDKInit.SDKInstance.GetNumberAmp(ref amps);
            // Again, according to documentation the only return code is DRV_SUCCESS = (uint) 20002, 
            // thus the number of amplifiers should be checked to be in a valid range (> 0)
            if (amps <= 0 )
                throw new AndorSDKException($"Function {nameof(SDKInit.SDKInstance.GetNumberAmp)} returned invalid number of amplifiers (returned {amps} should be greater than 0 and less than 2).", null);

            // Amplifier information array
            Tuple<string, OutputAmplification, float>[] amplifiers = new Tuple<string, OutputAmplification, float>[amps];

            for (int ampIndex = 0; ampIndex < amps; ampIndex++)
            {
                string ampName = "";
                float speed = 0.0f;

                // Retrieves amplifier name
                result = SDKInit.SDKInstance.GetAmpDesc(ampIndex, ref ampName, AmpDescriptorMaxLength);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetAmpDesc));

                // Retrieves maximum horizontal speed
                result = SDKInit.SDKInstance.GetAmpMaxSpeed(ampIndex, ref speed);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetAmpMaxSpeed));

                // Adds obtained values to array
                amplifiers[ampIndex] = new Tuple<string, OutputAmplification, float>(
                    ampName,
                    // In case of Clara 0 corresponds to Conventional (OutputAmplification = 1) and 1 corresponds to ExtendedNIR (OutputAmplification = 2)
                    // Adds 1 to obtained indices in case of Clara camera to store amplifier information properly
                    (OutputAmplification) (ampIndex + (Capabilities.CameraType == CameraType.Clara ? 1 : 0)), 
                    speed);                
            }
            

            // Stores the (maximum) number of different pre-Amp gain settings. Depends on currently selected AD-converter and amplifier
            int preAmpGainMaxNumber = 0;
            
            result = SDKInit.SDKInstance.GetNumberPreAmpGains(ref preAmpGainMaxNumber);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetNumberPreAmpGains));

            // Array of pre amp gain desciptions
            string[] preAmpGainDesc = new string[preAmpGainMaxNumber];


            for (int preAmpIndex = 0; preAmpIndex < preAmpGainMaxNumber; preAmpIndex++)
            {
                string desc = "";

                // Retrieves decription
                result = SDKInit.SDKInstance.GetPreAmpGainText(preAmpIndex, ref desc, PreAmpGainDescriptorMaxLength);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetPreAmpGainText));

                // If success, assigns it to array
                preAmpGainDesc[preAmpIndex] = desc;
            }


            // Stores the number of different vertical speeds available
            int VSSpeedNumber = 0;

            result = SDKInit.SDKInstance.GetNumberVSSpeeds(ref VSSpeedNumber);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetNumberVSSpeeds));

            // Checks if number of different vertical speeds is actually greater than 0
            if (VSSpeedNumber <= 0)
                throw new AndorSDKException($"Function {nameof(SDKInit.SDKInstance.GetNumberVSSpeeds)} returned invalid number of available vertical speeds (returned {VSSpeedNumber} should be greater than 0).", null);


            float[] speedArray = new float[VSSpeedNumber];
            
            for (int speedIndex = 0; speedIndex < VSSpeedNumber; speedIndex++)
            {
                // Variable is passed to the SDK function
                float localSpeed = 0;

                result = SDKInit.SDKInstance.GetVSSpeed(speedIndex, ref localSpeed);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetVSSpeed));

                // Assigns obtained speed to an array of speeds
                speedArray[speedIndex] = localSpeed;
            }

            // Assemples a new CameraProperties object using collected above information
            Properties = new CameraProperties()
            {
                AllowedTemperatures = new TemperatureRange(min, max),
                DetectorSize = new DetectorSize(h, v),
                HasInternalMechanicalShutter = shutter,
                ADConverters = ADsBitRange,
                Amplifiers = amplifiers,
                PreAmpGains = preAmpGainDesc,
                VSSpeeds = speedArray
                
            };
                        
        }

        private void GetSoftwareHardwareVersion()
        {
            // Stores return codes of SDK functions
            uint result = 0;

            // Variables are passed to SDK function and store version information
            uint eprom = 0;
            uint COF = 0;
            uint driverVer = 0;
            uint driverRev = 0;
            uint DllVer = 0;
            uint DllRev = 0;


            result = SDKInit.SDKInstance.GetSoftwareVersion(ref eprom, ref COF, ref driverRev, ref driverVer, ref DllRev, ref DllVer);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetSoftwareVersion));

            // Assigns obtained version information to the class field
            Software = new SoftwareVersion()
            {
                EPROM = new Version((int)eprom, 0),
                COFFile = new Version((int) COF, 0),
                Driver = new Version((int)driverVer, (int)driverRev),
                Dll = new Version((int)DllVer, (int)DllRev)
            };

            // Variables are passed to SDK function and store hardware version information
            uint PCB = 0;
            uint decode = 0;
            uint dummy = 0;
            uint firmwareVer = 0;
            uint firmwareRev = 0;

            result = SDKInit.SDKInstance.GetHardwareVersion(ref PCB, ref decode, ref dummy, ref dummy, ref firmwareVer, ref firmwareRev);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetHardwareVersion));

            // Assigns obtained hardware versions to the class field
            Hardware = new HardwareVersion()
            {
                PCB = new Version((int)PCB, 0),
                Decode = new Version((int)decode, 0),
                CameraFirmware = new Version((int)firmwareVer, (int)firmwareRev)
            };

        }
        
        /// <summary>
        /// Sets current camera active
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        public void SetActive()
        {
            // If camera address is invalid, throws exception
            if (CameraHandle.SDKPtr == 0 )
                throw new AndorSDKException($"Camera has invalid private address of {CameraHandle.SDKPtr}.", new NullReferenceException());

            // Tries to make this camera active
            var result = SDKInit.SDKInstance.SetCurrentCamera(CameraHandle.SDKPtr);
            // If it fails, throw an exception
            ThrowIfError(result, nameof(SDKInit.SDKInstance.SetCurrentCamera));

            // Updates the static field of Camera class to indicate that this camera is now active
            ActiveCamera = this;
            
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

        /// <summary>
        /// Generates an instance of <see cref="AcquisitionSettings"/> that can be used to select proper settings for image
        /// acquisition in the context of this camera
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <returns>A template that can be used to select proper acquisition settings</returns>
        public AcquisitionSettings GetAcquisitionSettingsTemplate()
        {
            if (!IsInitialized)
                throw new AndorSDKException("Camera is not initialized properly.", new NullReferenceException());

            return new AcquisitionSettings(this);
        }


        /// <summary>
        /// Creates a new instance of Camera class to represent a connected Andor device.
        /// Maximum 8 cameras can be controled at the same time
        /// </summary>
        /// <param name="camIndex">The index of a camera (cannot exceed [0, 7] range). Usually limited by <see cref="Camera.GetNumberOfCameras()"/></param>
        public Camera(int camIndex = 0)
        {
            // Stores return codes from SDK functions
            uint result = 0;

            // If cameraIndex is less than 0, it is out of range
            if (camIndex < 0)
                throw new ArgumentException($"Camera index is out of range; Cannot be less than 0 (provided {camIndex}).");
            // If cameraIndex equals to or exceeds the number of available cameras, it is also out of range
            if (camIndex >= GetNumberOfCameras())
                throw new ArgumentException($"Camera index is out of range; Cannot be greater than {GetNumberOfCameras() - 1} (provided {camIndex}).");

            // Stores the handle (SDK private pointer) to the camera. A unique identifier
            int handle = 0;
            result = SDKInit.SDKInstance.GetCameraHandle(camIndex, ref handle);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetCameraHandle));

            // If succede, assigns handle to Camera property
            CameraHandle = new SafeSDKCameraHandle(handle);

            // Sets current camera active
            SetActive();

            // Initializes current camera
            result = SDKInit.SDKInstance.Initialize(".\\");
            ThrowIfError(result, nameof(SDKInit.SDKInstance.Initialize));

            // If succeeded, sets IsInitialized flag to true and adds current camera to the list of initialized cameras
            IsInitialized = true;
            CreatedCameras.Add(this);

            // Gets information about software and hardware used in this system
            GetSoftwareHardwareVersion();

            // Queries capabilities of created camera. Result of this method is used later on to control 
            // available camera settings and regimes
            GetCapabilities();

            // Queries camera properties that contain information about physical regimes of camera
            GetCameraProperties();

            // Gets camera serial number
            GetCameraSerialNumber();

            // And model type
            GetHeadModel();

           
        }

        /// <summary>
        /// A realisation of <see cref="IDisposable.Dispose"/> method.
        /// Frees SDK-related resources
        /// </summary>
        public void Dispose()
        {    
            // If camera has valid SDK pointer and is initialized
            if (IsInitialized && !CameraHandle.IsClosed && !CameraHandle.IsInvalid)
            {
                // Saves currently active camera
                var oldCamera = ActiveCamera;

                // Makes active camera that is going to be disposed (this)
                SetActive();

                // <-------------------------------------->
                // TO DO:
                // Apparently there should be some procedure to ensure that camera has appropriate temperature before disconnecting and turning fan off
                // <-------------------------------------->

                // ShutsDown camera
                CameraHandle.Dispose();

                // If succeeded, removes camera instance from the list of cameras
                CreatedCameras.Remove(this);

                // If there are no other cameras, 
                if (CreatedCameras.Count == 0)
                    ActiveCamera = null;
                // If there are, sets active the one that was active before disposing procedure
                else oldCamera.SetActive();

               
            }

        }

        /// <summary>
        /// Queries the number of currently connected Andor cameras
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <returns>TNumber of detected cameras</returns>
        public static int GetNumberOfCameras()
        {
            // Variable is passed to SDK function
            int cameraCount = -1;
            
            var result = SDKInit.SDKInstance.GetAvailableCameras(ref cameraCount);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetAvailableCameras));   


            return cameraCount;
        }
               
        
        
    }

}
