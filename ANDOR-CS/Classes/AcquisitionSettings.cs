﻿//    This file is part of Dipol-3 Camera Manager.

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
using System.Collections.Generic;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Exceptions;
using static ANDOR_CS.Classes.AndorSdkInitialization;
using static ANDOR_CS.Exceptions.AndorSdkException;

#if X86
using SDK = ATMCD32CS.AndorSDK;
#endif
#if X64
using SDK = ATMCD64CS.AndorSDK;
#endif

namespace ANDOR_CS.Classes
{
    /// <summary>
    ///     Represents all possible acquisition/Camera settings that can be adjusted before taking any images
    /// </summary>
    public class AcquisitionSettings : SettingsBase
    {
        /// <summary>
        ///     Constructor adds reference to parent <see cref="Camera" /> object.
        /// </summary>
        /// <param name="camera">Parent object. Camera, to which settings should be applied.</param>
        internal AcquisitionSettings(CameraBase camera) 
            => Camera = camera;

        /// <summary>
        ///     SERIALIZATION TEST CONSTRUCTOR; DO NOT USE
        /// </summary>
//#if DEBUG
        public AcquisitionSettings(bool empty = false)
        {
            if (empty) 
                return;

            AcquisitionMode = Enums.AcquisitionMode.SingleScan;
            ADConverter = (0, 16);
            OutputAmplifier = (OutputAmplification.Conventional, "Conventional", 1);
            ExposureTime = 123;
            HSSpeed = (0, 3);
            ImageArea = new Rectangle(1, 1, 128, 256);

            KineticCycle = (12, 0.23f);
            PreAmpGain = (0, "Gain 1");
            ReadoutMode = ReadMode.FullImage;

            TriggerMode = Enums.TriggerMode.Bulb;
            VSAmplitude = Enums.VSAmplitude.Plus2;
            VSSpeed = (0, 0.3f);
        }

//#endif
        private SafeSdkCameraHandle Handle
            => Camera is Camera cam
                ? cam.CameraHandle
                : throw new NullReferenceException(
                    $"Camera object in {nameof(AcquisitionSettings)} is of type {Camera.GetType()}, " +
                    $"while it is expected to be ${typeof(Camera)}");


        /// <summary>
        ///     Applys currenlty selected settings to the Camera.
        /// </summary>
        /// <exception cref="ArgumentNullException" />
        /// <returns>Result of application of each non-null setting</returns>
        //public override List<(string Option, bool Success, uint ReturnCode)> ApplySettings(
        //    out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) timing)
        //{
        //    var output = new List<(string Option, bool Success, uint ReturnCode)>();

        //    SafeSdkCameraHandle handle;
        //    if (Camera is Camera locCam)
        //        handle = locCam.CameraHandle;
        //    else
        //        throw new Exception("Type of Camera is wrong.");

        //    CheckCamera();

        //    uint result;

        //    if (VSSpeed.HasValue)
        //        {
        //            result = Call(handle, SdkInstance.SetVSSpeed, VSSpeed.Value.Index);

        //            output.Add((nameof(VSSpeed), result == SDK.DRV_SUCCESS, result));
        //        }

        //        if (VSAmplitude.HasValue)
        //        {
        //            result = Call(handle, SdkInstance.SetVSAmplitude, (int) VSAmplitude.Value);

        //            output.Add((nameof(VSAmplitude), result == SDK.DRV_SUCCESS, result));
        //        }

        //        if (ADConverter.HasValue)
        //        {
        //            result = Call(handle, SdkInstance.SetADChannel, ADConverter.Value.Index);

        //            output.Add((nameof(ADConverter), result == SDK.DRV_SUCCESS, result));
        //        }

        //        if (OutputAmplifier.HasValue)
        //        {
        //            result = Call(handle, SdkInstance.SetOutputAmplifier, OutputAmplifier.Value.Index);

        //            output.Add((nameof(OutputAmplifier), result == SDK.DRV_SUCCESS, result));
        //        }

        //        if (HSSpeed.HasValue)
        //        {
        //            result = Call(handle,
        //                () => SdkInstance.SetHSSpeed(OutputAmplifier?.Item3 ?? 0, HSSpeed.Value.Index));

        //            output.Add((nameof(HSSpeed), result == SDK.DRV_SUCCESS, result));
        //        }

        //        if (PreAmpGain.HasValue)
        //        {
        //            result = Call(handle, SdkInstance.SetPreAmpGain, PreAmpGain.Value.Index);

        //            output.Add((nameof(PreAmpGain), result == SDK.DRV_SUCCESS, result));
        //        }


        //        if (ImageArea.HasValue)
        //        {
        //            result = Call(handle,
        //                () => SdkInstance.SetImage(1, 1, ImageArea.Value.X1, ImageArea.Value.X2, ImageArea.Value.Y1,
        //                    ImageArea.Value.Y2));


        //            output.Add((nameof(ImageArea), result == SDK.DRV_SUCCESS, result));
        //        }

        //        if (AcquisitionMode.HasValue)
        //        {
        //            var mode = AcquisitionMode.Value;

        //            if (mode.HasFlag(Enums.AcquisitionMode.FrameTransfer))
        //            {
        //                result = Call(handle, SdkInstance.SetFrameTransferMode, 1);
        //                ThrowIfError(result, nameof(SdkInstance.SetFrameTransferMode));
        //                mode ^= Enums.AcquisitionMode.FrameTransfer;

        //                output.Add(("FrameTransfer", result == SDK.DRV_SUCCESS, result));
        //            }
        //            else
        //            {
        //                ThrowIfError(Call(handle, SdkInstance.SetFrameTransferMode, 0),
        //                    nameof(SdkInstance.SetFrameTransferMode));
        //            }

        //            result = Call(handle, SdkInstance.SetAcquisitionMode, EnumConverter.AcquisitionModeTable[mode]);

        //            output.Add((nameof(AcquisitionMode), result == SDK.DRV_SUCCESS, result));
        //        }
        //        else
        //        {
        //            throw new NullReferenceException("Acquisition mode should be set before applying settings.");
        //        }


        //        if (ReadoutMode.HasValue)
        //        {
        //            result = Call(handle, SdkInstance.SetReadMode, EnumConverter.ReadModeTable[ReadoutMode.Value]);

        //            output.Add((nameof(ReadoutMode), result == SDK.DRV_SUCCESS, result));
        //        }
        //        else
        //        {
        //            throw new NullReferenceException("Read mode should be set before applying settings.");
        //        }


        //        if (TriggerMode.HasValue)
        //        {
        //            result = Call(handle, SdkInstance.SetTriggerMode,
        //                EnumConverter.TriggerModeTable[TriggerMode.Value]);

        //            output.Add((nameof(TriggerMode), result == SDK.DRV_SUCCESS, result));
        //        }
        //        else
        //        {
        //            throw new NullReferenceException("Trigger mode should be set before applying settings.");
        //        }

        //        if (ExposureTime.HasValue)
        //        {
        //            result = Call(handle, SdkInstance.SetExposureTime, ExposureTime.Value);

        //            output.Add((nameof(ExposureTime), result == SDK.DRV_SUCCESS, result));
        //        }
        //        else
        //        {
        //            throw new NullReferenceException("Exposure time should be set before applying settings.");
        //        }

        //        if (AcquisitionMode.Value.HasFlag(Enums.AcquisitionMode.Accumulation))
        //        {
        //            if (!AccumulateCycle.HasValue)
        //                throw new NullReferenceException(
        //                    $"Accumulation cycle should be set if acquisition mode is {AcquisitionMode.Value}.");

        //            result = Call(handle, SdkInstance.SetNumberAccumulations, AccumulateCycle.Value.Frames);
        //            output.Add((nameof(AccumulateCycle)+"Number", result == SDK.DRV_SUCCESS, result));


        //            result = Call(handle, SdkInstance.SetAccumulationCycleTime, AccumulateCycle.Value.Time);
        //            //ThrowIfError(result, nameof(SDKInstance.SetAccumulationCycleTime));
        //            output.Add((nameof(AccumulateCycle)+"Time", result == SDK.DRV_SUCCESS, result));
        //        }


        //        if (AcquisitionMode.Value.HasFlag(Enums.AcquisitionMode.Kinetic))
        //        {
        //            if (!AccumulateCycle.HasValue)
        //                throw new NullReferenceException(
        //                    $"Accumulation cycle should be set if acquisition mode is {AcquisitionMode.Value}.");
        //            if (!KineticCycle.HasValue)
        //                throw new NullReferenceException(
        //                    $"Kinetic cycle should be set if acquisition mode is {AcquisitionMode.Value}.");

        //            result = Call(handle, SdkInstance.SetNumberAccumulations, AccumulateCycle.Value.Frames);
        //            //ThrowIfError(result, nameof(SDKInstance.SetNumberAccumulations));
        //            output.Add((nameof(AccumulateCycle) + "Number", result == SDK.DRV_SUCCESS, result));


        //            result = Call(handle, SdkInstance.SetAccumulationCycleTime, AccumulateCycle.Value.Time);
        //            //ThrowIfError(result, nameof(SDKInstance.SetAccumulationCycleTime));
        //            output.Add((nameof(AccumulateCycle) + "Time", result == SDK.DRV_SUCCESS, result));

        //            result = Call(handle, SdkInstance.SetNumberKinetics, KineticCycle.Value.Frames);
        //            //ThrowIfError(result, nameof(SDKInstance.SetNumberKinetics));
        //            output.Add((nameof(KineticCycle)+"Number", result == SDK.DRV_SUCCESS, result));


        //            result = Call(handle, SdkInstance.SetKineticCycleTime, KineticCycle.Value.Time);
        //            //ThrowIfError(result, nameof(SDKInstance.SetKineticCycleTime));
        //            output.Add((nameof(AccumulateCycle) + "Time", result == SDK.DRV_SUCCESS, result));
        //        }

        //        if (EMCCDGain.HasValue)
        //        {
        //            if (!OutputAmplifier.HasValue ||
        //                !OutputAmplifier.Value.OutputAmplifier.HasFlag(OutputAmplification.Conventional))
        //                throw new NullReferenceException(
        //                    $"OutputAmplifier should be set to {OutputAmplification.Conventional}");

        //            result = Call(handle, SdkInstance.SetEMCCDGain, EMCCDGain.Value);
        //            //ThrowIfError(result, nameof(SDKInstance.SetEMCCDGain));
        //            output.Add((nameof(EMCCDGain), result == SDK.DRV_SUCCESS, result));
        //        }

        //        var expTime = 0f;
        //        var accTime = 0f;
        //        var kinTime = 0f;

        //        result = Call(handle, () => SdkInstance.GetAcquisitionTimings(ref expTime, ref accTime, ref kinTime));
        //        ThrowIfError(result, nameof(SdkInstance.GetAcquisitionTimings));

        //        result = Call(handle, SdkInstance.GetSizeOfCircularBuffer, out int size);
        //        ThrowIfError(result, nameof(SdkInstance.GetSizeOfCircularBuffer));

        //        timing =
        //            (ExposureTime: expTime, AccumulationCycleTime: accTime, KineticCycleTime: kinTime, BufferSize: size
        //            );

        //    //Camera.CurrentSettings = this;
        //    base.ApplySettings(out _);

        //    return output;
            
        //}


        /// <summary>
        ///     Tries to set vertical speed to fastest recommended speed.
        ///     Requires Camera to be active.
        /// </summary>
        /// <exception cref="AndorSdkException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="NotSupportedException" />
        public void SetVSSpeed()
        {
            // Checks if Camera is OK
            CheckCamera();


            // Checks if Camera actually supports changing vertical readout speed
            if (Camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalReadoutSpeed))
            {
                // Stores retrieved recommended vertical speed
                var speedIndex = -1;
                var speedVal = 0.0f;

                var result = Call(Handle, () => SdkInstance.GetFastestRecommendedVSSpeed(ref speedIndex, ref speedVal));
                ThrowIfError(result, nameof(SdkInstance.GetFastestRecommendedVSSpeed));

                // Available speeds max index
                var length = Camera.Properties.VSSpeeds.Length;

                // If speed index is invalid
                if (speedIndex < 0 || speedIndex >= length)
                    throw new ArgumentOutOfRangeException(
                        $"Fastest recommended vertical speed index({nameof(speedIndex)}) " +
                        $"is out of range (should be in [{0},  {length - 1}]).");

                // Calls overloaded version of current method with obtained speedIndex as argument
                SetVSSpeed(speedIndex);
            }
            else
            {
                throw new NotSupportedException("Camera does not support vertical readout speed control.");
            }
        }


        /// <summary>
        ///     Returns a collection of available Horizonal Readout Speeds for currently selected OutputAmplifier and AD Converter.
        ///     Requires Camera to be active.
        ///     Note: <see cref="SettingsBase.ADConverter" /> and <see cref="SettingsBase.OutputAmplifier" /> should
        ///     be set
        ///     via <see cref="SettingsBase.SetADConverter" /> and
        ///     <see cref="SettingsBase.SetOutputAmplifier(OutputAmplification)" />
        ///     before calling this method.
        /// </summary>
        /// <exception cref="NullReferenceException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="NotSupportedException" />
        /// <returns>An enumerable collection of speed indexes and respective speed values available.</returns>
        public override IEnumerable<(int Index, float Speed)> GetAvailableHSSpeeds(int adConverter, int amplifier)
        {
            // Checks if Camera is OK and is active
            CheckCamera();

            // Checks if Camera support horizontal speed controls
            if (!Camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed))
                throw new NotSupportedException("Camera does not support horizontal readout speed controls.");
            else
            {
                // Gets the number of availab;e speeds
                var result = Call(Handle, SdkInstance.GetNumberHSSpeeds, adConverter, amplifier, out int nSpeeds);
                ThrowIfError(result, nameof(SdkInstance.GetNumberHSSpeeds));

                // Checks if obtained value is valid
                if (nSpeeds < 0)
                    throw new ArgumentOutOfRangeException(
                        $"Returned number of available speeds is less than 0 ({nSpeeds}).");

                // Iterates through speed indexes
                for (var speedIndex = 0; speedIndex < nSpeeds; speedIndex++)
                {
                    float locSpeed = 0;

                    result = Call(Handle,
                        () => SdkInstance.GetHSSpeed(adConverter, amplifier, speedIndex, ref locSpeed));
                    ThrowIfError(result, nameof(SdkInstance.GetHSSpeed));

                    // Returns speed index and speed value for evvery subsequent call
                    yield return (Index: speedIndex, Speed: locSpeed);
                }
            }
        }

      
        /// <summary>
        ///     Returns a collection of available PreAmp gains for currently selected HSSpeed, OutputAmplifier, Converter.
        ///     Requires Camera to be active.
        ///     Note: <see cref="SettingsBase.ADConverter" />, <see cref="SettingsBase.HSSpeed" />
        ///     and <see cref="SettingsBase.OutputAmplifier" /> should be set
        ///     via <see cref="SettingsBase.SetADConverter" />, <see cref="SettingsBase.SetHSSpeed" />
        ///     and <see cref="SettingsBase.SetOutputAmplifier(OutputAmplification)" />.
        /// </summary>
        /// <exception cref="NullReferenceException" />
        /// <exception cref="NotSupportedException" />
        /// <returns>Available PreAmp gains</returns>
        public override IEnumerable<(int Index, string Name)> GetAvailablePreAmpGain(
            int adConverter,
            int amplifier,
            int hsSpeed)
        {
            // Checks if Camera is OK and is active
            CheckCamera();


            // Checks if Camera supports PreAmp Gain control
            if (Camera.Capabilities.SetFunctions.HasFlag(SetFunction.PreAmpGain))
            {
                // Total number of gain settings available
                var gainNumber = Camera.Properties.PreAmpGains.Length;

                for (var gainIndex = 0; gainIndex < gainNumber; gainIndex++)
                {
                    var status = -1;

                    var result = Call(Handle, () => SdkInstance.IsPreAmpGainAvailable(
                        adConverter, amplifier, hsSpeed, gainIndex, ref status));
                    ThrowIfError(result, nameof(SdkInstance.IsPreAmpGainAvailable));

                    // If status of a certain combination of settings is 1, return it
                    if (status == 1)
                        yield return (Index: gainIndex, Name: Camera.Properties.PreAmpGains[gainIndex]);
                }
            }
            else
            {
                throw new NotSupportedException("Camera does not support Pre Amp Gain controls.");
            }
        }


        /// <summary>
        ///     Checks if HS Speed is supported in current configuration.
        ///     Throws exceptions if SDK communication fails.
        /// </summary>
        /// <param name="speedIndex">Speed index to test.</param>
        /// <param name="adConverter">AD Converter index.</param>
        /// <param name="amplifier">OutputAmplifier index.</param>
        /// <param name="speed">
        ///     If call is successfull, assigns float value of HS speed,
        ///     otherwies, is initialized to 0.0f.
        /// </param>
        /// <exception cref="AndorSdkException" />
        /// <returns>
        ///     true if HS Speed is supported,
        ///     throws exception if SDK communication fails; false, otherwise.
        /// </returns>
        public override bool IsHSSpeedSupported(
            int speedIndex,
            int adConverter,
            int amplifier,
            out float speed)
        {
            // Checks if Camera is OK and is active
            CheckCamera();
            speed = 0;
            float locSpeed = 0;
            var locCam = Camera as Camera;
            // Checks if Camera supports horizontal readout speed control
            if (!Camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed))
                return false;

            // Gets the number of availab;e speeds
            var result = Call(Handle, SdkInstance.GetNumberHSSpeeds, adConverter, amplifier, out int nSpeeds);
            ThrowIfError(result, nameof(SdkInstance.GetNumberHSSpeeds));

            // Checks if speedIndex is in allowed range
            if (speedIndex < 0 || speedIndex >= nSpeeds)
                return false;

            // Retrieves float value of currently selected horizontal speed
            result = Call(Handle, () => SdkInstance.GetHSSpeed(adConverter, amplifier, speedIndex, ref locSpeed));
            ThrowIfError(result, nameof(SdkInstance.GetHSSpeed));

            speed = locSpeed;

            return true;

        }
    }
}