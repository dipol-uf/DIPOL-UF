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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Events;
using ANDOR_CS.Exceptions;

using SDKInit = ANDOR_CS.Classes.AndorSDKInitialization;
using SDK = ATMCD64CS.AndorSDK;
using ICameraControl = ANDOR_CS.Interfaces.ICameraControl;

using static ANDOR_CS.Exceptions.AndorSDKException;
using static ANDOR_CS.Exceptions.AcquisitionInProgressException;

using static ANDOR_CS.Classes.AndorSDKInitialization;

namespace ANDOR_CS.Classes
{
    /// <summary>
    /// Represents an instance of a Camera device
    /// </summary>
    public class Camera : ICameraControl
    {
        private const int AmpDescriptorMaxLength = 21;
        private const int PreAmpGainDescriptorMaxLength = 30;
        private const int StatusCheckTimeOutMS = 100;
        private const int TempCheckTimeOutMS = 5000;

        private static ConcurrentDictionary<int, ICameraControl> CreatedCameras 
            = new ConcurrentDictionary<int, ICameraControl>();
        private static Camera ActiveCamera = null;

        private static volatile SemaphoreSlim ActivityLocker = new SemaphoreSlim(1, 1);

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
        private CancellationTokenSource TemperatureMonitorCanellationSource 
            = new CancellationTokenSource();

        private volatile int LockDepth = 0;

        /// <summary>
        /// Read-only collection of all local cameras in use.
        /// </summary>
        public IReadOnlyDictionary<int, ICameraControl> CamerasInUse 
            => CreatedCameras as IReadOnlyDictionary<int, ICameraControl>;

        /// <summary>
        /// Indicates if this camera is currently active
        /// </summary>
        public bool IsActive
        {
            get
            {
                try
                {
                    if(LockDepth == 0)
                        ActivityLocker.Wait();

                    LockDepth++;
                    return Call(SDKInstance.GetCurrentCamera, out int handle) == SDK.DRV_SUCCESS ? handle == CameraHandle.SDKPtr : false;
                }
                finally
                {
                    ReleaseLock();
                }
            }
        }
                           
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
        public (
            ShutterMode Internal, 
            ShutterMode? External, 
            TTLShutterSignal Type,
            int OpenTime,
            int CloseTime
            ) Shutter
        {
            get;
            private set;
        }
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
        /// Andor SDK unique index of camera; passed to constructor
        /// </summary>
        public int CameraIndex
        {
            get;
            private set;
        } = -1;

        
        /// <summary>
        /// Curently set acquisition regime
        /// </summary>
        public AcquisitionSettings CurrentSettings
        {
            get;
            internal set;
        } = null;

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
        [Obsolete("Obsolete property", true)]
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
        [Obsolete("Obsolete property", true)]
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
        [Obsolete("Obsolete event", true)]
        public event TemperatureStatusEventHandler CoolingTemperatureChecked;
        /// <summary>
        /// Firs when cooling is started
        /// </summary>
        [Obsolete("Obsolete event", true)]
        public event TemperatureStatusEventHandler CoolingStarted;
        /// <summary>
        /// Fires when cooling is finished
        /// </summary>
        [Obsolete("Obsolete event", true)]
        public event TemperatureStatusEventHandler CoolingFinished;
        /// <summary>
        /// Fires when cooling is aborted manually
        /// </summary>
        [Obsolete("Obsolete event", true)]
        public event TemperatureStatusEventHandler CoolingAborted;
        /// <summary>
        /// Fires when an exception is thrown in a background asynchronous task.
        /// </summary>
        [Obsolete("Obsolete event", true)]
        public event TemperatureStatusEventHandler CoolingErrorReturned;

        /// <summary>
        /// Retrieves camera's capabilities
        /// </summary>
        private void GetCapabilities()
        {
            try
            {
                SetActiveAndLock();
            
                // Throws if camera is acquiring
                ThrowIfAcquiring(this);

                // Holds information about camera's capabilities
                SDK.AndorCapabilities caps = default(SDK.AndorCapabilities);

                // Unmanaged structure size
                caps.ulSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(caps);

                // Using manual locker controls to call SDk function task-safely

                LockManually();
                var result = SDKInstance.GetCapabilities(ref caps);
                ReleaseManually();

                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetCapabilities));

                // Assigns current camera's property
                Capabilities = new DeviceCpabilities(caps);
            }
            finally
            {
                ReleaseLock();
            }
        }

        /// <summary>
        /// Retrieves camera's serial number
        /// </summary>
        private void GetCameraSerialNumber()
        {
            try
            {
                SetActiveAndLock();
            
                // Checks if acquisition is in progress
                ThrowIfAcquiring(this);

                // Retrieves number
                var result = Call(SDKInstance.GetCameraSerialNumber, out int number);

                if (result == SDK.DRV_SUCCESS)
                    SerialNumber = number.ToString();
            }
            finally
            {
                ReleaseLock();
            }

        }

        /// <summary>
        /// Retrieves camera's model
        /// </summary>
        private void GetHeadModel()
        {
            try
            {
                SetActiveAndLock();

            
                // Checks if acquisition is in process
                ThrowIfAcquiring(this);

                // Retrieves model
                var result = Call(SDKInstance.GetHeadModel, out string model);


                if (result == SDK.DRV_SUCCESS)
                    CameraModel = model;
            }
            finally
            {
                ReleaseLock();
            }
        }

        /// <summary>
        /// Determines properties of currently active camera and sets respective Camera.Properties field.
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="AcquisitionInProgressException"/>
        private void GetCameraProperties()
        {
            try
            {
                SetActiveAndLock();

            
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
                    // Uses manual synchronization calls
                    LockManually();
                    result = SDKInstance.GetTemperatureRange(ref min, ref max);
                    ReleaseManually();

                    // If return code is not DRV_SUCCESS = (uint) 20002, throws standard AndorSDKException 
                    ThrowIfError(result, nameof(SDKInstance.GetTemperatureRange));

                    // Check if returned temperatures are valid (min <= max)
                    if (min > max)
                        throw new AndorSDKException($"SDK function {nameof(SDKInstance.GetTemperatureRange)} returned invalid temperature range (should be {min} <= {max})", null);
                }

                // Variable used to retrieve horizotal and vertical (maximum?) detector size in pixels (if applicable)
                int h = 0;
                int v = 0;

                // Checks if current camera supports detector size queries
                if (Capabilities.GetFunctions.HasFlag(GetFunction.DetectorSize))
                {
                    // Manual synchronization
                    LockManually();
                    result = SDKInstance.GetDetector(ref h, ref v);
                    ReleaseManually();

                    ThrowIfError(result, nameof(SDKInstance.GetDetector));

                    // Checks if detector size is valid (h > 0, v > 0)
                    if ((h <= 0) | (v <= 0))
                        throw new AndorSDKException($"SDK function {nameof(SDKInstance.GetDetector)} returned invalid detector size (should be {h} > 0 and {v} > 0)", null);
                }

                // Variable used to store retrieved infromation about presence of private mechanical shutter (if applicable)
                bool shutter = false;

                // private shutters are only present in these cameras (according to documentation)
                if (Capabilities.CameraType == CameraType.iXon | Capabilities.CameraType == CameraType.iXonUltra)
                {

                    // Task-synchronized call to SDK method
                    result = Call(SDKInstance.IsInternalMechanicalShutter, out int shutterFlag);
                    // Here result can be DRV_NOT_AVAILABLE = (uint) 20992, which means that camera is not iXon.
                    // If this code is returned, then something went wrong while camera was initialized and camera type is incorrect
                    ThrowIfError(result, nameof(GetCameraProperties));

                    // Converts int value to bool
                    shutter = shutterFlag == 1;

                }



                // Task-synchronized call to SDK method

                result = Call(SDKInstance.GetNumberADChannels, out int ADChannels);
                // According to documentation, this call returns always DRV_SUCCESS = (uint) 20002, 
                // so there is no need for error-check
                // However, it is checked that the number of AD-converters is a valid number (> 0)
                if (ADChannels <= 0)
                    throw new AndorSDKException($"Function {nameof(SDKInstance.GetNumberADChannels)} returned invalid number of AD converters (returned {ADChannels} should be greater than 0).", null);

                // An array of bit ranges for each available AD converter
                int[] ADsBitRange = new int[ADChannels];

                for (int ADCIndex = 0; ADCIndex < ADsBitRange.Length; ADCIndex++)
                {

                    result = Call(SDKInstance.GetBitDepth, ADCIndex, out int localBitDepth);
                    ThrowIfError(result, nameof(SDKInit.SDKInstance.GetBitDepth));

                    // If it is successful, asssign obtained bit depth to an element of an array
                    ADsBitRange[ADCIndex] = localBitDepth;
                }


                result = Call(SDKInstance.GetNumberAmp, out int amps);
                // Again, according to documentation the only return code is DRV_SUCCESS = (uint) 20002, 
                // thus the number of amplifiers should be checked to be in a valid range (> 0)
                if (amps <= 0)
                    throw new AndorSDKException($"Function {nameof(SDKInstance.GetNumberAmp)} returned invalid number of amplifiers (returned {amps} should be greater than 0 and less than 2).", null);

                // Amplifier information array
                (string Name, OutputAmplification Amplifier, float MaxSpeed)[] amplifiers = new(string Name, OutputAmplification Amplifier, float MaxSpeed)[amps];

                for (int ampIndex = 0; ampIndex < amps; ampIndex++)
                {
                    string ampName = "";

                    // Manual synchronization
                    LockManually();
                    result = SDKInstance.GetAmpDesc(ampIndex, ref ampName, AmpDescriptorMaxLength);
                    ReleaseManually();

                    ThrowIfError(result, nameof(SDKInstance.GetAmpDesc));

                    // Retrieves maximum horizontal speed
                    result = Call(SDKInstance.GetAmpMaxSpeed, ampIndex, out float speed);
                    ThrowIfError(result, nameof(SDKInstance.GetAmpMaxSpeed));

                    // Adds obtained values to array
                    amplifiers[ampIndex] = (
                        Name: ampName,
                        // In case of Clara 0 corresponds to Conventional (OutputAmplification = 1) and 1 corresponds to ExtendedNIR (OutputAmplification = 2)
                        // Adds 1 to obtained indices in case of Clara camera to store amplifier information properly
                        Amplifier: (OutputAmplification)(ampIndex + (Capabilities.CameraType == CameraType.Clara ? 1 : 0)),
                        MaxSpeed: speed);
                }


                // Stores the (maximum) number of different pre-Amp gain settings. Depends on currently selected AD-converter and amplifier

                result = Call(SDKInstance.GetNumberPreAmpGains, out int preAmpGainMaxNumber);
                ThrowIfError(result, nameof(SDKInstance.GetNumberPreAmpGains));

                // Array of pre amp gain desciptions
                string[] preAmpGainDesc = new string[preAmpGainMaxNumber];


                for (int preAmpIndex = 0; preAmpIndex < preAmpGainMaxNumber; preAmpIndex++)
                {
                    string desc = "";

                    // Retrieves decription
                    // Manual synchronization
                    LockManually();
                    result = SDKInstance.GetPreAmpGainText(preAmpIndex, ref desc, PreAmpGainDescriptorMaxLength);
                    ReleaseManually();

                    ThrowIfError(result, nameof(SDKInstance.GetPreAmpGainText));

                    // If success, adds it to array
                    preAmpGainDesc[preAmpIndex] = desc;
                }



                result = Call(SDKInstance.GetNumberVSSpeeds, out int VSSpeedNumber);
                ThrowIfError(result, nameof(SDKInstance.GetNumberVSSpeeds));

                // Checks if number of different vertical speeds is actually greater than 0
                if (VSSpeedNumber <= 0)
                    throw new AndorSDKException($"Function {nameof(SDKInstance.GetNumberVSSpeeds)} returned invalid number of available vertical speeds (returned {VSSpeedNumber} should be greater than 0).", null);


                float[] speedArray = new float[VSSpeedNumber];

                for (int speedIndex = 0; speedIndex < VSSpeedNumber; speedIndex++)
                {

                    result = Call(SDKInstance.GetVSSpeed, speedIndex, out float localSpeed);
                    ThrowIfError(result, nameof(SDKInstance.GetVSSpeed));

                    // Assigns obtained speed to an array of speeds
                    speedArray[speedIndex] = localSpeed;
                }

                (int Low, int High) = (0, 0);

                if (Capabilities.GetFunctions.HasFlag(GetFunction.EMCCDGain))
                {
                    LockManually();
                    SDKInstance.GetEMGainRange(ref Low, ref High);
                    ReleaseManually();
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
                    VSSpeeds = speedArray,
                    EMCCDGainRange = (Low, High)                    
                };
            }
            finally
            {
                ReleaseLock();
            }
                        
        }

        /// <summary>
        /// Retrieves software/hardware versions
        /// </summary>
        /// <exception cref="AcquisitionInProgressException"/>
        private void GetSoftwareHardwareVersion()
        {
            try
            {
                SetActiveAndLock();

            

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

                // Manual synchronization
                LockManually();
                result = SDKInstance.GetSoftwareVersion(ref eprom, ref COF, ref driverRev, ref driverVer, ref DllRev, ref DllVer);
                ReleaseManually();

                ThrowIfError(result, nameof(SDKInstance.GetSoftwareVersion));

                // Assigns obtained version information to the class field
                Software = (
                    EPROM: new Version((int)eprom, 0),
                    COFFile: new Version((int)COF, 0),
                    Driver: new Version((int)driverVer, (int)driverRev),
                    Dll: new Version((int)DllVer, (int)DllRev)
                );

                // Variables are passed to SDK function and store hardware version information
                uint PCB = 0;
                uint decode = 0;
                uint dummy = 0;
                uint firmwareVer = 0;
                uint firmwareRev = 0;

                // Manual synchronization
                LockManually();
                result = SDKInstance.GetHardwareVersion(ref PCB, ref decode, ref dummy, ref dummy, ref firmwareVer, ref firmwareRev);
                ReleaseManually();

                ThrowIfError(result, nameof(SDKInstance.GetHardwareVersion));

                // Assigns obtained hardware versions to the class field
                Hardware = (
                    PCB: new Version((int)PCB, 0),
                    Decode: new Version((int)decode, 0),
                    CameraFirmware: new Version((int)firmwareVer, (int)firmwareRev)
                );
            }
            finally
            {
                ReleaseLock();
            }

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
        /// Sets curernt camera active and locks it, preventing any further switches
        /// </summary>
        internal void SetActiveAndLock()
        {
            if (LockDepth == 0)
            {
                ActivityLocker.Wait();

                SetActive();
            }

            LockDepth++;
        }

        /// <summary>
        /// Releases current camera, allowing to switch to another device
        /// </summary>
        internal void ReleaseLock()
        {
            if(LockDepth == 1)
                ActivityLocker.Release();
            LockDepth--;
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
            Call(SDKInstance.GetCurrentCamera, out int handle);

            //if (!IsActive)
            {
                // If camera address is invalid, throws exception
                if (CameraHandle.SDKPtr == 0)
                    throw new AndorSDKException($"Camera has invalid private address of {CameraHandle.SDKPtr}.", new NullReferenceException());

                // Tries to make this camera active
                var result = Call(SDKInstance.SetCurrentCamera, CameraHandle.SDKPtr);
                // If it fails, throw an exception
                ThrowIfError(result, nameof(SDKInstance.SetCurrentCamera));

                // Updates the static field of Camera class to indicate that this camera is now active
                //ActiveCamera = this;
            }
        }

        /// <summary>
        /// Gets current status of the camera
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <returns>Camera status</returns>
        public CameraStatus GetStatus()
        {
            try
            {
                SetActiveAndLock();

                // Queries status, throws exception if error happened
                ThrowIfError(Call(SDKInstance.GetStatus, out int status), nameof(SDKInstance.GetStatus));

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
            finally
            {
                ReleaseLock();
            }
           
        }

        /// <summary>
        /// Sets fan mode
        /// </summary>
        /// <param name="mode">Desired fan mode</param>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="AcquisitionInProgressException"/>
        public void FanControl(FanMode mode)
        {
            try
            {
                SetActiveAndLock();

                // Checks if acquisition is in progress; throws exception
                ThrowIfAcquiring(this);

                // Checks if Fan Control is supported
                if (!Capabilities.Features.HasFlag(SDKFeatures.FanControl))
                    throw new NotSupportedException("Camera does not support fan controls.");

                // Checks if intermediate mode is supported
                if (mode == FanMode.LowSpeed &&
                    !Capabilities.Features.HasFlag(SDKFeatures.LowFanMode))
                    throw new NotSupportedException("Camera does not support low-speed fan mode.");


                var result = Call(SDKInstance.SetFanMode, (int)mode);

                ThrowIfError(result, nameof(SDKInstance.SetFanMode));

                FanMode = mode;
            }
            finally
            {
                ReleaseLock();
            }
        }

        /// <summary>
        /// Controls cooler regime
        /// </summary>
        /// <param name="mode">Desired mode</param>
        /// <exception cref="AndorSDKException"/>
        public void CoolerControl(Switch mode)
        {
            try
            {
                SetActiveAndLock();

                // Checks if cooling is supported
                if (!Capabilities.SetFunctions.HasFlag(SetFunction.Temperature))
                    throw new AndorSDKException("Camera does not support cooler controls.", new ArgumentException());

                //if (IsInTemperatureCycle &&
                //    IsAsyncTemperatureCycle &&
                //    mode == Switch.Disabled)
                //    throw new TaskCanceledException("Camera is in process of async cooling. Cannot control cooler synchronously.");

                uint result = SDK.DRV_SUCCESS;
                
                // Switches cooler mode
                if (mode == Switch.Enabled)
                    result = Call(SDKInstance.CoolerON);
                else if (mode == Switch.Disabled)
                    result = Call(SDKInstance.CoolerOFF);

                ThrowIfError(result, nameof(SDKInstance.CoolerON) + " or " + nameof(SDKInstance.CoolerOFF));
                CoolerMode = mode;

                var status = GetCurrentTemperature();

                if (mode == Switch.Enabled)
                    OnCoolingStarted(new TemperatureStatusEventArgs(status.Status, status.Temperature));
                else
                    OnCoolingFinished(new TemperatureStatusEventArgs(status.Status, status.Temperature));

            }
            finally
            {
                ReleaseLock();
            }
        }

        /// <summary>
        /// Sets target cooling temperature
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="AcquisitionInProgressException"/>
        /// <param name="temperature">Temperature</param>
        public void SetTemperature(int temperature)
        {
            try
            {
                SetActiveAndLock();

                // Checks if acquisition is in progress; throws exception
                ThrowIfAcquiring(this);

                // Checks if temperature can be controlled
                if (!Capabilities.SetFunctions.HasFlag(SetFunction.Temperature))
                    throw new AndorSDKException("Camera does not support temperature controls.", new ArgumentException());

                // Checks if temperature is valid
                if (Properties.AllowedTemperatures.Minimum >= Properties.AllowedTemperatures.Maximum)
                    throw new AndorSDKException("Valid temperature range was not received from camera.", new ArgumentNullException());

                // Checks if temperature is in valid range
                if (temperature > Properties.AllowedTemperatures.Maximum ||
                    temperature < Properties.AllowedTemperatures.Minimum)
                    throw new ArgumentOutOfRangeException($"Provided temperature ({temperature}) is out of valid range " +
                        $"({Properties.AllowedTemperatures.Minimum }, " +
                         $"{Properties.AllowedTemperatures.Maximum }).");

                var result = Call(SDKInstance.SetTemperature, temperature);
                ThrowIfError(result, nameof(SDKInstance.SetTemperature));
            }
            finally
            {
                ReleaseLock();
            }
        }

        public void ShutterControl(            
            int clTime,
            int opTime,
            ShutterMode inter,           
            ShutterMode exter = ShutterMode.FullyAuto,
            TTLShutterSignal type = TTLShutterSignal.Low)
        {
            if (clTime < 0)
                throw new ArgumentOutOfRangeException($"Closing time cannot be less than 0 (should be {clTime} > {0}).");

            if (opTime < 0)
                throw new ArgumentOutOfRangeException($"Opening time cannot be less than 0 (should be {opTime} > {0}).");

            try
            {
                SetActiveAndLock();

                if (!Capabilities.Features.HasFlag(SDKFeatures.Shutter))
                    throw new AndorSDKException("Camera does not support shutter control.", null);

                if (Capabilities.Features.HasFlag(SDKFeatures.ShutterEx))
                {
                    LockManually();
                    var result = SDKInstance.SetShutterEx((int)type, (int)inter, clTime, opTime, (int)exter);
                    ReleaseManually();

                    ThrowIfError(result, nameof(SDKInstance.SetShutterEx));

                    Shutter = ( Internal: inter, External: exter, Type: type, OpenTime: opTime, CloseTime: clTime);
                }
                else
                {
                    LockManually();
                    var result = SDKInstance.SetShutter((int)type, (int)inter, clTime, opTime);
                    ReleaseManually();

                    ThrowIfError(result, nameof(SDKInstance.SetShutter));

                    Shutter = (Internal: inter, External: null, Type: type, OpenTime: opTime, CloseTime: clTime);
                }
            }
            finally
            {
                ReleaseLock();
            }
        }

        /// <summary>
        /// Returns current camera temperature and temperature status
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <returns>Temperature status and temperature in degrees</returns>
        public (TemperatureStatus Status, float Temperature) GetCurrentTemperature()
        {
            try
            {
                SetActiveAndLock();

                // Checks if acquisition is in progress; throws exception
               // ThrowIfAcquiring(this);

                if (!Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                    throw new AndorSDKException("Camera does not support temperature inquires.", new ArgumentException());

                
                var result = Call(SDKInstance.GetTemperatureF, out float temp);
                switch (result)
                {
                   // case SDK.DRV_ACQUIRING:
                    //    throw new AcquisitionInProgressException("Camera is in acquisition mode.");
                    case SDK.DRV_NOT_INITIALIZED:
                        throw new AndorSDKException("Camera is not initialized.", result);
                    case SDK.DRV_ERROR_ACK:
                        throw new AndorSDKException("Communication error.", result);

                }

                var status = (TemperatureStatus)result;

                //if (!IsAsyncTemperatureCycle &&
                //    IsInTemperatureCycle &&
                //    status != TemperatureStatus.NotReached)
                //    IsInTemperatureCycle = false;

                return (Status: status, Temperature: temp);
            }
            finally
            {
                ReleaseLock();
            }
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
            try
            {
                SetActiveAndLock();

                // If acquisition is already in progress, throw exception
                ThrowIfAcquiring(this);

                // Starts acquisition
                ThrowIfError(Call(SDKInstance.StartAcquisition), nameof(SDKInstance.StartAcquisition));

                // Fires event
                OnAcquisitionStarted(new AcquisitionStatusEventArgs(GetStatus(), IsAsyncAcquisition));

                // Marks camera as in process of acquiring
                IsAcquiring = true;
            }
            finally
            {
                ReleaseLock();
            }
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
            try
            {
                SetActiveAndLock();

                // If there is no acquisition, throws exception
                if (!IsAcquiring)
                    throw new AndorSDKException("Acquisition abort attemted while there is no acquisition in proress.", null);

                if (isAsyncAcquisition)
                    throw new TaskCanceledException("Camera is in process of async acquisition. Cannot call synchronous abort.");

                // Tries to abort acquisition
                ThrowIfError(Call(SDKInstance.AbortAcquisition), nameof(SDKInstance.AbortAcquisition));

                // Fires AcquisitionAborted event
                OnAcquisitionAborted(new AcquisitionStatusEventArgs(GetStatus(), IsAsyncAcquisition));

                // Marks the end of acquisition
                IsAcquiring = false;
            }
            finally
            {
                ReleaseLock();
            }
        }

        /// <summary>
        /// Enables or disables background temperature monitor
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        /// <param name="mode">Regime</param>
        /// <param name="timeout">Time interval between checks</param>
        public void TemperatureMonitor(Switch mode, int timeout = TempCheckTimeOutMS)
        {
            try
            {
                SetActiveAndLock();

                // If monitor shold be enbled
                if (mode == Switch.Enabled)
                {
                    // Throws if temperature monitoring is not supported
                    if (!Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                        throw new NotSupportedException("Camera dose not support temperature queries.");

                    // If background task has not been started yet
                    if (TemperatureMonitorWorker == null ||
                        TemperatureMonitorWorker.Status == TaskStatus.Canceled ||
                        TemperatureMonitorWorker.Status == TaskStatus.RanToCompletion ||
                        TemperatureMonitorWorker.Status == TaskStatus.Faulted)
                        // Starts new with a cancellation token
                        TemperatureMonitorWorker = Task.Factory.StartNew(
                            () => TemperatureMonitorCycler(TemperatureMonitorCanellationSource.Token, timeout),
                            TemperatureMonitorCanellationSource.Token);

                    // If task was created, but has not started, start it
                    if (TemperatureMonitorWorker.Status == TaskStatus.Created)
                        TemperatureMonitorWorker.Start();

                }
                // If monitor should be disabled
                if (mode == Switch.Disabled)
                {
                    // if there is a working background monitor
                    if (TemperatureMonitorWorker?.Status == TaskStatus.Running ||
                        TemperatureMonitorWorker?.Status == TaskStatus.WaitingForActivation ||
                        TemperatureMonitorWorker?.Status == TaskStatus.WaitingToRun)
                        // Stops it via cancellation token
                        TemperatureMonitorCanellationSource.Cancel();
                }
            }
            finally
            {
                ReleaseLock();
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
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <param name="camIndex">The index of a camera (cannot exceed [0, 7] range). Usually limited by <see cref="Camera.GetNumberOfCameras()"/></param>
        public Camera(int camIndex = 0)
        {
            // Stores return codes from SDK functions
            uint result = 0;
            int n = GetNumberOfCameras();
            if (n == 0)
                throw new AndorSDKException("No ANDOR-compatible cameras found.", null);

            // If cameraIndex is less than 0, it is out of range
            if (camIndex < 0)
                throw new ArgumentException($"Camera index is out of range; Cannot be less than 0 (provided {camIndex}).");
            // If cameraIndex equals to or exceeds the number of available cameras, it is also out of range
            if (camIndex >= n)
                throw new ArgumentException($"Camera index is out of range; Cannot be greater than {GetNumberOfCameras() - 1} (provided {camIndex}).");
            // If camera with such index is already in use, throws exception
            if (CreatedCameras.Count(cam => cam.Value.CameraIndex == camIndex) != 0)
                throw new ArgumentException($"Camera with index {camIndex} is already created.");

            // Stores the handle (SDK private pointer) to the camera. A unique identifier
            result = Call(SDKInstance.GetCameraHandle, camIndex, out int handle);
            ThrowIfError(result, nameof(SDKInstance.GetCameraHandle));

            // If succede, assigns handle to Camera property
            CameraHandle = new SafeSDKCameraHandle(handle);

            try
            {// Sets current camera active
                SetActiveAndLock();

                // Initializes current camera
                result = Call(SDKInstance.Initialize, ".\\");
                ThrowIfError(result, nameof(SDKInstance.Initialize));

                // If succeeded, sets IsInitialized flag to true and adds current camera to the list of initialized cameras
                IsInitialized = true;
                if (!CreatedCameras.TryAdd(CameraHandle.SDKPtr, this))
                    throw new InvalidOperationException("Failed to add camera to the concurrent dictionary");
                
                CameraIndex = camIndex;

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

                //if (Capabilities.Features.HasFlag(SDKFeatures.FanControl))
                //    FanControl(FanMode.Off);

            }
            finally
            {
                ReleaseLock();
            }
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
                Console.WriteLine($"Disposing camera {this.SerialNumber}");

                // Saves currently active camera
                var oldCamera = ActiveCamera;

                // Makes active camera that is going to be disposed (this)
                try
                {
                    SetActiveAndLock();

                    // ShutsDown camera
                    CameraHandle.Dispose();

                    // If succeeded, removes camera instance from the list of cameras
                    CreatedCameras.TryRemove(CameraHandle.SDKPtr, out _);

                   
                }
                finally
                {
                   
                    // If there are no other cameras, 
                    if (CreatedCameras.Count == 0)
                        ActiveCamera = null;
                    // If there are, sets active the one that was active before disposing procedure
                    else oldCamera?.SetActive();

                    ReleaseLock();
                }
               
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
            CameraStatus status = CameraStatus.Idle;
            try
            {
                SetActiveAndLock();

                // Checks if acquisition is in progress; throws exception
                ThrowIfAcquiring(this);

                // If camera is not idle, cannot start acquisition
                if (GetStatus() != CameraStatus.Idle)
                    throw new AndorSDKException("Camera is not in the idle mode.", null);

                
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
                ReleaseLock();
            }
            

        }

        [Obsolete("Do not use this method, it is deprecated. Use serial tempearture controls.",true)]
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
           
            var result = Call(SDKInstance.GetAvailableCameras, out int cameraCount);
            ThrowIfError(result, nameof(SDKInstance.GetAvailableCameras));


            return cameraCount;
        }

        /// <summary>
        /// Generates an interface for debug purposes 
        /// Does not requrire real camera
        /// </summary>
        /// <returns></returns>
        public static Interfaces.ICameraControl GetDebugInterface(int camIndex = 0) 
            => new DebugCamera(camIndex);
        
    }

}
