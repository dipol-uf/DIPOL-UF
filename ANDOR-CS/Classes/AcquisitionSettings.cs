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
        ///     Tries to set vertical speed to fastest recommended speed.
        ///     Requires Camera to be active.
        /// </summary>
        /// <exception cref="AndorSdkException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="NotSupportedException" />
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
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
                if (FailIfError(result, nameof(SdkInstance.GetFastestRecommendedVSSpeed), out var except))
                    throw except;

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
                if (FailIfError(result, nameof(SdkInstance.GetNumberHSSpeeds), out var except))
                    throw except;

                // Checks if obtained value is valid
                if (nSpeeds < 0)
                    throw new ArgumentOutOfRangeException(
                        $"Returned number of available speeds is less than 0 ({nSpeeds}).");

                // Iterates through speed indexes
                for (var speedIndex = 0; speedIndex < nSpeeds; speedIndex++)
                {
                    float locSpeed = 0;

                    // ReSharper disable once AccessToModifiedClosure
                    result = Call(Handle,
                        () => SdkInstance.GetHSSpeed(adConverter, amplifier, speedIndex, ref locSpeed));
                    if (FailIfError(result, nameof(SdkInstance.GetHSSpeed), out except))
                        throw except;

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

                    // ReSharper disable once AccessToModifiedClosure
                    var result = Call(Handle, () => SdkInstance.IsPreAmpGainAvailable(
                        adConverter, amplifier, hsSpeed, gainIndex, ref status));
                    if (FailIfError(result, nameof(SdkInstance.IsPreAmpGainAvailable), out var except))
                        throw except;

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
            // Checks if Camera supports horizontal readout speed control
            if (!Camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed))
                return false;

            // Gets the number of availab;e speeds
            var result = Call(Handle, SdkInstance.GetNumberHSSpeeds, adConverter, amplifier, out int nSpeeds);
            if (FailIfError(result, nameof(SdkInstance.GetNumberHSSpeeds), out var except))
                throw except;

            // Checks if speedIndex is in allowed range
            if (speedIndex < 0 || speedIndex >= nSpeeds)
                return false;

            // Retrieves float value of currently selected horizontal speed
            result = Call(Handle, () => SdkInstance.GetHSSpeed(adConverter, amplifier, speedIndex, ref locSpeed));
            if (FailIfError(result, nameof(SdkInstance.GetHSSpeed), out except))
                throw except;

            speed = locSpeed;

            return true;

        }
    }
}