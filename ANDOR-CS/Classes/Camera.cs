//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Events;
using ANDOR_CS.Exceptions;
using FITS_CS;
#if X86
using SDK = ATMCD32CS.AndorSDK;
#endif
#if X64
using SDK = ATMCD64CS.AndorSDK;
#endif

using static ANDOR_CS.Exceptions.AndorSdkException;
using static ANDOR_CS.Exceptions.AcquisitionInProgressException;

using static ANDOR_CS.Classes.AndorSdkInitialization;
using Image= DipolImage.Image;
#pragma warning disable 1591

namespace ANDOR_CS.Classes
{
    /// <inheritdoc />
    /// <summary>
    /// Represents an instance of a Camera device
    /// </summary>
    public sealed class Camera : CameraBase
    {
        private static readonly ConcurrentDictionary<int, CameraBase> CreatedCameras
            = new ConcurrentDictionary<int, CameraBase>();
        
        private ConcurrentDictionary<int, (Image Image, NewImageReceivedEventArgs args)> _images = 
            new ConcurrentDictionary<int, (Image Image, NewImageReceivedEventArgs args)>();

        private readonly EventWaitHandle _eventHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private readonly bool _isMetadataAvailable;
        private readonly bool _useSdkEvents;
        private readonly CancellationTokenSource _sdkEventCancellation;

        private event EventHandler SdkEventFired;

        /// <summary>
        /// Indicates if this camera is currently active
        /// </summary>
        public override bool IsActive
        {
            get
            {
                if (Call(CameraHandle, SdkInstance.GetCurrentCamera, out int cam) == SDK.DRV_SUCCESS)
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
            => CreatedCameras;
        
        /// <summary>
        /// Retrieves camera's capabilities
        /// </summary>
        private void GetCapabilities()
        {
            CheckIsDisposed();
            // Throws if camera is acquiring
            //ThrowIfAcquiring(this);
            if (FailIfAcquiring(this, out var except)) throw except;

            // Holds information about camera's capabilities
            var caps = default(SDK.AndorCapabilities);

            // Unmanaged structure size
            caps.ulSize = (uint)Marshal.SizeOf(caps);

            // Using manual locker controls to call SDk function task-safely

            //var result = Call(CameraHandle, () => SdkInstance.GetCapabilities(ref caps));
            if (FailIfError(
                Call(CameraHandle, () => SdkInstance.GetCapabilities(ref caps)),
                nameof(SdkInstance.GetCapabilities), 
                out except)) throw except;

            //ThrowIfError(result, nameof(SdkInstance.GetCapabilities));

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
            //ThrowIfAcquiring(this);
            if (FailIfAcquiring(this, out var except)) throw except;

            // Retrieves number
            var result = Call(CameraHandle, SdkInstance.GetCameraSerialNumber, out int number);

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
            //ThrowIfAcquiring(this);
            if (FailIfAcquiring(this, out var except)) throw except;

            // Retrieves model
            var result = Call(CameraHandle, SdkInstance.GetHeadModel, out string model);


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
            //ThrowIfAcquiring(this);
            if (FailIfAcquiring(this, out var except)) throw except;


            // To call SDK methods, current camera should be active (this.IsActive == true).
            // If it is not the case, then either it was not set active (wrong design of program) or an error happened 
            // while switching control to this camera (and thus behaviour is undefined)

            // Variables used to retrieve minimum and maximum temperature range (if applicable)
            var min = 0;
            var max = 0;

            // Checks if current camera supports temperature queries
            if (Capabilities.GetFunctions.HasFlag(GetFunction.TemperatureRange))
            {
                // Native call to SDK
                // Uses manual synchronization calls
                //result = Call(CameraHandle, (ref (int Min, int Max) output) =>
                //    SdkInstance.GetTemperatureRange(ref output.Min, ref output.Max),
                //    out var oMinMax);

                if(FailIfError(
                    Call(CameraHandle, (ref (int Min, int Max) output) =>
                            SdkInstance.GetTemperatureRange(ref output.Min, ref output.Max),
                        out var oMinMax),
                    nameof(SdkInstance.GetTemperatureRange),
                    out except)) throw except;

                min = oMinMax.Min;
                max = oMinMax.Max;

                // If return code is not DRV_SUCCESS = (uint) 20002, throws standard AndorSDKException 
                //ThrowIfError(result, nameof(SdkInstance.GetTemperatureRange));

                // Check if returned temperatures are valid (min <= max)
                if (min > max)
                    throw new AndorSdkException($"SDK function {nameof(SdkInstance.GetTemperatureRange)} returned invalid temperature range (should be {min} <= {max})", null);
            }

            // Variable used to retrieve horizontal and vertical (maximum?) detector size in pixels (if applicable)
            var h = 0;
            var v = 0;

            // Checks if current camera supports detector size queries
            if (Capabilities.GetFunctions.HasFlag(GetFunction.DetectorSize))
            {
                // Manual synchronization
                //result = Call(CameraHandle, (ref (int H, int V) output) =>
                //    SdkInstance.GetDetector(ref output.H, ref output.V),
                //    out var detectorSize);

                if (FailIfError(
                    Call(CameraHandle, (ref (int H, int V) output) =>
                            SdkInstance.GetDetector(ref output.H, ref output.V),
                        out var detectorSize),
                    nameof(SdkInstance.GetDetector),
                    out except)) throw except;

                h = detectorSize.H;
                v = detectorSize.V;

                //ThrowIfError(result, nameof(SdkInstance.GetDetector));

                // Checks if detector size is valid (h > 0, v > 0)
                if ((h <= 0) | (v <= 0))
                    throw new AndorSdkException($"SDK function {nameof(SdkInstance.GetDetector)} returned invalid detector size (should be {h} > 0 and {v} > 0)", null);
            }

            // Variable used to store retrieved information about presence of private mechanical shutter (if applicable)
            var shutter = false;

            // private shutters are only present in these cameras (according to documentation)
            if (Capabilities.CameraType == CameraType.IXon | Capabilities.CameraType == CameraType.IXonUltra)
            {

                // Task-synchronized call to SDK method
                //result = Call(CameraHandle, SdkInstance.IsInternalMechanicalShutter, out int shutterFlag);
                if (FailIfError(
                    Call(CameraHandle, SdkInstance.IsInternalMechanicalShutter, out int shutterFlag),
                    nameof(SdkInstance.IsInternalMechanicalShutter),
                    out except)) throw except;

                // Here result can be DRV_NOT_AVAILABLE = (uint) 20992, which means that camera is not iXon.
                // If this code is returned, then something went wrong while camera was initialized and camera type is incorrect
                //ThrowIfError(result, nameof(GetCameraProperties));

                // Converts int value to bool
                shutter = shutterFlag == 1;

            }



            //result = Call(CameraHandle, SdkInstance.GetNumberADChannels, out int adChannels);
            //ThrowIfError(result, nameof(SdkInstance.GetNumberADChannels));
            if (FailIfError(
                Call(CameraHandle, SdkInstance.GetNumberADChannels, out int adChannels),
                nameof(SdkInstance.GetNumberADChannels),
                out except)) throw except;

            // According to documentation, this call returns always DRV_SUCCESS = (uint) 20002, 
            // so there is no need for error-check
            // However, it is checked that the number of AD-converters is a valid number (> 0)
            if (adChannels <= 0)
                throw new AndorSdkException($"Function {nameof(SdkInstance.GetNumberADChannels)} returned invalid number of AD converters (returned {adChannels} should be greater than 0).", null);

            // An array of bit ranges for each available AD converter
            var aDsBitRange = new int[adChannels];

            for (var adcIndex = 0; adcIndex < aDsBitRange.Length; adcIndex++)
            {

                //result = Call(CameraHandle, SdkInstance.GetBitDepth, adcIndex, out int localBitDepth);
                //ThrowIfError(result, nameof(SdkInstance.GetBitDepth));
                if (FailIfError(
                    Call(CameraHandle, SdkInstance.GetBitDepth, adcIndex, out int localBitDepth),
                    nameof(SdkInstance.GetBitDepth),
                    out except)) throw except;

                // If it is successful, assign obtained bit depth to an element of an array
                aDsBitRange[adcIndex] = localBitDepth;
            }


            //result = Call(CameraHandle, SdkInstance.GetNumberAmp, out int amps);
            //ThrowIfError(result, nameof(SdkInstance.GetNumberAmp));
            if (FailIfError(
                Call(CameraHandle, SdkInstance.GetNumberAmp, out int amps),
                nameof(SdkInstance.GetNumberAmp),
                out except)) throw except;

            // Again, according to documentation the only return code is DRV_SUCCESS = (uint) 20002, 
            // thus the number of amplifiers should be checked to be in a valid range (> 0)
            if (amps <= 0)
                throw new AndorSdkException($"Function {nameof(SdkInstance.GetNumberAmp)} returned invalid number of amplifiers (returned {amps} should be greater than 0 and less than 2).", null);

            // Amplifier information array
            var amplifiers = new(string Name, OutputAmplification Amplifier, float MaxSpeed)[amps];

            for (var ampIndex = 0; ampIndex < amps; ampIndex++)
            {

                var locIndex = ampIndex;
                // Manual synchronization
                //result = Call(CameraHandle, (ref string output) =>
                //    SdkInstance.GetAmpDesc(locIndex, ref output, AmpDescriptorMaxLength), out var ampName);
                //ThrowIfError(result, nameof(SdkInstance.GetAmpDesc));
                if (FailIfError(
                    Call(CameraHandle, (ref string output) =>
                        SdkInstance.GetAmpDesc(locIndex, ref output, AmpDescriptorMaxLength), out var ampName),
                    nameof(SdkInstance.GetAmpDesc),
                    out except)) throw except;


                    // Retrieves maximum horizontal speed
                    //result = Call(CameraHandle, SdkInstance.GetAmpMaxSpeed, ampIndex, out float speed);
                    //ThrowIfError(result, nameof(SdkInstance.GetAmpMaxSpeed));
                if (FailIfError(
                    Call(CameraHandle, SdkInstance.GetAmpMaxSpeed, ampIndex, out float speed),
                    nameof(SdkInstance.GetAmpMaxSpeed),
                    out except)) throw except;


                // Adds obtained values to array
                amplifiers[ampIndex] = (
                    Name: ampName,
                    // In case of Clara 0 corresponds to Conventional (OutputAmplification = 1) and 1 corresponds to ExtendedNIR (OutputAmplification = 2)
                    // Adds 1 to obtained indices in case of Clara camera to store amplifier information properly
                    Amplifier: (OutputAmplification)(ampIndex + (Capabilities.CameraType == CameraType.Clara ? 1 : 0)),
                    MaxSpeed: speed);
            }


            // Stores the (maximum) number of different pre-Amp gain settings. Depends on currently selected AD-converter and amplifier

            //result = Call(CameraHandle, SdkInstance.GetNumberPreAmpGains, out int preAmpGainMaxNumber);
            //ThrowIfError(result, nameof(SdkInstance.GetNumberPreAmpGains));
            if (FailIfError(
                Call(CameraHandle, SdkInstance.GetNumberPreAmpGains, out int preAmpGainMaxNumber),
                nameof(SdkInstance.GetNumberPreAmpGains),
                out except)) throw except;

            // Array of pre amp gain descriptions
            var preAmpGainDesc = new string[preAmpGainMaxNumber];


            for (var preAmpIndex = 0; preAmpIndex < preAmpGainMaxNumber; preAmpIndex++)
            {

                // Retrieves description
                // Manual synchronization
                //result = Call((ref string output) =>
                //    SDKInstance.GetPreAmpGainText(preAmpIndex, ref output, PreAmpGainDescriptorMaxLength),
                //    out string desc);                    
                var desc = "";
                var index = preAmpIndex;
                //result = Call(CameraHandle, () => SdkInstance.GetPreAmpGainText(index, ref desc, PreAmpGainDescriptorMaxLength));
                //ThrowIfError(result, nameof(SdkInstance.GetPreAmpGainText));
                if (FailIfError(
                    Call(CameraHandle,
                        () => SdkInstance.GetPreAmpGainText(index, ref desc, PreAmpGainDescriptorMaxLength)),
                    nameof(SdkInstance.GetPreAmpGainText),
                    out except)) throw except;

                // If success, adds it to array
                preAmpGainDesc[preAmpIndex] = desc;
            }

            //result = Call(CameraHandle, SdkInstance.GetNumberVSSpeeds, out int vsSpeedNumber);
            //ThrowIfError(result, nameof(SdkInstance.GetNumberVSSpeeds));
            if (FailIfError(
                Call(CameraHandle, SdkInstance.GetNumberVSSpeeds, out int vsSpeedNumber),
                nameof(SdkInstance.GetNumberVSSpeeds),
                out except)) throw except;

            // Checks if number of different vertical speeds is actually greater than 0
            if (vsSpeedNumber <= 0)
                throw new AndorSdkException($"Function {nameof(SdkInstance.GetNumberVSSpeeds)} returned invalid number of available vertical speeds (returned {vsSpeedNumber} should be greater than 0).", null);


            var speedArray = new float[vsSpeedNumber];

            for (var speedIndex = 0; speedIndex < vsSpeedNumber; speedIndex++)
            {

                //result = Call(CameraHandle, SdkInstance.GetVSSpeed, speedIndex, out float localSpeed);
                //ThrowIfError(result, nameof(SdkInstance.GetVSSpeed));
                if (FailIfError(
                    Call(CameraHandle, SdkInstance.GetVSSpeed, speedIndex, out float localSpeed),
                    nameof(SdkInstance.GetVSSpeed),
                    out except)) throw except;

                // Assigns obtained speed to an array of speeds
                speedArray[speedIndex] = localSpeed;
            }

            var (low, high) = (0, 0);

            if (Capabilities.GetFunctions.HasFlag(GetFunction.EmccdGain))
            {
                Call(CameraHandle, (ref (int Low, int High) output) =>
                    SdkInstance.GetEMGainRange(ref output.Low, ref output.High),
                    out var gainRange);
                low = gainRange.Low;
                high = gainRange.High;
            }

            // Assembles a new CameraProperties object using collected above information
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
            if (FailIfAcquiring(this, out var except))
                throw except;

            // Stores return codes of SDK functions

            // Variables are passed to SDK function and store version information
            uint eprom = 0;
            uint cof = 0;
            uint driverVer = 0;
            uint driverRev = 0;
            uint dllVer = 0;
            uint dllRev = 0;


            var result = Call(CameraHandle, () => SdkInstance.GetSoftwareVersion(ref eprom, ref cof, ref driverRev, ref driverVer, ref dllRev, ref dllVer));

            if (FailIfError(result, nameof(SdkInstance.GetSoftwareVersion), out except))
                throw except;

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

            result = Call(CameraHandle, () => SdkInstance.GetHardwareVersion(ref pcb, ref decode, ref dummy, ref dummy, ref firmwareVer, ref firmwareRev));

            if (FailIfError(result, nameof(SdkInstance.GetHardwareVersion), out except))
                throw except;

            // Assigns obtained hardware versions to the class field
            Hardware = (
                PCB: new Version((int)pcb, 0),
                Decode: new Version((int)decode, 0),
                CameraFirmware: new Version((int)firmwareVer, (int)firmwareRev)
            );

        }
        /// <summary>
        /// Retrieves new image from camera buffer and pushes it to queue.
        /// </summary>
        /// <param name="e">Parameters obtained from <see cref="CameraBase.NewImageReceived"/> event.</param>
        private void PushNewImage(NewImageReceivedEventArgs e)
        {

            CheckIsDisposed();

            if (CurrentSettings.ImageArea != null)
            {
                var array = new ushort[CurrentSettings.ImageArea.Value.Height * CurrentSettings.ImageArea.Value.Width];

                var (first, last) = (0, 0);

                if (FailIfError(Call(CameraHandle, () =>
                        SdkInstance.GetImages16(e.Last, e.Last, array, (uint) (array.Length), ref first, ref last)),
                    nameof(SdkInstance.GetImages16), out var except))
                    throw except;

                var im = new Image(array, CurrentSettings.ImageArea.Value.Width,
                    CurrentSettings.ImageArea.Value.Height);
                _acquiredImages.Enqueue(im);

                //if (!(FilePattern is null))
                //{
                //    var path = Path.Combine(_imgDir, 
                //        string.Format(FilePattern, CameraModel, last, e.EventTime));
                //    // ReSharper disable once AssignNullToNotNullAttribute
                //    Directory.CreateDirectory(Path.GetDirectoryName(path));

                //    var keys = new FitsKey[SettingsProvider.MetaFitsKeys.Count + 1];
                //    SettingsProvider.MetaFitsKeys.CopyTo(0, keys, 1, SettingsProvider.MetaFitsKeys.Count);
                //    keys[0] = FitsKey.CreateDate("OBSTIME", e.EventTime);
                //    using (var str = new FileStream(path, FileMode.OpenOrCreate))
                //        FitsStream.WriteImage(im, FitsImageType.Int16, str, keys);
                //}
            }
        }
        private void Temp()
        {
              var timings = (Exposure: 0f, Accumulation: 0f, Kinetic: 0f);
                Call(CameraHandle,
                    () => SdkInstance.GetAcquisitionTimings(ref timings.Exposure, ref timings.Accumulation,
                        ref timings.Kinetic));

                Call<float>(CameraHandle, SdkInstance.GetReadOutTime, out var readout);
                Call<float>(CameraHandle, SdkInstance.GetKeepCleanTime, out var keepClean);

                Console.WriteLine((timings, readout, keepClean));

                // Marks acquisition asynchronous
                var acquisitionPollingTimer = new System.Timers.Timer()
                {
                    Enabled = false,
                    AutoReset = true
                };
                // Start acquisition
                var completionSrc = new TaskCompletionSource<bool>();

                var start = DateTime.Now;

                void StatusUpdaterKinetic(object sender, ElapsedEventArgs e)
                {
                    if (!(sender is System.Timers.Timer timer) || !timer.Enabled) return;
                    try
                    {
                        var status = GetStatus();
                        var progress = (Accumulation: 0, Kinetic: 0);
                        if (FailIfError(Call(CameraHandle,
                                () => SdkInstance.GetAcquisitionProgress(ref progress.Accumulation,
                                    ref progress.Kinetic)),
                            nameof(SdkInstance.GetAcquisitionProgress), out var innerExcept))
                            throw innerExcept;

                        OnAcquisitionStatusChecked(new AcquisitionStatusEventArgs(status));


                        foreach (var item in PullNewImages<ushort>())
                        {
                            var offset = TimeSpan.FromSeconds(item.Index * timings.Accumulation);
                            //_images.TryAdd(item.Key, (item.Image, default));
                            Console.WriteLine($"{item.Index}\t{start + offset:ss.ffffff}\r\n");
                        }
                        //Console.WriteLine(progress);


                        if (status != CameraStatus.Acquiring)
                        {
                            acquisitionPollingTimer.Stop();
                            if(!completionSrc.Task.IsCompleted)
                                completionSrc.SetResult(true);
                        }
                    }
                    catch (Exception innerEx)
                    {
                        acquisitionPollingTimer.Stop();
                        if (!completionSrc.Task.IsCompleted)
                            completionSrc.SetException(innerEx);
                    }
                }

                void StatusUpdaterNormal(object sender, ElapsedEventArgs e)
                {
                    if (!(sender is System.Timers.Timer timer) || !timer.Enabled) return;
                    try
                    {
                        var status = GetStatus();
                        OnAcquisitionStatusChecked(new AcquisitionStatusEventArgs(status, e.SignalTime, 0, 0));
                        
                        if (status != CameraStatus.Acquiring)
                        {
                            acquisitionPollingTimer.Stop();
                            if (!completionSrc.Task.IsCompleted)
                                completionSrc.SetResult(true);
                        }
                    }
                    catch (Exception innerEx)
                    {
                        acquisitionPollingTimer.Stop();
                        if (!completionSrc.Task.IsCompleted)
                            completionSrc.SetException(innerEx);
                    }
                }


                //acquisitionPollingTimer.Interval = timeout;

                if (CurrentSettings.KineticCycle.HasValue)
                    acquisitionPollingTimer.Elapsed += StatusUpdaterKinetic;
                else
                    acquisitionPollingTimer.Elapsed += StatusUpdaterNormal;

                start = DateTime.Now.AddMilliseconds(SettingsProvider.Settings.Get("AcquisitionTimerOffsetMS", 0.0));

                StartAcquisition();
                acquisitionPollingTimer.Start();

                //await completionSrc.Task;
                acquisitionPollingTimer.Stop();
        }
        private List<(int Key, int Index, Image Image)> PullNewImages<T>() where T : unmanaged
        {
            TypeCode imageType;

            if (typeof(T) == typeof(ushort))
                imageType = TypeCode.UInt16;
            else if (typeof(T) == typeof(int))
                imageType = TypeCode.Int32;
            else
                throw new ArgumentException("Cannot pull images in the unsupported type.", nameof(T));

            if (CurrentSettings?.ImageArea is null)
                throw new NullReferenceException(
                    "Cannot pull images without acquisition settings applied and known image area.");

            

            var size = Marshal.SizeOf<T>();
            var matrixSize = CurrentSettings.ImageArea.Value.Width *
                             CurrentSettings.ImageArea.Value.Height;
            var indices = (First: 0, Last: 0);
            var all = (First: 0, Last: 0);

            if (FailIfError(Call(CameraHandle,
                    () => SdkInstance.GetNumberNewImages(ref indices.First, ref indices.Last)),
                nameof(SdkInstance.GetNumberNewImages), out var except))
                throw except;
            if (FailIfError(Call(CameraHandle,
                    () => SdkInstance.GetNumberAvailableImages(ref all.First, ref all.Last)),
                nameof(SdkInstance.GetNumberNewImages), out except))
                throw except;
            Console.WriteLine($"{indices}\t{all}");

            var images =  new List<(int Key, int Index, Image Image)>();

            if (indices.First > 0 && indices.First <= indices.Last)
            {
                var nImage = indices.Last - indices.First + 1;
                var nBlocks = nImage / MaxImagesPerCall;
                nBlocks = nBlocks * MaxImagesPerCall < nImage ? nBlocks + 1 : nBlocks;

                
                Array buffer = new T[MaxImagesPerCall * matrixSize];
                var imgBuffer = new byte[matrixSize * size];
                var validIndex = (First: 0, Last: 0);

                for (var i = 0; i < nBlocks; i++)
                {
                    var currentIndex = (First: indices.First + MaxImagesPerCall * i,
                        Last: Math.Min(indices.First + MaxImagesPerCall * (i + 1) - 1, indices.Last));

                    //if (typeof(T) == typeof(ushort)
                    //    && FailIfError(Call(CameraHandle, () => SdkInstance.GetImages16(
                    //        currentIndex.First, currentIndex.Last,
                    //        (ushort[]) buffer,
                    //        (uint) ((currentIndex.Last - currentIndex.First + 1) * matrixSize),
                    //        ref validIndex.First, ref validIndex.Last)), nameof(SdkInstance.GetImages16), out except))
                    //    throw except;

                    //if (typeof(T) == typeof(int)
                    //    && FailIfError(Call(CameraHandle, () => SdkInstance.GetImages(
                    //        currentIndex.First, currentIndex.Last,
                    //        (int[]) buffer,
                    //        (uint) ((currentIndex.Last - currentIndex.First + 1) * matrixSize),
                    //        ref validIndex.First, ref validIndex.Last)), nameof(SdkInstance.GetImages), out except))
                    //    throw except;

                    for (var j = currentIndex.First; j <= currentIndex.Last; j++)
                    {
                        Buffer.BlockCopy(buffer, (j - 1) % MaxImagesPerCall * size * matrixSize, imgBuffer, 0,
                            matrixSize * size);
                        SDK.SYSTEMTIME time = default;
                        
                        var fromStart = 0f;

                        // ReSharper disable once AccessToModifiedClosure
                        if(_isMetadataAvailable 
                           && Call(CameraHandle, () => SdkInstance.GetMetaDataInfo(ref time, ref fromStart, j)) is var result)
                            Console.WriteLine($"{j}\t{(time.ToDateTime().AddMilliseconds(fromStart)):ss.ffffff}\t{fromStart,10}\t{result == SDK.DRV_SUCCESS}\t{result == SDK.DRV_MSTIMINGS_ERROR}");

                        images.Add((
                            Key: DateTime.UtcNow.GetHashCode() + j,
                            Index: j,
                            Image: new Image(imgBuffer, CurrentSettings.ImageArea.Value.Width,
                                CurrentSettings.ImageArea.Value.Height, imageType)));
                    }

                }

            }

            return images;
        }

        private async Task EventBasedAcquisitionAsync()
        {
            var completionSrc = new TaskCompletionSource<bool>();

            void ListenSdkEvent(object sender, EventArgs e)
            {
                var status = GetStatus();
                Console.WriteLine(status);
                if (status != CameraStatus.Acquiring 
                    && !completionSrc.Task.IsCompleted)
                    completionSrc.SetResult(true);
            }

            SdkEventFired += ListenSdkEvent;
            try
            {
                StartAcquisition();
                await completionSrc.Task;
            }
            finally
            {
                SdkEventFired -= ListenSdkEvent;
            }
        }
        
        /// <summary>
        /// Starts acquisition of the image. Does not block current thread.
        /// To monitor acquisition progress, use <see cref="GetStatus"/>.
        /// Fires <see cref="CameraBase.OnAcquisitionStarted"/> 
        /// with <see cref="AcquisitionStatusEventArgs.IsAsync"/> = false.
        /// NOTE: this method is not recommended. Consider using async version
        /// <see cref="StartAcquisitionAsync(CancellationTokenSource,int)"/>.
        /// Async version allows <see cref="Camera"/> to properly monitor acquisition progress.
        /// </summary>
        /// <exception cref="AcquisitionInProgressException"/>
        /// <exception cref="AndorSdkException"/>
        protected override void StartAcquisition()
        {
            CheckIsDisposed();

            // TODO: Fix Images here
            _acquiredImages = new ConcurrentQueue<Image>();

            // If acquisition is already in progress, throw exception
            if (FailIfAcquiring(this, out var except))
                throw except;

            // Marks camera as in process of acquiring

            // Fires event
            if (FailIfError(Call(CameraHandle, SdkInstance.PrepareAcquisition), nameof(SdkInstance.PrepareAcquisition),
                out except))
                throw except;

            // Starts acquisition
            if (FailIfError(Call(CameraHandle, SdkInstance.StartAcquisition), nameof(SdkInstance.StartAcquisition),
                out except))
                throw except;

            IsAcquiring = true;
            OnAcquisitionStarted(new AcquisitionStatusEventArgs(GetStatus()));
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
        protected override void AbortAcquisition()
        {
            CheckIsDisposed();

            // If there is no acquisition, throws exception
            if (!IsAcquiring)
                throw new AndorSdkException("Acquisition abort attempted while there is no acquisition in progress.", null);

            //if (IsAsyncAcquisition)
            //    throw new TaskCanceledException("Camera is in process of async acquisition. Cannot call synchronous abort.");

            // Tries to abort acquisition
            if (FailIfError(Call(CameraHandle, SdkInstance.AbortAcquisition), nameof(SdkInstance.AbortAcquisition),
                out var except))
                throw except;

            // Fires AcquisitionAborted event
            OnAcquisitionAborted(new AcquisitionStatusEventArgs(GetStatus()));

            // Marks the end of acquisition
            IsAcquiring = false;
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
                var result = Call(CameraHandle, SdkInstance.SetCurrentCamera, CameraHandle.SdkPtr);
                // If it fails, throw an exception
                if (FailIfError(result, nameof(SdkInstance.SetCurrentCamera), out var except))
                    throw except;

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
            if (FailIfError(Call(CameraHandle, SdkInstance.GetStatus, out int status), nameof(SdkInstance.GetStatus),
                out var except))
                throw except;

            // Converts status to enum
            var camStatus = (CameraStatus)status;

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
            if (FailIfAcquiring(this, out var except))
                throw except;

            // Checks if Fan Control is supported
            if (!Capabilities.Features.HasFlag(SdkFeatures.FanControl))
                throw new NotSupportedException("Camera does not support fan controls.");

            // Checks if intermediate mode is supported
            if (mode == FanMode.LowSpeed &&
                !Capabilities.Features.HasFlag(SdkFeatures.LowFanMode))
                throw new NotSupportedException("Camera does not support low-speed fan mode.");


            var result = Call(CameraHandle, SdkInstance.SetFanMode, (int)mode);

            if (FailIfError(result, nameof(SdkInstance.SetFanMode), out except))
                throw except;

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

            var result = SDK.DRV_SUCCESS;

            // Switches cooler mode
            if (mode == Switch.Enabled)
                result = Call(CameraHandle, SdkInstance.CoolerON);
            else if (mode == Switch.Disabled)
                result = Call(CameraHandle, SdkInstance.CoolerOFF);

            if (FailIfError(result, nameof(SdkInstance.CoolerON) + " or " + nameof(SdkInstance.CoolerOFF),
                out var except))
                throw except;

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
            if (FailIfAcquiring(this, out var except))
                throw except;

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

            var result = Call(CameraHandle, SdkInstance.SetTemperature, temperature);
            if (FailIfError(result, nameof(SdkInstance.SetTemperature), out except))
                throw except;

        }

        public override void ShutterControl(
            ShutterMode inter,
            ShutterMode extrn,
            int opTime,
            int clTime,
            TtlShutterSignal type)
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

                var result = Call(CameraHandle, () => SdkInstance.SetShutterEx((int)type, (int)inter, clTime, opTime, (int)extrn));


                if(FailIfError(result, nameof(SdkInstance.SetShutterEx), out var except))
                    throw except;

                Shutter = (Internal: inter, External: extrn, Type: type, OpenTime: opTime, CloseTime: clTime);
            }
            else
            {

                var result = Call(CameraHandle, () => SdkInstance.SetShutter((int)type, (int)inter, clTime, opTime));


                if(FailIfError(result, nameof(SdkInstance.SetShutter), out var except))
                    throw except;

                Shutter = (Internal: inter, External: null, Type: type, OpenTime: opTime, CloseTime: clTime);
            }
        }

        public override void ShutterControl(ShutterMode inter, ShutterMode extrn)
        {
            ShutterControl(inter, extrn,
                SettingsProvider.Settings.Get("ShutterOpenTimeMS", 27),
                SettingsProvider.Settings.Get("ShutterCloseTimeMS", 27),
                (TtlShutterSignal)SettingsProvider.Settings.Get("TTLShutterSignal", 1));
        }

        /// <inheritdoc />
        /// <summary>
        /// Returns current camera temperature and temperature status
        /// </summary>
        /// <exception cref="T:ANDOR_CS.Exceptions.AndorSdkException" />
        /// <returns>Temperature status and temperature in degrees</returns>
        public override (TemperatureStatus Status, float Temperature) GetCurrentTemperature()
        {
            CheckIsDisposed();

            if (!Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                throw new AndorSdkException("Camera does not support temperature inquires.", new ArgumentException());


            var result = Call(CameraHandle, SdkInstance.GetTemperatureF, out float temp);
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

        public override void EnableAutosave(in string pattern)
        {
            FilePattern = pattern;
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
        /// An implementation of <see cref="IDisposable.Dispose"/> method.
        /// Frees SDK-related resources
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    IsDisposing = true;
                    // If camera has valid SDK pointer and is initialized
                    if (IsInitialized && !CameraHandle.IsClosed && !CameraHandle.IsInvalid)
                    {
                        if (IsAcquiring)
                           AbortAcquisition();

                        if (Capabilities.SetFunctions.HasFlag(SetFunction.Temperature))
                        {
                            Call(CameraHandle, SdkInstance.CoolerOFF);
                            CoolerMode = Switch.Disabled;
                        }

                        if (Capabilities.Features.HasFlag(SdkFeatures.Shutter))
                        {
                            if (Capabilities.Features.HasFlag(SdkFeatures.ShutterEx))
                            {

                                Call(CameraHandle, () => SdkInstance.SetShutterEx(
                                    (int)Shutter.Type, 
                                    (int)ShutterMode.PermanentlyClosed, 
                                    Shutter.CloseTime, Shutter.OpenTime, 
                                    (int)ShutterMode.PermanentlyClosed));

                                Shutter = (
                                    Internal: ShutterMode.PermanentlyClosed, 
                                    External: ShutterMode.PermanentlyClosed, 
                                    Shutter.Type, 
                                    Shutter.OpenTime, 
                                    Shutter.CloseTime);
                            }
                            else
                            {

                                Call(CameraHandle, () => SdkInstance.SetShutter(
                                    (int)Shutter.Type, 
                                    (int)ShutterMode.PermanentlyClosed, 
                                    Shutter.CloseTime, 
                                    Shutter.OpenTime));

                                Shutter = (
                                    Internal: ShutterMode.PermanentlyClosed,
                                    External: null,
                                    Shutter.Type,
                                    Shutter.OpenTime,
                                    Shutter.CloseTime);
                            }
                        }

                        if (_useSdkEvents)
                            _sdkEventCancellation.Cancel();
                    }

                    // If succeeded, removes camera instance from the list of cameras
                    CreatedCameras.TryRemove(CameraHandle.SdkPtr, out _);
                    // ShutsDown camera
                    CameraHandle.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Creates a new instance of Camera class to represent a connected Andor device.
        /// Maximum 8 cameras can be controlled at the same time
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
            if (CreatedCameras.Count(cam => cam.Value.CameraIndex == camIndex) != 0)
                throw new ArgumentException($"Camera with index {camIndex} is already created.");

            // Stores the handle (SDK private pointer) to the camera. A unique identifier
            var result = CallWithoutHandle(SdkInstance.GetCameraHandle, camIndex, out int handle);
            if (FailIfError(result, nameof(SdkInstance.GetCameraHandle), out var except))
                throw except;

            // If succeed, assigns handle to Camera property
            CameraHandle = new SafeSdkCameraHandle(handle);

            
            // Initializes current camera
            result = Call(CameraHandle, SdkInstance.Initialize, ".\\");
            if(FailIfError(result, nameof(SdkInstance.Initialize), out except))
	            throw except;

            // If succeeded, sets IsInitialized flag to true and adds current camera to the list of initialized cameras
            IsInitialized = true;
            if (!CreatedCameras.TryAdd(CameraHandle.SdkPtr, this))
                throw new InvalidOperationException("Failed to add camera to the concurrent dictionary");

            CameraIndex = camIndex;

            CreatedCameras.TryAdd(CameraHandle.SdkPtr, this);
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

            if (!Capabilities.Features.HasFlag(SdkFeatures.Polling) &&
                !Capabilities.Features.HasFlag(SdkFeatures.Events))
            {
                Dispose();
                throw new NotSupportedException("Connected camera is incompatible with the software. Status polling or events are required.");
            }

            NewImageReceived += (sender, e) => PushNewImage(e);

            // Default state of fan - FullSpeed
            if (Capabilities.Features.HasFlag(SdkFeatures.FanControl))
                FanControl(FanMode.FullSpeed);

            // Default state of cooler - Off
            if (Capabilities.SetFunctions.HasFlag(SetFunction.Temperature))
                CoolerControl(Switch.Disabled);

            // Default state of shutter(s) - Closed
            if (Capabilities.Features.HasFlag(SdkFeatures.Shutter))
                ShutterControl(ShutterMode.PermanentlyClosed, ShutterMode.PermanentlyClosed);

            // If available, enables metadata for precise image timing
            if(Capabilities.Features.HasFlag(SdkFeatures.MetaData))
                _isMetadataAvailable = Call(CameraHandle, SdkInstance.SetMetaData, 1) == SDK.DRV_SUCCESS;

            // If available, temporary saves all acquired over one acquisition session images to the folder
            // Really useful when using cycles/series and producing more than 1 image per run.
            if (Capabilities.Features.HasFlag(SdkFeatures.Spooling)
                && SettingsProvider.Settings.TryGet("RootDirectory", out string rootPath))
            {
                var spoolPath = Path.Combine(rootPath, "Temp");
                if(SettingsProvider.Settings.Get("CleanTempOnStartup", true))
                    Directory.Delete(spoolPath, true);
                Directory.CreateDirectory(spoolPath);
                Call(CameraHandle, () => SdkInstance.SetSpool(1, 0, spoolPath + "\\", 128));
            }

            // If events are supported, use events to control acquisition
            if (Capabilities.Features.HasFlag(SdkFeatures.Events))
            {
                if (Call(CameraHandle, SdkInstance.SetDriverEvent, _eventHandle.SafeWaitHandle.DangerousGetHandle()) ==
                    SDK.DRV_SUCCESS)
                {
                    _sdkEventCancellation = new CancellationTokenSource();
                    var timeoutMs = SettingsProvider.Settings.Get("PollingIntervalMS", 100);
                    var interval = TimeSpan.FromMilliseconds(timeoutMs > 1000 ? 1000 : timeoutMs);

                    Task.Run(() =>
                    {
                        while (!_sdkEventCancellation.IsCancellationRequested)
                        {
                            if (_eventHandle.WaitOne(interval))
                                SdkEventFired?.Invoke(this, EventArgs.Empty);
                        }
                    }, _sdkEventCancellation.Token).ConfigureAwait(false);

                    _useSdkEvents = true;
                }
            }

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
        public override async Task StartAcquisitionAsync(CancellationTokenSource source,
            int timeout = StatusCheckTimeOutMs)
        {
            CheckIsDisposed();
            _images.Clear();
            try
            {
                // Checks if acquisition is in progress; throws exception
                if (FailIfAcquiring(this, out var except))
                    throw except;

                // If camera is not idle, cannot start acquisition
                if (GetStatus() != CameraStatus.Idle)
                    throw new AndorSdkException("Camera is not in the idle mode.", null);

                //if (_useSdkEvents)
                {
                    await EventBasedAcquisitionAsync();
                }


                if (_isMetadataAvailable)
                {
                    SDK.SYSTEMTIME time = default;
                    float offset = default;

                    var result = Call(CameraHandle, () => SdkInstance.GetMetaDataInfo(ref time, ref offset, 0));
                    result = Call(CameraHandle, () => SdkInstance.GetMetaDataInfo(ref time, ref offset, 1));
                    result = Call(CameraHandle, () => SdkInstance.GetMetaDataInfo(ref time, ref offset, 2));

                }

            }
            // If there were exceptions during status checking loop
            catch
            {
                // Fire event
                OnAcquisitionErrorReturned(new AcquisitionStatusEventArgs(default));
                // re-throw received exception
                throw;
            }
            // Ensures that acquisition is properly finished and event is fired
            finally
            {
                IsAcquiring = false;
                OnAcquisitionFinished(new AcquisitionStatusEventArgs(GetStatus()));
            }
        }

        /// <summary>
        /// Queries the number of currently connected Andor cameras
        /// </summary>
        /// <exception cref="AndorSdkException"/>
        /// <returns>TNumber of detected cameras</returns>
        public static int GetNumberOfCameras()
        {
            // Variable is passed to SDK function

            var result = CallWithoutHandle(SdkInstance.GetAvailableCameras, out int cameraCount);
            if (FailIfError(result, nameof(SdkInstance.GetAvailableCameras), out var except))
                throw except;

            return cameraCount;
        }

        public new static CameraBase Create(int camIndex = 0, object otherParams = null)
            => new Camera(camIndex);

        public new static async Task<CameraBase> CreateAsync(int camIndex = 0, object otherParams = null)
            => await Task.Run(() => Create(camIndex, otherParams));

#if DEBUG
        public static CameraBase GetDebugInterface(int camIndex = 0)
            => new DebugCamera(camIndex);

#endif


    }

}
