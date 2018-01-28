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
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using ANDOR_CS.Exceptions;
using static ANDOR_CS.Classes.AndorSdkInitialization;
using static ANDOR_CS.Exceptions.AndorSdkException;
using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Classes
{
    /// <summary>
    ///     Represents all possible acuisition/Camera settings that can be adjusted before taking any images
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
#if DEBUG
        internal AcquisitionSettings(bool empty = false)
        {
            if (empty) 
                return;

            AcquisitionMode = Enums.AcquisitionMode.SingleScan;
            ADConverter = (0, 16);
            OutputAmplifier = (OutputAmplification.Conventional, "Conventional", 1);
            ExposureTime = 123;
            HSSpeed = (0, 3);
            ImageArea = new Rectangle(1, 1, 128, 256);

            //this.KineticCycle = (12, 23);
            PreAmpGain = (0, "Gain 1");
            ReadoutMode = ReadMode.FullImage;

            TriggerMode = Enums.TriggerMode.Bulb;
            VSAmplitude = Enums.VSAmplitude.Plus2;
            VSSpeed = (0, 0.3f);
        }

#endif
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
        public override List<(string Option, bool Success, uint ReturnCode)> ApplySettings(
            out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) timing)
        {
            var output = new List<(string Option, bool Success, uint ReturnCode)>();

            SafeSdkCameraHandle handle = null;
            if (Camera is Camera locCam)
                handle = locCam.CameraHandle;
            else
                throw new Exception("Type of Camera is wrong.");

            CheckCamera();

            uint result;

            if (VSSpeed.HasValue)
                {
                    result = Call(handle, SDKInstance.SetVSSpeed, VSSpeed.Value.Index);

                    output.Add((nameof(VSSpeed), result == SDK.DRV_SUCCESS, result));
                }

                if (VSAmplitude.HasValue)
                {
                    result = Call(handle, SDKInstance.SetVSAmplitude, (int) VSAmplitude.Value);

                    output.Add((nameof(VSAmplitude), result == SDK.DRV_SUCCESS, result));
                }

                if (ADConverter.HasValue)
                {
                    result = Call(handle, SDKInstance.SetADChannel, ADConverter.Value.Index);

                    output.Add((nameof(ADConverter), result == SDK.DRV_SUCCESS, result));
                }

                if (OutputAmplifier.HasValue)
                {
                    result = Call(handle, SDKInstance.SetOutputAmplifier, OutputAmplifier.Value.Index);

                    output.Add((nameof(OutputAmplifier), result == SDK.DRV_SUCCESS, result));
                }

                if (HSSpeed.HasValue)
                {
                    result = Call(handle,
                        () => SDKInstance.SetHSSpeed(OutputAmplifier?.Item3 ?? 0, HSSpeed.Value.Index));

                    output.Add((nameof(HSSpeed), result == SDK.DRV_SUCCESS, result));
                }

                if (PreAmpGain.HasValue)
                {
                    result = Call(handle, SDKInstance.SetPreAmpGain, PreAmpGain.Value.Index);

                    output.Add((nameof(PreAmpGain), result == SDK.DRV_SUCCESS, result));
                }


                if (ImageArea.HasValue)
                {
                    result = Call(handle,
                        () => SDKInstance.SetImage(1, 1, ImageArea.Value.X1, ImageArea.Value.X2, ImageArea.Value.Y1,
                            ImageArea.Value.Y2));


                    output.Add((nameof(ImageArea), result == SDK.DRV_SUCCESS, result));
                }

                if (AcquisitionMode.HasValue)
                {
                    var mode = AcquisitionMode.Value;

                    if (mode.HasFlag(Enums.AcquisitionMode.FrameTransfer))
                    {
                        result = Call(handle, SDKInstance.SetFrameTransferMode, 1);
                        ThrowIfError(result, nameof(SDKInstance.SetFrameTransferMode));
                        mode ^= Enums.AcquisitionMode.FrameTransfer;

                        output.Add(("FrameTransfer", result == SDK.DRV_SUCCESS, result));
                    }
                    else
                    {
                        ThrowIfError(Call(handle, SDKInstance.SetFrameTransferMode, 0),
                            nameof(SDKInstance.SetFrameTransferMode));
                    }

                    result = Call(handle, SDKInstance.SetAcquisitionMode, EnumConverter.AcquisitionModeTable[mode]);

                    output.Add((nameof(AcquisitionMode), result == SDK.DRV_SUCCESS, result));
                }
                else
                {
                    throw new NullReferenceException("Acquisition mode should be set before applying settings.");
                }


                if (ReadoutMode.HasValue)
                {
                    result = Call(handle, SDKInstance.SetReadMode, EnumConverter.ReadModeTable[ReadoutMode.Value]);

                    output.Add((nameof(ReadoutMode), result == SDK.DRV_SUCCESS, result));
                }
                else
                {
                    throw new NullReferenceException("Read mode should be set before applying settings.");
                }


                if (TriggerMode.HasValue)
                {
                    result = Call(handle, SDKInstance.SetTriggerMode,
                        EnumConverter.TriggerModeTable[TriggerMode.Value]);

                    output.Add((nameof(TriggerMode), result == SDK.DRV_SUCCESS, result));
                }
                else
                {
                    throw new NullReferenceException("Trigger mode should be set before applying settings.");
                }

                if (ExposureTime.HasValue)
                {
                    result = Call(handle, SDKInstance.SetExposureTime, ExposureTime.Value);

                    output.Add((nameof(ExposureTime), result == SDK.DRV_SUCCESS, result));
                }
                else
                {
                    throw new NullReferenceException("Exposure time should be set before applying settings.");
                }

                if (AcquisitionMode.Value.HasFlag(Enums.AcquisitionMode.Accumulation))
                {
                    if (!AccumulateCycle.HasValue)
                        throw new NullReferenceException(
                            $"Accumulation cycle should be set if acquisition mode is {AcquisitionMode.Value}.");

                    result = Call(handle, SDKInstance.SetNumberAccumulations, AccumulateCycle.Value.Frames);
                    output.Add((nameof(AccumulateCycle)+"Number", result == SDK.DRV_SUCCESS, result));


                    result = Call(handle, SDKInstance.SetAccumulationCycleTime, AccumulateCycle.Value.Time);
                    //ThrowIfError(result, nameof(SDKInstance.SetAccumulationCycleTime));
                    output.Add((nameof(AccumulateCycle)+"Time", result == SDK.DRV_SUCCESS, result));
                }


                if (AcquisitionMode.Value.HasFlag(Enums.AcquisitionMode.Kinetic))
                {
                    if (!AccumulateCycle.HasValue)
                        throw new NullReferenceException(
                            $"Accumulation cycle should be set if acquisition mode is {AcquisitionMode.Value}.");
                    if (!KineticCycle.HasValue)
                        throw new NullReferenceException(
                            $"Kinetic cycle should be set if acquisition mode is {AcquisitionMode.Value}.");

                    result = Call(handle, SDKInstance.SetNumberAccumulations, AccumulateCycle.Value.Frames);
                    //ThrowIfError(result, nameof(SDKInstance.SetNumberAccumulations));
                    output.Add((nameof(AccumulateCycle) + "Number", result == SDK.DRV_SUCCESS, result));


                    result = Call(handle, SDKInstance.SetAccumulationCycleTime, AccumulateCycle.Value.Time);
                    //ThrowIfError(result, nameof(SDKInstance.SetAccumulationCycleTime));
                    output.Add((nameof(AccumulateCycle) + "Time", result == SDK.DRV_SUCCESS, result));

                    result = Call(handle, SDKInstance.SetNumberKinetics, KineticCycle.Value.Frames);
                    //ThrowIfError(result, nameof(SDKInstance.SetNumberKinetics));
                    output.Add((nameof(KineticCycle)+"Number", result == SDK.DRV_SUCCESS, result));


                    result = Call(handle, SDKInstance.SetKineticCycleTime, KineticCycle.Value.Time);
                    //ThrowIfError(result, nameof(SDKInstance.SetKineticCycleTime));
                    output.Add((nameof(AccumulateCycle) + "Time", result == SDK.DRV_SUCCESS, result));
                }

                if (EMCCDGain.HasValue)
                {
                    if (!OutputAmplifier.HasValue ||
                        !OutputAmplifier.Value.OutputAmplifier.HasFlag(OutputAmplification.Conventional))
                        throw new NullReferenceException(
                            $"OutputAmplifier should be set to {OutputAmplification.Conventional}");

                    result = Call(handle, SDKInstance.SetEMCCDGain, EMCCDGain.Value);
                    //ThrowIfError(result, nameof(SDKInstance.SetEMCCDGain));
                    output.Add((nameof(EMCCDGain), result == SDK.DRV_SUCCESS, result));
                }

                var expTime = 0f;
                var accTime = 0f;
                var kinTime = 0f;

                result = Call(handle, () => SDKInstance.GetAcquisitionTimings(ref expTime, ref accTime, ref kinTime));
                ThrowIfError(result, nameof(SDKInstance.GetAcquisitionTimings));

                result = Call(handle, SDKInstance.GetSizeOfCircularBuffer, out int size);
                ThrowIfError(result, nameof(SDKInstance.GetSizeOfCircularBuffer));

                timing =
                    (ExposureTime: expTime, AccumulationCycleTime: accTime, KineticCycleTime: kinTime, BufferSize: size
                    );

                Camera.CurrentSettings = this;

                return output;
            
        }


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

                var result = Call(Handle, () => SDKInstance.GetFastestRecommendedVSSpeed(ref speedIndex, ref speedVal));
                ThrowIfError(result, nameof(SDKInstance.GetFastestRecommendedVSSpeed));

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
                var result = Call(Handle, SDKInstance.GetNumberHSSpeeds, adConverter, amplifier, out int nSpeeds);
                ThrowIfError(result, nameof(SDKInstance.GetNumberHSSpeeds));

                // Checks if obtained value is valid
                if (nSpeeds < 0)
                    throw new ArgumentOutOfRangeException(
                        $"Returned number of available speeds is less than 0 ({nSpeeds}).");

                // Iterates through speed indexes
                for (var speedIndex = 0; speedIndex < nSpeeds; speedIndex++)
                {
                    float locSpeed = 0;

                    result = Call(Handle,
                        () => SDKInstance.GetHSSpeed(adConverter, amplifier, speedIndex, ref locSpeed));
                    ThrowIfError(result, nameof(SDKInstance.GetHSSpeed));

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

                    var result = Call(Handle, () => SDKInstance.IsPreAmpGainAvailable(
                        adConverter, amplifier, hsSpeed, gainIndex, ref status));
                    ThrowIfError(result, nameof(SDKInstance.IsPreAmpGainAvailable));

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
            var result = Call(Handle, SDKInstance.GetNumberHSSpeeds, adConverter, amplifier, out int nSpeeds);
            ThrowIfError(result, nameof(SDKInstance.GetNumberHSSpeeds));

            // Checks if speedIndex is in allowed range
            if (speedIndex < 0 || speedIndex >= nSpeeds)
                return false;

            // Retrieves float value of currently selected horizontal speed
            result = Call(Handle, () => SDKInstance.GetHSSpeed(adConverter, amplifier, speedIndex, ref locSpeed));
            ThrowIfError(result, nameof(SDKInstance.GetHSSpeed));

            speed = locSpeed;

            return true;

        }
    }
}