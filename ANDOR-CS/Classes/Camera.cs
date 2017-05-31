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
using System.Threading;
using System.Threading.Tasks;

using ATMCD64CS;
using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Events;
using ANDOR_CS.Exceptions;

using SDKInit = ANDOR_CS.Classes.AndorSDKInitialization;
using SDK = ATMCD64CS.AndorSDK;

using static ANDOR_CS.Exceptions.AndorSDKException;
using static ANDOR_CS.Exceptions.AcquisitionInProgressException;
using static ANDOR_CS.Exceptions.TemperatureCycleInProgressException;

namespace ANDOR_CS.Classes
{
    /// <summary>
    /// Represents an instance of a Camera device
    /// </summary>
    public class Camera : IDisposable
    {
        private const int AmpDescriptorMaxLength = 21;
        private const int PreAmpGainDescriptorMaxLength = 30;
        private const int StatusCheckTimeOutMS = 100;
        private const int TempCheckTimeOutMS = 5000;

        private static List<Camera> CreatedCameras = new List<Camera>();
        private static Camera ActiveCamera = null;

        /// <summary>
        /// Backend field. Indicates if acquisition is in progress.
        /// Volatile, atomic read/write.
        /// </summary>
        private volatile bool isAcquiring = false;
        /// <summary>
        /// Backend field. Indicates if temperature cycle is in progress.
        /// Volatile, atomic read/write.
        /// </summary>
        private volatile bool isInTemperatureCycle = false;
        /// <summary>
        /// Backend field. Indicates if acquisition is launched from async task.
        /// With async acquisition camera is able to properly fire all events.
        /// Volatile, atomic read/write.
        /// </summary>
        private volatile bool isAsyncAcquisition = false;
        /// <summary>
        /// Backend field. Indicates if temperature cycle is launched from async task.
        /// With async temperature cycle camera is able to properly fire all events.
        /// Volatile, atomic read/write.
        /// </summary>
        private volatile bool isAsyncTemperatureCycle = false;

        private Task TemperatureMonitorWorker = null;
        private CancellationTokenSource TemperatureMonitorCanellationSource = new CancellationTokenSource();

        /// <summary>
        /// Indicates if this camera is currently active
        /// </summary>
        public bool IsActive => ActiveCamera.CameraHandle.SDKPtr == this.CameraHandle.SDKPtr;
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
        public (Version EPROM, Version COFFile, Version Driver, Version Dll) Software
        {
            get;
            private set;
        }
        public (Version PCB, Version Decode, Version CameraFirmware) Hardware
        {
            get;
            private set;
        }


        /// <summary>
        /// Indicates if camera is in process of image acquisition.
        /// </summary>
        public bool IsAcquiring
        {
            get => isAcquiring;
            set => isAcquiring = value;
        }
        /// <summary>
        /// Indicates of camera is in temperature cycle.
        /// </summary>
        public bool IsInTemperatureCycle
        {
            get => isInTemperatureCycle;
            set => isInTemperatureCycle = value;
        }
        /// <summary>
        /// Indicates if acquisition is launched from async method and
        /// camera is able to properly fire all events.
        /// </summary>
        public bool IsAsyncAcquisition
        {
            get => isAsyncAcquisition;
            set => isAsyncAcquisition = value;
        }
        /// <summary>
        /// Indicates if temperature cycle is launched from async method and
        /// camera is able to properly fire all events.
        /// </summary>
        public bool IsAsyncTemperatureCycle
        {
            get => isAsyncTemperatureCycle;
            set => isAsyncTemperatureCycle = value;
        }

        /// <summary>
        /// Handles all events related to acquisition of image process.
        /// </summary>
        /// <param name="sender">A <see cref="Camera"/> type source</param>
        /// <param name="e">Event arguments</param>
        public delegate void AcquisitionStatusEventHandler(object sender, AcquisitionStatusEventArgs e);
        /// <summary>
        /// Handles all events related to temperature cycle.
        /// </summary>
        /// <param name="sender">A <see cref="Camera"/> type source</param>
        /// <param name="e">Event arguments</param>
        public delegate void TemperatureStatusEventHandler(object sender, TemperatureStatusEventArgs e);

        /// <summary>
        /// Fires when acquisition is started.
        /// </summary>
        public event AcquisitionStatusEventHandler AcquisitionStarted;
        /// <summary>
        /// Fires when acquisition is finished.
        /// </summary>
        public event AcquisitionStatusEventHandler AcquisitionFinished;
        /// <summary>
        /// Fires when acquisition status is asynchronously checked
        /// </summary>
        public event AcquisitionStatusEventHandler AcquisitionStatusChecked;
        /// <summary>
        /// Fires when an exception is thrown in a background asynchronous task
        /// </summary>
        public event AcquisitionStatusEventHandler AcquisitionErrorReturned;
        /// <summary>
        /// Fires when acquisition is aborted manually
        /// </summary>
        public event AcquisitionStatusEventHandler AcquisitionAborted;

        /// <summary>
        /// Fires when backround task acsynchronously checks temperature
        /// </summary>
        public event TemperatureStatusEventHandler TemperatureStatusChecked;

        /// <summary>
        /// Fires when temperature is asynchronously checked during cooling process
        /// </summary>
        public event TemperatureStatusEventHandler CoolingTemperatureChecked;
        /// <summary>
        /// Firs when cooling is started
        /// </summary>
        public event TemperatureStatusEventHandler CoolingStarted;
        /// <summary>
        /// Fires when cooling is finished
        /// </summary>
        public event TemperatureStatusEventHandler CoolingFinished;
        /// <summary>
        /// Fires when cooling is aborted manually
        /// </summary>
        public event TemperatureStatusEventHandler CoolingAborted;
        /// <summary>
        /// Fires when an exception is thrown in a background asynchronous task.
        /// </summary>
        public event TemperatureStatusEventHandler CoolingErrorReturned;

        private void GetCapabilities()
        {
            ThrowIfAcquiring(this);

            SDK.AndorCapabilities caps = default(SDK.AndorCapabilities);
            caps.ulSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(caps);

            var result = SDKInit.SDKInstance.GetCapabilities(ref caps);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetCapabilities));
            Capabilities = new DeviceCpabilities(caps);
        }
        private void GetCameraSerialNumber()
        {
            ThrowIfAcquiring(this);

            int number = -1;
            var result = SDKInit.SDKInstance.GetCameraSerialNumber(ref number);

            if (result == SDK.DRV_SUCCESS)
                SerialNumber = number.ToString();


        }
        private void GetHeadModel()
        {
            ThrowIfAcquiring(this);

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
            // Checks if acquisition is in progress; throws exception
            ThrowIfAcquiring(this);

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
            (string Name, OutputAmplification Amplifier, float MaxSpeed)[] amplifiers = new (string Name, OutputAmplification Amplifier, float MaxSpeed)[amps];

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
                amplifiers[ampIndex] = (
                    Name: ampName,
                    // In case of Clara 0 corresponds to Conventional (OutputAmplification = 1) and 1 corresponds to ExtendedNIR (OutputAmplification = 2)
                    // Adds 1 to obtained indices in case of Clara camera to store amplifier information properly
                    Amplifier: (OutputAmplification) (ampIndex + (Capabilities.CameraType == CameraType.Clara ? 1 : 0)), 
                    MaxSpeed: speed);                
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
                AllowedTemperatures = (Minimum: min, Maximum: max),
                DetectorSize = new Size(h, v),
                HasInternalMechanicalShutter = shutter,
                ADConverters = ADsBitRange,
                Amplifiers = amplifiers,
                PreAmpGains = preAmpGainDesc,
                VSSpeeds = speedArray
                
            };
                        
        }

        private void GetSoftwareHardwareVersion()
        {
            // Checks if acquisition is in progress; throws exception
            ThrowIfAcquiring(this);

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
            Software = (
                EPROM: new Version((int)eprom, 0),
                COFFile: new Version((int) COF, 0),
                Driver: new Version((int)driverVer, (int)driverRev),
                Dll: new Version((int)DllVer, (int)DllRev)
            );

            // Variables are passed to SDK function and store hardware version information
            uint PCB = 0;
            uint decode = 0;
            uint dummy = 0;
            uint firmwareVer = 0;
            uint firmwareRev = 0;

            result = SDKInit.SDKInstance.GetHardwareVersion(ref PCB, ref decode, ref dummy, ref dummy, ref firmwareVer, ref firmwareRev);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetHardwareVersion));

            // Assigns obtained hardware versions to the class field
            Hardware = (
                PCB: new Version((int)PCB, 0),
                Decode: new Version((int)decode, 0),
                CameraFirmware: new Version((int)firmwareVer, (int)firmwareRev)
            );

        }

        private void TemperatureMonitorCycler(CancellationToken token, int delay)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    return;

                (var status, var temp) = GetCurrentTemperature();

                OnTemperatureStatusChecked(new TemperatureStatusEventArgs(status, temp));

                Task.Delay(delay).Wait();
            }

        }

        /// <summary>
        /// Fires <see cref="AcquisitionStarted"/> event.
        /// </summary>
        /// <param name="e">Status of camera at the beginning of acquisition</param>
        protected virtual void OnAcquisitionStarted(AcquisitionStatusEventArgs e) => AcquisitionStarted?.Invoke(this, e);
        /// <summary>
        /// Fires <see cref="AcquisitionStatusChecked"/> event.
        /// </summary>
        /// <param name="e">Status of camera during acquisition</param>
        protected virtual void OnAcquisitionStatusChecked(AcquisitionStatusEventArgs e) => AcquisitionStatusChecked?.Invoke(this, e);
        /// <summary>
        /// Fires <see cref="AcquisitionFinished"/> event.
        /// </summary>
        /// <param name="e">Status of camera at the end of acquisition</param>
        protected virtual void OnAcquisitionFinished(AcquisitionStatusEventArgs e) => AcquisitionFinished?.Invoke(this, e);
        /// <summary>
        /// Fires <see cref="AcquisitionErrorReturned"/> event.
        /// </summary>
        /// <param name="e">Status of camera when exception was thrown</param>
        protected virtual void OnAcquisitionErrorReturned(AcquisitionStatusEventArgs e) => AcquisitionErrorReturned?.Invoke(this, e);
        /// <summary>
        /// Fires <see cref="AcquisitionAborted"/> event.
        /// </summary>
        /// <param name="e">Status of camera when abortion happeed</param>
        protected virtual void OnAcquisitionAborted(AcquisitionStatusEventArgs e) => AcquisitionAborted?.Invoke(this, e);
        protected virtual void OnTemperatureStatusChecked(TemperatureStatusEventArgs e) => TemperatureStatusChecked?.Invoke(this, e);
        protected virtual void OnCoolingTemperatureChecked(TemperatureStatusEventArgs e) => CoolingTemperatureChecked?.Invoke(this, e);
        protected virtual void OnCoolingStarted(TemperatureStatusEventArgs e) => CoolingStarted?.Invoke(this, e);
        protected virtual void OnCoolingFinished(TemperatureStatusEventArgs e) => CoolingFinished?.Invoke(this, e);
        protected virtual void OnCoolingAborted(TemperatureStatusEventArgs e) => CoolingAborted?.Invoke(this, e);
        protected virtual void OnCoolingErrorReturned(TemperatureStatusEventArgs e) => CoolingErrorReturned?.Invoke(this, e);


        /// <summary>
        /// Sets current camera active
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        public void SetActive()
        {
            // Checks if acquisition is in progress; throws exception
            ThrowIfAcquiring(this);

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

        /// <summary>
        /// Gets current status of the camera
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <returns>Camera status</returns>
        public CameraStatus GetStatus()
        {
            // Used to query status of the camera
            int status = 0;

            // Queries status, throws exception if error happened
            ThrowIfError(SDKInit.SDKInstance.GetStatus(ref status), nameof(SDKInit.SDKInstance.GetStatus));

            // Converts status to enum
            var camStatus = (CameraStatus)status;

            // If acquisition is started without background task, camera instance 
            // is in acquisition state, but actual camera returns status that is different
            // from "Acquiring", then updates status, acknowledging end of acquisition 
            // end firing AcquisitioFinished event.
            // Without this call there is no way to synchronously update instance of camera class
            // when real acquisition on camera finished.
            if (IsAcquiring &&
                !IsAsyncAcquisition &&
                camStatus != CameraStatus.Acquiring)
            {
                IsAcquiring = false;
                OnAcquisitionFinished(new AcquisitionStatusEventArgs(camStatus, false));
            }

            return camStatus;
        }

        public void FanControl(FanMode mode)
        {
            // Checks if acquisition is in progress; throws exception
            ThrowIfAcquiring(this);

            if (!Capabilities.Features.HasFlag(SDKFeatures.FanControl))
                throw new NotSupportedException("Camera does not support fan controls.");

            if(mode == FanMode.LowSpeed && 
                !Capabilities.Features.HasFlag(SDKFeatures.LowFanMode))
                throw new NotSupportedException("Camera does not support low-speed fan mode.");

            var result = SDKInit.SDKInstance.SetFanMode((int)mode);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.SetFanMode));

            FanMode = mode;
        }

        public void CoolerControl(Switch mode)
        {
            // Checks if acquisition is in progress; throws exception
            //ThrowIfAcquiring(this);

            if (!Capabilities.SetFunctions.HasFlag(SetFunction.Temperature))
                throw new AndorSDKException("Camera does not support cooler controls.", new ArgumentException());

            if (IsInTemperatureCycle &&
                IsAsyncTemperatureCycle &&
                mode == Switch.Disabled)
                throw new TaskCanceledException("Camera is in process of async cooling. Cannot control cooler synchronously.");

            uint result = SDK.DRV_SUCCESS;

            
            if (mode == Switch.Enabled)
                result = SDKInit.SDKInstance.CoolerON();
            else if (mode == Switch.Disabled)
                result = SDKInit.SDKInstance.CoolerOFF();

            ThrowIfError(result, nameof(SDKInit.SDKInstance.CoolerON) + " or " + nameof(SDKInit.SDKInstance.CoolerOFF));
            CoolerMode = mode;
                
            var status = GetCurrentTemperature();

            if (mode == Switch.Enabled)
                OnCoolingStarted(new TemperatureStatusEventArgs(status.Status, status.Temperature));
            else
                OnCoolingFinished(new TemperatureStatusEventArgs(status.Status, status.Temperature));
            

        }

        public void SetTemperature(int temperature)
        {
            // Checks if acquisition is in progress; throws exception
            ThrowIfAcquiring(this);

            if (!Capabilities.SetFunctions.HasFlag(SetFunction.Temperature))
                throw new AndorSDKException("Camera does not support temperature controls.", new ArgumentException());

            if (Properties.AllowedTemperatures.Minimum >= Properties.AllowedTemperatures.Maximum)
                throw new AndorSDKException("Valid temperature range was not received from camera.", new ArgumentNullException());

            if (temperature > Properties.AllowedTemperatures.Maximum ||
                temperature < Properties.AllowedTemperatures.Minimum )
                throw new ArgumentOutOfRangeException($"Provided temperature ({temperature}) is out of valid range " +
                    $"({Properties.AllowedTemperatures.Minimum }, " +
                     $"{Properties.AllowedTemperatures.Maximum }).");
            
            var result = SDKInit.SDKInstance.SetTemperature(temperature);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.SetTemperature));

        }

        public (TemperatureStatus Status, float Temperature) GetCurrentTemperature()
        {
            // Checks if acquisition is in progress; throws exception
            ThrowIfAcquiring(this);

            if (!Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                throw new AndorSDKException("Camera does not support temperature inquires.", new ArgumentException());

            float temp = float.NaN;

            var result = SDKInit.SDKInstance.GetTemperatureF(ref temp);
            switch (result)
            {
                case SDK.DRV_ACQUIRING:
                    throw new AcquisitionInProgressException("Camera is in acquisition mode.");
                case SDK.DRV_NOT_INITIALIZED:
                    throw new AndorSDKException("Camera is not initialized.", result);
                case SDK.DRV_ERROR_ACK:
                    throw new AndorSDKException("Communication error.", result);

            }

            var status = (TemperatureStatus)result;

            if (!IsAsyncTemperatureCycle && 
                IsInTemperatureCycle &&
                status != TemperatureStatus.NotReached)
                IsInTemperatureCycle = false;

            return (Status: status, Temperature: temp);

        }

        /// <summary>
        /// Starts acquisition of the image. Does not block current thread.
        /// To monitor acquisition progress, use <see cref="GetStatus"/>.
        /// Fires <see cref="OnAcquisitionStarted(AcquisitionStatusEventArgs)"/> 
        /// with <see cref="AcquisitionStatusEventArgs.IsAsync"/> = false.
        /// NOTE: this method is not recommended. Consider using async version
        /// <see cref="StartAcquistionAsync(CancellationToken, int)"/>.
        /// Async version allows <see cref="Camera"/> to properly monitor acquisition progress.
        /// </summary>
        /// <exception cref="AcquisitionInProgressException"/>
        /// <exception cref="AndorSDKException"/>
        public void StartAcquisition()
        {
            // If acquisition is already in progress, throw exception
            ThrowIfAcquiring(this);

            // Starts acquisition
            ThrowIfError(SDKInit.SDKInstance.StartAcquisition(), nameof(SDKInit.SDKInstance.StartAcquisition));

            // Fires event
            OnAcquisitionStarted(new AcquisitionStatusEventArgs(GetStatus(), IsAsyncAcquisition));

            // Marks camera as in process of acquiring
            IsAcquiring = true;
        }

        /// <summary>
        /// A synchronous way to manually abort acquisition.
        /// NOTE: if called while async acquisition is in progress, throws
        /// <see cref="TaskCanceledException"/>. To cancel async acquisition, use 
        /// <see cref="CancellationToken"/>.
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="TaskCanceledException"/>
        public void AbortAcquisition()
        {
            // If there is no acquisition, throws exception
            if (!IsAcquiring)
                throw new AndorSDKException("Acquisition abort attemted while there is no acquisition in proress.", null);

            if (isAsyncAcquisition)
                throw new TaskCanceledException("Camera is in process of async acquisition. Cannot call synchronous abort.");
            
            // Tries to abort acquisition
            ThrowIfError(SDKInit.SDKInstance.AbortAcquisition(), nameof(SDKInit.SDKInstance.AbortAcquisition));

            // Fires AcquisitionAborted event
            OnAcquisitionAborted(new AcquisitionStatusEventArgs(GetStatus(), IsAsyncAcquisition));

            // Marks the end of acquisition
            IsAcquiring = false;
        }

        public void TemperatureMonitor(Switch mode, int timeout = TempCheckTimeOutMS)
        {
            if (mode == Switch.Enabled)
            {
                if (!Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                    throw new NotSupportedException("Camera dose not support temperature queries.");

                if (TemperatureMonitorWorker == null ||
                    TemperatureMonitorWorker.Status == TaskStatus.Canceled ||
                    TemperatureMonitorWorker.Status == TaskStatus.RanToCompletion ||
                    TemperatureMonitorWorker.Status == TaskStatus.Faulted)
                    TemperatureMonitorWorker = Task.Factory.StartNew(
                        () => TemperatureMonitorCycler(TemperatureMonitorCanellationSource.Token, timeout),
                        TemperatureMonitorCanellationSource.Token);

                if (TemperatureMonitorWorker.Status == TaskStatus.Created)
                    TemperatureMonitorWorker.Start();
                
            }
            if (mode == Switch.Disabled)
            {
                if (TemperatureMonitorWorker?.Status == TaskStatus.Running ||
                    TemperatureMonitorWorker?.Status == TaskStatus.WaitingForActivation ||
                    TemperatureMonitorWorker?.Status == TaskStatus.WaitingToRun)
                    TemperatureMonitorCanellationSource.Cancel();
            }
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
        /// Starts process of acquisition asynchronously.
        /// This is the preferred way to acquire images from camera.
        /// To run synchronously, call i.e. <see cref="Task.Wait()"/> on the returned task.
        /// </summary>
        /// <param name="token">Cancellation token that can be used to abort process.</param>
        /// <param name="timeout">Time interval in ms between subsequent camera status queries.</param>
        /// <exception cref="AcquisitionInProgressException"/>
        /// <exception cref="AndorSDKException"/>
        /// <returns>Task that can be queried for execution status.</returns>
        public async Task StartAcquistionAsync(CancellationToken token, int timeout = StatusCheckTimeOutMS)
        {
            // Checks if acquisition is in progress; throws exception
            ThrowIfAcquiring(this);
            
            // If camera is not idle, cannot start acquisition
            if (GetStatus() != CameraStatus.Idle)
                throw new AndorSDKException("Camera is not in the idle mode.", null);

            CameraStatus status = CameraStatus.Acquiring;

            //await Task.Run(() =>
           // {
                try
                {
                    // Marks acuisition asynchronous
                    IsAsyncAcquisition = true;

                    // Start scquisition
                    StartAcquisition();
                                    
                    status = GetStatus();
                    
                    // While status is acquiring
                    while ((status = GetStatus()) == CameraStatus.Acquiring)
                    {
                        // Fires AcquisitionStatusChecked event
                        OnAcquisitionStatusChecked(new AcquisitionStatusEventArgs(status, true));
                        // If task is aborted
                        if (token.IsCancellationRequested)
                        {
                            // Aborts
                            AbortAcquisition();
                            // Exits wait loop
                            break;
                        }
                       
                        // Waits for specified amount of time before checking status again
                        await Task.Delay(StatusCheckTimeOutMS);//.Wait();

                    }

                    // If after end of acquisition camera status is not idle, throws exception
                    if (!token.IsCancellationRequested && status != CameraStatus.Idle)
                        throw new AndorSDKException($"Acquisiotn finished with non-Idle status ({status}).", null);

                    
                }
                // If there were exceptions during status checking loop
                catch (Exception e)
                {
                    // Fire event
                    OnAcquisitionErrorReturned(new AcquisitionStatusEventArgs(status, true));
                    // re-throw received exception
                    throw;
                }
                // Ensures that acquisition is properly finished and event is fired
                finally
                {
                    IsAcquiring = false;
                    IsAsyncAcquisition = false;
                    OnAcquisitionFinished(new AcquisitionStatusEventArgs(GetStatus(), true));
                    
                }
            //});

        }


        public async Task StartCoolingCycleAsync(
            int targetTemperature, 
            FanMode desiredMode,
            CancellationToken token,
            int timeout = TempCheckTimeOutMS
            )
        {
            // Checks if acquisition is in progress; throws exception
            //ThrowIfAcquiring(this);

            if (IsInTemperatureCycle)
                throw new TemperatureCycleInProgressException("Cooling is already in process.");

            SetTemperature(targetTemperature);

            var oldFanMode = FanMode;

            try
            {
                FanControl(desiredMode);
            }
            catch (NotSupportedException e)
            {
                if (desiredMode == FanMode.LowSpeed && Capabilities.Features.HasFlag(SDKFeatures.FanControl))
                    FanControl(FanMode.FullSpeed);
                else
                    throw;
            }
            
            (var status, var temp) = GetCurrentTemperature();

            try
            {
                IsInTemperatureCycle = true;
                IsAsyncTemperatureCycle = true;
                CoolerControl(Switch.Enabled);

                while (((status, temp) = GetCurrentTemperature()).Item1.HasFlag(TemperatureStatus.NotReached))
                    {
                        if (token.IsCancellationRequested)
                        {
                            OnCoolingAborted(new TemperatureStatusEventArgs(status, temp));
                            break;
                        }

                        OnCoolingTemperatureChecked(new TemperatureStatusEventArgs(status, temp));

                        await Task.Delay(timeout);
                    }

                
            }
            catch (Exception e)
            {
                OnCoolingErrorReturned(new TemperatureStatusEventArgs(status, temp));
                throw;
            }
            finally
            {
               

                if (Capabilities.Features.HasFlag(SDKFeatures.FanControl))
                    FanControl(oldFanMode);

                IsInTemperatureCycle = false;
                IsAsyncTemperatureCycle = true;
                CoolerControl(Switch.Disabled);
                
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
