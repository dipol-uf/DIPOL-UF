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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using ANDOR_CS.Exceptions;
using ImageDisplayLib;
using SDK = ATMCD64CS.AndorSDK;
using static ANDOR_CS.Exceptions.AndorSdkException;
using static ANDOR_CS.Exceptions.AcquisitionInProgressException;

using static ANDOR_CS.Classes.AndorSdkInitialization;
using Timer = System.Timers.Timer;

namespace ANDOR_CS.Classes
{
    /// <summary>
    /// Represents an instance of a Camera device
    /// </summary>
    public sealed class Camera : CameraBase
    {
        private Timer _temperatureMonitorTimer;
        //private Task TemperatureMonitorWorker = null;
        //private CancellationTokenSource TemperatureMonitorCancellationSource
        //    = new CancellationTokenSource();

        private static readonly ConcurrentDictionary<int, CameraBase> _createdCameras
            = new ConcurrentDictionary<int, CameraBase>();
        
        private readonly ConcurrentDictionary<int, (Task Task, CancellationTokenSource Source)> _runningTasks = 
            new ConcurrentDictionary<int, (Task Task, CancellationTokenSource Source)>();

        public override bool IsTemperatureMonitored =>
            _temperatureMonitorTimer?.Enabled ?? false;
        /// <summary>
        /// Indicates if this camera is currently active
        /// </summary>
        public override bool IsActive
        {
            get
            {
                if (Call(CameraHandle, SDKInstance.GetCurrentCamera, out int cam) == SDK.DRV_SUCCESS)
                    return cam == CameraHandle.SdkPtr;
                throw new Exception();

            }

            protected set => throw new NotSupportedException();
        }
        /// <summary>
        /// A safe handle that stores native SDK pointer to the current <see cref="Camera"/> resource.
        /// </summary>
        public SafeSdkCameraHandle CameraHandle
        {
            get;
        }


        /// <summary>
        /// Read-only collection of all local cameras in use.
        /// </summary>
        public static IReadOnlyDictionary<int, CameraBase> CamerasInUse
            => _createdCameras;


        /// <summary>
        /// Retrieves camera's capabilities
        /// </summary>
        private void GetCapabilities()
        {
            CheckIsDisposed();
            // Throws if camera is acquiring
            ThrowIfAcquiring(this);

            // Holds information about camera's capabilities
            var caps = default(SDK.AndorCapabilities);

            // Unmanaged structure size
            caps.ulSize = (uint)Marshal.SizeOf(caps);

            // Using manual locker controls to call SDk function task-safely

            //LockManually();
            //var result = SDKInstance.GetCapabilities(ref caps);
            //ReleaseManually();

            var result = Call(CameraHandle, () => SDKInstance.GetCapabilities(ref caps));


            ThrowIfError(result, nameof(SDKInstance.GetCapabilities));

            // Assigns current camera's property
            Capabilities = new DeviceCapabilities(caps);

        }
        /// <summary>
        /// Retrieves camera's serial number
        /// </summary>
        private void GetCameraSerialNumber()
        {
            CheckIsDisposed();

            // Checks if acquisition is in progress
            ThrowIfAcquiring(this);

            // Retrieves number
            var result = Call(CameraHandle, SDKInstance.GetCameraSerialNumber, out int number);

            if (result == SDK.DRV_SUCCESS)
                SerialNumber = number.ToString();

        }
        /// <summary>
        /// Retrieves camera's model
        /// </summary>
        private void GetHeadModel()
        {
            CheckIsDisposed();

            // Checks if acquisition is in process
            ThrowIfAcquiring(this);

            // Retrieves model
            var result = Call(CameraHandle, SDKInstance.GetHeadModel, out string model);


            if (result == SDK.DRV_SUCCESS)
                CameraModel = model;

        }
        /// <summary>
        /// Determines properties of currently active camera and sets respective Camera.Properties field.
        /// </summary>
        /// <exception cref="AndorSdkException"/>
        /// <exception cref="AcquisitionInProgressException"/>
        private void GetCameraProperties()
        {
            CheckIsDisposed();

            // Checks if acquisition is in progress; throws exception
            ThrowIfAcquiring(this);

            // To call SDK methods, current camera should be active (this.IsActive == true).
            // If it is not the case, then either it was not set active (wrong design of program) or an error happened 
            // while switching control to this camera (and thus behaviour is undefined)
            //if (!IsActive)
            //    throw new AndorSdkException("Camera is not active. Cannnot perform this operation.", null);

            // Stores return codes of ANDOR functions
            uint result;

            // Variables used to retrieve minimum and maximum temperature range (if applicable)
            var min = 0;
            var max = 0;

            // Checks if current camera supports temperature queries
            if (Capabilities.GetFunctions.HasFlag(GetFunction.TemperatureRange))
            {
                // Native call to SDK
                // Uses manual synchronization calls
                result = Call(CameraHandle, (ref (int Min, int Max) output) =>
                    SDKInstance.GetTemperatureRange(ref output.Min, ref output.Max),
                    out var oMinMax);

                min = oMinMax.Min;
                max = oMinMax.Max;

                // If return code is not DRV_SUCCESS = (uint) 20002, throws standard AndorSDKException 
                ThrowIfError(result, nameof(SDKInstance.GetTemperatureRange));

                // Check if returned temperatures are valid (min <= max)
                if (min > max)
                    throw new AndorSdkException($"SDK function {nameof(SDKInstance.GetTemperatureRange)} returned invalid temperature range (should be {min} <= {max})", null);
            }

            // Variable used to retrieve horizotal and vertical (maximum?) detector size in pixels (if applicable)
            var h = 0;
            var v = 0;

            // Checks if current camera supports detector size queries
            if (Capabilities.GetFunctions.HasFlag(GetFunction.DetectorSize))
            {
                // Manual synchronization
                result = Call(CameraHandle, (ref (int H, int V) output) =>
                    SDKInstance.GetDetector(ref output.H, ref output.V),
                    out var oHV);
                h = oHV.H;
                v = oHV.V;

                ThrowIfError(result, nameof(SDKInstance.GetDetector));

                // Checks if detector size is valid (h > 0, v > 0)
                if ((h <= 0) | (v <= 0))
                    throw new AndorSdkException($"SDK function {nameof(SDKInstance.GetDetector)} returned invalid detector size (should be {h} > 0 and {v} > 0)", null);
            }

            // Variable used to store retrieved infromation about presence of private mechanical shutter (if applicable)
            var shutter = false;

            // private shutters are only present in these cameras (according to documentation)
            if (Capabilities.CameraType == CameraType.IXon | Capabilities.CameraType == CameraType.IXonUltra)
            {

                // Task-synchronized call to SDK method
                result = Call(CameraHandle, SDKInstance.IsInternalMechanicalShutter, out int shutterFlag);
                // Here result can be DRV_NOT_AVAILABLE = (uint) 20992, which means that camera is not iXon.
                // If this code is returned, then something went wrong while camera was initialized and camera type is incorrect
                ThrowIfError(result, nameof(GetCameraProperties));

                // Converts int value to bool
                shutter = shutterFlag == 1;

            }



            result = Call(CameraHandle, SDKInstance.GetNumberADChannels, out int adChannels);
            ThrowIfError(result, nameof(SDKInstance.GetNumberADChannels));
            // According to documentation, this call returns always DRV_SUCCESS = (uint) 20002, 
            // so there is no need for error-check
            // However, it is checked that the number of AD-converters is a valid number (> 0)
            if (adChannels <= 0)
                throw new AndorSdkException($"Function {nameof(SDKInstance.GetNumberADChannels)} returned invalid number of AD converters (returned {adChannels} should be greater than 0).", null);

            // An array of bit ranges for each available AD converter
            var aDsBitRange = new int[adChannels];

            for (var adcIndex = 0; adcIndex < aDsBitRange.Length; adcIndex++)
            {

                result = Call(CameraHandle, SDKInstance.GetBitDepth, adcIndex, out int localBitDepth);
                ThrowIfError(result, nameof(SDKInstance.GetBitDepth));

                // If it is successful, asssign obtained bit depth to an element of an array
                aDsBitRange[adcIndex] = localBitDepth;
            }


            result = Call(CameraHandle, SDKInstance.GetNumberAmp, out int amps);
            // Again, according to documentation the only return code is DRV_SUCCESS = (uint) 20002, 
            // thus the number of amplifiers should be checked to be in a valid range (> 0)
            if (amps <= 0)
                throw new AndorSdkException($"Function {nameof(SDKInstance.GetNumberAmp)} returned invalid number of amplifiers (returned {amps} should be greater than 0 and less than 2).", null);

            // Amplifier information array
            var amplifiers = new(string Name, OutputAmplification Amplifier, float MaxSpeed)[amps];

            for (var ampIndex = 0; ampIndex < amps; ampIndex++)
            {

                // Manual synchronization
                result = Call(CameraHandle, (ref string output) =>
                    SDKInstance.GetAmpDesc(ampIndex, ref output, AmpDescriptorMaxLength), out var ampName);

                ThrowIfError(result, nameof(SDKInstance.GetAmpDesc));

                // Retrieves maximum horizontal speed
                result = Call(CameraHandle, SDKInstance.GetAmpMaxSpeed, ampIndex, out float speed);
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

            result = Call(CameraHandle, SDKInstance.GetNumberPreAmpGains, out int preAmpGainMaxNumber);
            ThrowIfError(result, nameof(SDKInstance.GetNumberPreAmpGains));

            // Array of pre amp gain desciptions
            var preAmpGainDesc = new string[preAmpGainMaxNumber];


            for (var preAmpIndex = 0; preAmpIndex < preAmpGainMaxNumber; preAmpIndex++)
            {

                // Retrieves decription
                // Manual synchronization
                //result = Call((ref string output) =>
                //    SDKInstance.GetPreAmpGainText(preAmpIndex, ref output, PreAmpGainDescriptorMaxLength),
                //    out string desc);                    
                var desc = "";
                result = Call(CameraHandle, () => SDKInstance.GetPreAmpGainText(preAmpIndex, ref desc, PreAmpGainDescriptorMaxLength));
                ThrowIfError(result, nameof(SDKInstance.GetPreAmpGainText));

                // If success, adds it to array
                preAmpGainDesc[preAmpIndex] = desc;
            }



            result = Call(CameraHandle, SDKInstance.GetNumberVSSpeeds, out int vsSpeedNumber);
            ThrowIfError(result, nameof(SDKInstance.GetNumberVSSpeeds));

            // Checks if number of different vertical speeds is actually greater than 0
            if (vsSpeedNumber <= 0)
                throw new AndorSdkException($"Function {nameof(SDKInstance.GetNumberVSSpeeds)} returned invalid number of available vertical speeds (returned {vsSpeedNumber} should be greater than 0).", null);


            var speedArray = new float[vsSpeedNumber];

            for (var speedIndex = 0; speedIndex < vsSpeedNumber; speedIndex++)
            {

                result = Call(CameraHandle, SDKInstance.GetVSSpeed, speedIndex, out float localSpeed);
                ThrowIfError(result, nameof(SDKInstance.GetVSSpeed));

                // Assigns obtained speed to an array of speeds
                speedArray[speedIndex] = localSpeed;
            }

            (int low, int high) = (0, 0);

            if (Capabilities.GetFunctions.HasFlag(GetFunction.EmccdGain))
            {
                Call(CameraHandle, (ref (int Low, int High) output) =>
                    SDKInstance.GetEMGainRange(ref output.Low, ref output.High),
                    out var oLH);
                low = oLH.Low;
                high = oLH.High;
            }

            // Assemples a new CameraProperties object using collected above information
            Properties = new CameraProperties
            {
                AllowedTemperatures = (Minimum: min, Maximum: max),
                DetectorSize = new Size(h, v),
                HasInternalMechanicalShutter = shutter,
                ADConverters = aDsBitRange,
                OutputAmplifiers = amplifiers,
                PreAmpGains = preAmpGainDesc,
                VSSpeeds = speedArray,
                EMCCDGainRange = (low, high)
            };

        }
        /// <summary>
        /// Retrieves software/hardware versions
        /// </summary>
        /// <exception cref="AcquisitionInProgressException"/>
        private void GetSoftwareHardwareVersion()
        {

            CheckIsDisposed();

            // Checks if acquisition is in progress; throws exception
            ThrowIfAcquiring(this);

            // Stores return codes of SDK functions

            // Variables are passed to SDK function and store version information
            uint eprom = 0;
            uint cof = 0;
            uint driverVer = 0;
            uint driverRev = 0;
            uint dllVer = 0;
            uint dllRev = 0;


            var result = Call(CameraHandle, () => SDKInstance.GetSoftwareVersion(ref eprom, ref cof, ref driverRev, ref driverVer, ref dllRev, ref dllVer));

            ThrowIfError(result, nameof(SDKInstance.GetSoftwareVersion));

            // Assigns obtained version information to the class field
            Software = (
                EPROM: new Version((int)eprom, 0),
                COFFile: new Version((int)cof, 0),
                Driver: new Version((int)driverVer, (int)driverRev),
                Dll: new Version((int)dllVer, (int)dllRev)
            );

            // Variables are passed to SDK function and store hardware version information
            uint pcb = 0;
            uint decode = 0;
            uint dummy = 0;
            uint firmwareVer = 0;
            uint firmwareRev = 0;

            // Manual synchronization

            result = Call(CameraHandle, () => SDKInstance.GetHardwareVersion(ref pcb, ref decode, ref dummy, ref dummy, ref firmwareVer, ref firmwareRev));

            ThrowIfError(result, nameof(SDKInstance.GetHardwareVersion));

            // Assigns obtained hardware versions to the class field
            Hardware = (
                PCB: new Version((int)pcb, 0),
                Decode: new Version((int)decode, 0),
                CameraFirmware: new Version((int)firmwareVer, (int)firmwareRev)
            );

        }
        /// <summary>
        /// Represents a worker that runs infinite loop until cancellation is requested.
        /// </summary>
        /// <param name="sender">Timer</param>
        /// <param name="e">Timer event arguments</param>
        private void TemperatureMonitorCycler(object sender, ElapsedEventArgs e)
        {
            // Checks if temperature can be queried
            if (!_isDisposed && // camera is not disposed
                (   !IsAcquiring ||  // either it is not acquiring or it supports run-time queries
                    Capabilities.Features.HasFlag(SdkFeatures.ReadTemperatureDuringAcquisition)) &&
                sender is Timer t && // sender is Timer
                t.Enabled) // and Timer is enabled (not stopped and not in process of disposal)
            {
                // Gets temperature and status
                (var status, var temp) = GetCurrentTemperature();

                // Fires event
                OnTemperatureStatusChecked(new TemperatureStatusEventArgs(status, temp));
            }
        }
        /// <summary>
        /// Retrives new image from camera buffer and pushes it to queue.
        /// </summary>
        /// <param name="e">Parameters obtained from <see cref="CameraBase.NewImageReceived"/> event.</param>
        private void PushNewImage(NewImageReceivedEventArgs e)
        {

            CheckIsDisposed();

            var array = new ushort[CurrentSettings.ImageArea.Value.Height * CurrentSettings.ImageArea.Value.Width];
            (int First, int Last) = (0, 0);
            ThrowIfError(Call(CameraHandle, () =>
                SDKInstance.GetImages16(e.Last, e.Last, array, (uint)(array.Length), ref First, ref Last)), nameof(SDKInstance.GetImages16));

            _acquiredImages.Enqueue(new Image(array, CurrentSettings.ImageArea.Value.Width, CurrentSettings.ImageArea.Value.Height));

        }


        /// <summary>
        /// Sets current camera active
        /// </summary>
        /// <exception cref="AndorSdkException"/>
        public override void SetActive()
        {
            CheckIsDisposed();

            if (!IsActive)
            {
                // If camera address is invalid, throws exception
                if (CameraHandle.SdkPtr == 0)
                    throw new AndorSdkException($"Camera has invalid private address of {CameraHandle.SdkPtr}.", new NullReferenceException());

                // Tries to make this camera active
                var result = Call(CameraHandle, SDKInstance.SetCurrentCamera, CameraHandle.SdkPtr);
                // If it fails, throw an exception
                ThrowIfError(result, nameof(SDKInstance.SetCurrentCamera));

            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets current status of the camera
        /// </summary>
        /// <exception cref="T:ANDOR_CS.Exceptions.AndorSdkException" />
        /// <returns>Camera status</returns>
        public override CameraStatus GetStatus()
        {
            CheckIsDisposed();

            // Queries status, throws exception if error happened
            ThrowIfError(Call(CameraHandle, SDKInstance.GetStatus, out int status), nameof(SDKInstance.GetStatus));

            // Converts status to enum
            var camStatus = (CameraStatus)status;

            // If acquisition is started without background task, camera instance 
            // is in acquisition state, but actual camera returns status that is different
            // from "Acquiring", then updates status, acknowledging end of acquisition 
            // end firing AcquisitioFinished event.
            // Without this call there is no way to synchronously update instance of camera class
            // when real acquisition on camera finished.
            if (!IsAcquiring || IsAsyncAcquisition || camStatus == CameraStatus.Acquiring)
                return camStatus;
            IsAcquiring = false;
            OnAcquisitionFinished(new AcquisitionStatusEventArgs(camStatus, false));
            return camStatus;

        }

        /// <summary>
        /// Sets fan mode
        /// </summary>
        /// <param name="mode">Desired fan mode</param>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="AndorSdkException"/>
        /// <exception cref="AcquisitionInProgressException"/>
        public override void FanControl(FanMode mode)
        {
            CheckIsDisposed();

            // Checks if acquisition is in progress; throws exception
            ThrowIfAcquiring(this);

            // Checks if Fan Control is supported
            if (!Capabilities.Features.HasFlag(SdkFeatures.FanControl))
                throw new NotSupportedException("Camera does not support fan controls.");

            // Checks if intermediate mode is supported
            if (mode == FanMode.LowSpeed &&
                !Capabilities.Features.HasFlag(SdkFeatures.LowFanMode))
                throw new NotSupportedException("Camera does not support low-speed fan mode.");


            var result = Call(CameraHandle, SDKInstance.SetFanMode, (int)mode);

            ThrowIfError(result, nameof(SDKInstance.SetFanMode));

            FanMode = mode;


        }

        /// <summary>
        /// Controls cooler regime
        /// </summary>
        /// <param name="mode">Desired mode</param>
        /// <exception cref="AndorSdkException"/>
        public override void CoolerControl(Switch mode)
        {
            CheckIsDisposed();
            // Checks if cooling is supported
            if (!Capabilities.SetFunctions.HasFlag(SetFunction.Temperature))
                throw new AndorSdkException("Camera does not support cooler controls.", new ArgumentException());

            //if (IsInTemperatureCycle &&
            //    IsAsyncTemperatureCycle &&
            //    mode == Switch.Disabled)
            //    throw new TaskCanceledException("Camera is in process of async cooling. Cannot control cooler synchronously.");

            uint result = SDK.DRV_SUCCESS;

            // Switches cooler mode
            if (mode == Switch.Enabled)
                result = Call(CameraHandle, SDKInstance.CoolerON);
            else if (mode == Switch.Disabled)
                result = Call(CameraHandle, SDKInstance.CoolerOFF);

            ThrowIfError(result, nameof(SDKInstance.CoolerON) + " or " + nameof(SDKInstance.CoolerOFF));
            CoolerMode = mode;

        }


        /// <summary>
        /// Sets target cooling temperature
        /// </summary>
        /// <exception cref="AndorSdkException"/>
        /// <exception cref="AcquisitionInProgressException"/>
        /// <param name="temperature">Temperature</param>
        public override void SetTemperature(int temperature)
        {
            CheckIsDisposed();
            // Checks if acquisition is in progress; throws exception
            ThrowIfAcquiring(this);

            // Checks if temperature can be controlled
            if (!Capabilities.SetFunctions.HasFlag(SetFunction.Temperature))
                throw new AndorSdkException("Camera does not support temperature controls.", new ArgumentException());

            // Checks if temperature is valid
            if (Properties.AllowedTemperatures.Minimum >= Properties.AllowedTemperatures.Maximum)
                throw new AndorSdkException("Valid temperature range was not received from camera.", new ArgumentNullException());

            // Checks if temperature is in valid range
            if (temperature > Properties.AllowedTemperatures.Maximum ||
                temperature < Properties.AllowedTemperatures.Minimum)
                throw new ArgumentOutOfRangeException($"Provided temperature ({temperature}) is out of valid range " +
                    $"({Properties.AllowedTemperatures.Minimum }, " +
                     $"{Properties.AllowedTemperatures.Maximum }).");

            var result = Call(CameraHandle, SDKInstance.SetTemperature, temperature);
            ThrowIfError(result, nameof(SDKInstance.SetTemperature));

        }

        public override void ShutterControl(
            int clTime,
            int opTime,
            ShutterMode inter,
            ShutterMode exter = ShutterMode.FullyAuto,
            TtlShutterSignal type = TtlShutterSignal.Low)
        {
            if (clTime < 0)
                throw new ArgumentOutOfRangeException($"Closing time cannot be less than 0 (should be {clTime} > {0}).");

            if (opTime < 0)
                throw new ArgumentOutOfRangeException($"Opening time cannot be less than 0 (should be {opTime} > {0}).");

            CheckIsDisposed();


            if (!Capabilities.Features.HasFlag(SdkFeatures.Shutter))
                throw new AndorSdkException("Camera does not support shutter control.", null);

            if (Capabilities.Features.HasFlag(SdkFeatures.ShutterEx))
            {

                var result = Call(CameraHandle, () => SDKInstance.SetShutterEx((int)type, (int)inter, clTime, opTime, (int)exter));


                ThrowIfError(result, nameof(SDKInstance.SetShutterEx));

                Shutter = (Internal: inter, External: exter, Type: type, OpenTime: opTime, CloseTime: clTime);
            }
            else
            {

                var result = Call(CameraHandle, () => SDKInstance.SetShutter((int)type, (int)inter, clTime, opTime));


                ThrowIfError(result, nameof(SDKInstance.SetShutter));

                Shutter = (Internal: inter, External: null, Type: type, OpenTime: opTime, CloseTime: clTime);
            }
        }

        /// <summary>
        /// Returns current camera temperature and temperature status
        /// </summary>
        /// <exception cref="AndorSdkException"/>
        /// <returns>Temperature status and temperature in degrees</returns>
        public override (TemperatureStatus Status, float Temperature) GetCurrentTemperature()
        {
            CheckIsDisposed();

            if (!Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                throw new AndorSdkException("Camera does not support temperature inquires.", new ArgumentException());


            var result = Call(CameraHandle, SDKInstance.GetTemperatureF, out float temp);
            switch (result)
            {
                // case SDK.DRV_ACQUIRING:
                //    throw new AcquisitionInProgressException("Camera is in acquisition mode.");
                case SDK.DRV_NOT_INITIALIZED:
                    throw new AndorSdkException("Camera is not initialized.", result);
                case SDK.DRV_ERROR_ACK:
                    throw new AndorSdkException("Communication error.", result);

            }

            var status = (TemperatureStatus)result;

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
        /// <exception cref="AndorSdkException"/>
        public override void StartAcquisition()
        {
            CheckIsDisposed();

            _acquiredImages = new ConcurrentQueue<Image>();

            // If acquisition is already in progress, throw exception
            ThrowIfAcquiring(this);

            // Starts acquisition
            ThrowIfError(Call(CameraHandle, SDKInstance.StartAcquisition), nameof(SDKInstance.StartAcquisition));

            // Fires event
            OnAcquisitionStarted(new AcquisitionStatusEventArgs(GetStatus(), IsAsyncAcquisition));

            // Marks camera as in process of acquiring
            IsAcquiring = true;
        }

        /// <inheritdoc />
        /// <summary>
        /// A synchronous way to manually abort acquisition.
        /// NOTE: if called while async acquisition is in progress, throws
        /// <see cref="T:System.Threading.Tasks.TaskCanceledException" />. To cancel async acquisition, use 
        /// <see cref="T:System.Threading.CancellationToken" />.
        /// </summary>
        /// <exception cref="T:ANDOR_CS.Exceptions.AndorSdkException" />
        /// <exception cref="T:System.Threading.Tasks.TaskCanceledException" />
        public override void AbortAcquisition()
        {
            CheckIsDisposed();

            // If there is no acquisition, throws exception
            if (!IsAcquiring)
                throw new AndorSdkException("Acquisition abort atemted while there is no acquisition in proress.", null);

            //if (IsAsyncAcquisition)
            //    throw new TaskCanceledException("Camera is in process of async acquisition. Cannot call synchronous abort.");

            // Tries to abort acquisition
            ThrowIfError(Call(CameraHandle, SDKInstance.AbortAcquisition), nameof(SDKInstance.AbortAcquisition));

            // Fires AcquisitionAborted event
            OnAcquisitionAborted(new AcquisitionStatusEventArgs(GetStatus(), IsAsyncAcquisition));

            // Marks the end of acquisition
            IsAcquiring = false;
            IsAsyncAcquisition = false;
        }

        /// <summary>
        /// Enables or disables background temperature monitor
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        /// <param name="mode">Regime</param>
        /// <param name="timeout">Time interval between checks</param>
        public override void TemperatureMonitor(Switch mode, int timeout = TempCheckTimeOutMs)
        {
            CheckIsDisposed();

            // Throws if temperature monitoring is not supported
            if (!Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                throw new NotSupportedException("Camera dose not support temperature queries.");

            // If monitor shold be enbled
            if (mode == Switch.Enabled)
            {

                if (_temperatureMonitorTimer == null)
                    _temperatureMonitorTimer = new Timer();

                if (_temperatureMonitorTimer.Enabled)
                    _temperatureMonitorTimer.Stop();

                _temperatureMonitorTimer.AutoReset = true;

                _temperatureMonitorTimer.Interval = timeout;

                _temperatureMonitorTimer.Elapsed += TemperatureMonitorCycler;

                _temperatureMonitorTimer.Start();
                
            }
            else
                _temperatureMonitorTimer?.Stop();

        }

        /// <summary>
        /// Generates an instance of <see cref="AcquisitionSettings"/> that can be used to select proper settings for image
        /// acquisition in the context of this camera
        /// </summary>
        /// <exception cref="AndorSdkException"/>
        /// <returns>A template that can be used to select proper acquisition settings</returns>
        public override SettingsBase GetAcquisitionSettingsTemplate()
        {
            CheckIsDisposed();

            if (!IsInitialized)
                throw new AndorSdkException("Camera is not initialized properly.", new NullReferenceException());

            return new AcquisitionSettings(this);
        }

        /// <summary>
        /// A realisation of <see cref="IDisposable.Dispose"/> method.
        /// Frees SDK-related resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {

            if (!_isDisposed)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    // If camera has valid SDK pointer and is initialized
                    if (IsInitialized && !CameraHandle.IsClosed && !CameraHandle.IsInvalid)
                    {
                        if (CoolerMode == Switch.Enabled)
                            CoolerControl(Switch.Disabled);
                        //if (TemperatureMonitorWorker?.Status == TaskStatus.Running)
                        //    TemperatureMonitor(Switch.Disabled);

                        if (_temperatureMonitorTimer != null)
                        {
                            if (_temperatureMonitorTimer.Enabled)
                                _temperatureMonitorTimer.Stop();

                            _temperatureMonitorTimer.Close();
                        }

                        foreach (var key in _runningTasks.Keys)
                        {
                            _runningTasks.TryRemove(key, out var item);
                            item.Source.Cancel();
                        }
                    }

                    // If succeeded, removes camera instance from the list of cameras
                    _createdCameras.TryRemove(CameraHandle.SdkPtr, out _);
                    // ShutsDown camera
                    CameraHandle.Dispose();
                }
            }
        }

        /// <summary>
        /// Creates a new instance of Camera class to represent a connected Andor device.
        /// Maximum 8 cameras can be controled at the same time
        /// </summary>
        /// <exception cref="AndorSdkException"/>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <param name="camIndex">The index of a camera (cannot exceed [0, 7] range). Usually limited by <see cref="Camera.GetNumberOfCameras()"/></param>
        public Camera(int camIndex = 0)
        {
            // Stores return codes from SDK functions
            var n = GetNumberOfCameras();
            if (n == 0)
                throw new AndorSdkException("No ANDOR-compatible cameras found.", null);

            // If cameraIndex is less than 0, it is out of range
            if (camIndex < 0)
                throw new ArgumentException($"Camera index is out of range; Cannot be less than 0 (provided {camIndex}).");
            // If cameraIndex equals to or exceeds the number of available cameras, it is also out of range
            if (camIndex > n)
                throw new ArgumentException($"Camera index is out of range; Cannot be greater than {GetNumberOfCameras() - 1} (provided {camIndex}).");
            // If camera with such index is already in use, throws exception
            if (_createdCameras.Count(cam => cam.Value.CameraIndex == camIndex) != 0)
                throw new ArgumentException($"Camera with index {camIndex} is already created.");

            // Stores the handle (SDK private pointer) to the camera. A unique identifier
            var result = CallWithoutHandle(SDKInstance.GetCameraHandle, camIndex, out int handle);
            ThrowIfError(result, nameof(SDKInstance.GetCameraHandle));

            // If succede, assigns handle to Camera property
            CameraHandle = new SafeSdkCameraHandle(handle);

            // Sets current camera active
            //ActiveCamera = this;

            // SetActiveAndLock();
            // SetActive();
            // Initializes current camera
            result = Call(CameraHandle, SDKInstance.Initialize, ".\\");
            ThrowIfError(result, nameof(SDKInstance.Initialize));

            // If succeeded, sets IsInitialized flag to true and adds current camera to the list of initialized cameras
            IsInitialized = true;
            if (!_createdCameras.TryAdd(CameraHandle.SdkPtr, this))
                throw new InvalidOperationException("Failed to add camera to the concurrent dictionary");

            CameraIndex = camIndex;

            //SetActive();

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

            //CreatedCameras.TryAdd(CameraIndex, this);

            NewImageReceived += (sender, e) => PushNewImage(e);

            // Default state of fan - FullSpeed
            if (Capabilities.Features.HasFlag(SdkFeatures.FanControl))
                FanControl(FanMode.FullSpeed);

            // Default state of cooler - Off
            if (Capabilities.SetFunctions.HasFlag(SetFunction.Temperature))
                CoolerControl(Switch.Disabled);

            // Default state of shutter(s) - Closed
            if (Capabilities.Features.HasFlag(SdkFeatures.Shutter))
                ShutterControl(27, 27, ShutterMode.PermanentlyClosed, ShutterMode.PermanentlyClosed, TtlShutterSignal.High);
        }

        /// <summary>
        /// Starts process of acquisition asynchronously.
        /// This is the preferred way to acquire images from camera.
        /// To run synchronously, call i.e. <see cref="Task.Wait()"/> on the returned task.
        /// </summary>
        /// <param name="source">Cancellation token source that can be used to abort process.</param>
        /// <param name="timeout">Time interval in ms between subsequent camera status queries.</param>
        /// <exception cref="AcquisitionInProgressException"/>
        /// <exception cref="AndorSdkException"/>
        /// <returns>Task that can be queried for execution status.</returns>
        public override async Task StartAcquistionAsync(CancellationTokenSource source, int timeout = StatusCheckTimeOutMs)
        {
            CheckIsDisposed();

            var task = Task.Run(() =>
            {
                var status = CameraStatus.Idle;
                try
                {
                    // Checks if acquisition is in progress; throws exception
                    ThrowIfAcquiring(this);

                    // If camera is not idle, cannot start acquisition
                    if (GetStatus() != CameraStatus.Idle)
                        throw new AndorSdkException("Camera is not in the idle mode.", null);

                    // Marks acuisition asynchronous
                    IsAsyncAcquisition = true;

                    // Start scquisition
                    StartAcquisition();

                    status = GetStatus();

                    (int First, int Last) previousImages = (0, 0);
                    (int First, int Last) acquiredImagesIndex;
                    // While status is acquiring
                    while ((status = GetStatus()) == CameraStatus.Acquiring)
                    {
                        // Fires AcquisitionStatusChecked event
                        OnAcquisitionStatusChecked(new AcquisitionStatusEventArgs(status, true));

                        // Checks if new image is already acuired and is available in camera memory

                        // Gets indexes of first and last available new images
                        acquiredImagesIndex = (0, 0);


                        ThrowIfError(Call(CameraHandle, () => SDKInstance.GetNumberNewImages(ref acquiredImagesIndex.First, ref acquiredImagesIndex.Last)),
                            nameof(SDKInstance.GetNumberNewImages));

                        // If there is new image, updates indexes of previous abailable images and fires an event.
                        if (acquiredImagesIndex.Last != previousImages.Last
                            || acquiredImagesIndex.First != previousImages.First)
                        {
                            previousImages = acquiredImagesIndex;

                            OnNewImageReceived(new NewImageReceivedEventArgs(acquiredImagesIndex.First, acquiredImagesIndex.Last));
                        }

                        // If task is aborted
                        if (source.Token.IsCancellationRequested)
                        {
                            // Aborts
                            AbortAcquisition();
                            // Exits wait loop
                            break;
                        }

                        // Waits for specified amount of time before checking status again

                        Thread.Sleep(timeout);
                    }

                    // Gets indexes of first and last available new images


                    ThrowIfError(Call(CameraHandle, (ref (int, int) output) =>
                        SDKInstance.GetNumberNewImages(ref output.Item1, ref output.Item2),
                        out acquiredImagesIndex), nameof(SDKInstance.GetNumberNewImages));

                    // If there is new image, updates indexes of previous abailable images and fires an event.
                    if (acquiredImagesIndex.Last != previousImages.Last
                        || acquiredImagesIndex.First != previousImages.First)
                    {
                        OnNewImageReceived(new NewImageReceivedEventArgs(acquiredImagesIndex.First, acquiredImagesIndex.Last));
                    }

                    // If after end of acquisition camera status is not idle, throws exception
                    if (!source.Token.IsCancellationRequested && status != CameraStatus.Idle)
                        throw new AndorSdkException($"Acquisiotn finished with non-Idle status ({status}).", null);

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
            });

            int id = task.Id;

            _runningTasks.TryAdd(id, (Task: task, Source: source));

            await task;

            if (!_runningTasks.TryRemove(id, out _))
                throw new InvalidOperationException("Failed to remove finished task from queue.");

        }

        /// <summary>
        /// Queries the number of currently connected Andor cameras
        /// </summary>
        /// <exception cref="AndorSdkException"/>
        /// <returns>TNumber of detected cameras</returns>
        public static int GetNumberOfCameras()
        {
            // Variable is passed to SDK function

            var result = CallWithoutHandle(SDKInstance.GetAvailableCameras, out int cameraCount);
            ThrowIfError(result, nameof(SDKInstance.GetAvailableCameras));

            return cameraCount;
        }

#if DEBUG
        public static CameraBase GetDebugInterface(int camIndex = 0)
            => new DebugCamera(camIndex);
#endif

        
    }

}
