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
using System.Threading.Tasks;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Exceptions;

using SDKInit = ANDOR_CS.AndorSDKInitialization;
using SDK = ATMCD64CS.AndorSDK;

using static ANDOR_CS.Exceptions.AndorSDKException;


namespace ANDOR_CS.Classes
{
    /// <summary>
    /// Represents all possible acuisition/camera settings that can be adjusted before taking any images
    /// </summary>
    public class AcquisitionSettings
    {

        /// <summary>
        /// A reference to the parent <see cref="Camera"/> object.
        /// Used to perform checks of capabilities of the current camera.
        /// </summary>
        private Camera camera = null;


        /// <summary>
        /// Stores the value of currently set vertical speed
        /// </summary>
        public (int Index, float Speed)? VSSpeed
        {
            get;
            private set;
        } = null;

        /// <summary>
        /// Stoers the value of currently set horizontal speed
        /// </summary>
        public (int Index, float Speed)? HSSpeed
        {
            get;
            private set;
        } = null;

        /// <summary>
        /// Stores the index of currently set Analogue-Digital Converter and its bit depth.
        /// </summary>
        public (int Index, int BitDepth)? ADConverter
        {
            get;
            private set;
        } = null;

        /// <summary>
        /// Stores the value of currently set vertical clock voltage amplitude
        /// </summary>
        public VSAmplitude? VSAmplitude
        {
            get;
            private set;
        } = null;

        /// <summary>
        /// Stores type of currentlt set Amplifier
        /// </summary>
        public (string Name, OutputAmplification Amplifier, int Index)? Amplifier
        {
            get;
            private set;
        } = null;

        /// <summary>
        /// Stores type of currently set PreAmp Gain
        /// </summary>
        public (int Index, string Name)? PreAmpGain
        {
            get;
            private set;
        } = null;

        /// <summary>
        /// Stores currently set acquisition mode
        /// </summary>
        public AcquisitionMode? AcquisitionMode
        {
            get;
            private set;
        } = null;

        /// <summary>
        /// Stores currently set read mode
        /// </summary>
        public ReadMode? ReadMode
        {
            get;
            private set;
        }

        /// <summary>
        /// Stores currently set trigger mode
        /// </summary>
        public TriggerMode? TriggerMode
        {
            get;
            private set;
        }

        /// <summary>
        /// Stores exposure time
        /// </summary>
        public float? ExposureTime
        {
            get;
            private set;
        } = null;

        /// <summary>
        /// Stoers seleced image area - part of the CCD from where data should be collected
        /// </summary>
        public Rectangle? ImageArea
        {
            get;
            private set;
        } = null;

        /// <summary>
        /// Constructor adds reference to parent <see cref="Camera"/> object.
        /// </summary>
        /// <param name="camera">Parent object. Camera, to which settings should be applied.</param>
        internal AcquisitionSettings(Camera camera)
        {
            this.camera = camera;
        }
        

        /// <summary>
        /// Checks camera parent <see cref="Camera"/>. If it is not initialized or active, throws exception.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="AndorSDKException"/>
        private void CheckCamera(bool requiresActive = true)
        {
            // Checks if camera object is null
            if (camera == null)
                throw new NullReferenceException("Parent camera is null.");

            // Checks if camera is initialized
            if (!camera.IsInitialized)
                throw new AndorSDKException("Camera is not properly initialized.", null);

            // If speed index is invalid
            if (requiresActive && !camera.IsActive)
                throw new AndorSDKException("Camera is not currently active.", null);
        }

        /// <summary>
        /// Applys currenlty selected settings to the camera.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <returns>Result of application of each non-null setting</returns>
        public List<(string Option, bool Success, uint ReturnCode)> ApplySettings(
            out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime) timing)
        {
            List<(string Option, bool Success, uint ReturnCode)> output = new List<(string Option, bool Success, uint ReturnCode)>();

            CheckCamera(true);

            uint result = 0;


            if (VSSpeed.HasValue)
            {
                result = SDKInit.SDKInstance.SetVSSpeed(VSSpeed.Value.Index);

                output.Add(("VS Speed", result == SDK.DRV_SUCCESS, result));
            }

            if (VSAmplitude.HasValue)
            {
                result = SDKInit.SDKInstance.SetVSAmplitude((int)VSAmplitude);

                output.Add(("VS Amplitude", result == SDK.DRV_SUCCESS, result));
            }

            if (ADConverter.HasValue)
            {
                result = SDKInit.SDKInstance.SetADChannel(ADConverter.Value.Index);

                output.Add(("AD Converter", result == SDK.DRV_SUCCESS, result));
            }

            if (Amplifier.HasValue)
            {
                result = SDKInit.SDKInstance.SetOutputAmplifier(Amplifier.Value.Index);

                output.Add(("Amplifier", result == SDK.DRV_SUCCESS, result));
            }

            if (HSSpeed.HasValue)
            {
                result = SDKInit.SDKInstance.SetHSSpeed(Amplifier?.Item3 ?? 0, HSSpeed.Value.Index);

                output.Add(("HS Speed", result == SDK.DRV_SUCCESS, result));
            }

            if (PreAmpGain.HasValue)
            {
                result = SDKInit.SDKInstance.SetPreAmpGain(PreAmpGain.Value.Index);

                output.Add(("PreAmp Gain", result == SDK.DRV_SUCCESS, result));
            }


            if (ImageArea.HasValue)
            {
                result = SDKInit.SDKInstance.SetImage(1, 1, ImageArea.Value.X1, ImageArea.Value.X2, ImageArea.Value.Y1, ImageArea.Value.Y2);


                output.Add(("Image", result == SDK.DRV_SUCCESS, result));
            }

            if (AcquisitionMode.HasValue)
            {
                result = SDKInit.SDKInstance.SetAcquisitionMode(EnumConverter.AcquisitionModeTable[AcquisitionMode.Value]);

                output.Add(("Acquisition mode", result == SDK.DRV_SUCCESS, result));
            }
            else throw new ArgumentNullException("Acquisition mode should be set before applying settings.");


            if (ReadMode.HasValue)
            {
                result = SDKInit.SDKInstance.SetReadMode(EnumConverter.ReadModeTable[ReadMode.Value]);

                output.Add(("Read mode", result == SDK.DRV_SUCCESS, result));

            }
            else throw new ArgumentNullException("Read mode should be set before applying settings.");


            if (TriggerMode.HasValue)
            {
                result = SDKInit.SDKInstance.SetTriggerMode(EnumConverter.TriggerModeTable[TriggerMode.Value]);

                output.Add(("Trigger mode", result == SDK.DRV_SUCCESS, result));

            }
            else throw new ArgumentNullException("Trigger mode should be set before applying settings.");

            if (ExposureTime.HasValue)
            {
                result = SDKInit.SDKInstance.SetExposureTime(ExposureTime.Value);

                output.Add(("Exposure time", result == SDK.DRV_SUCCESS, result));
            }
            else throw new ArgumentNullException("Exposure time should be set before applying settings.");

            float expTime = 0f;
            float accTime = 0f;
            float kinTime = 0f;

            result = SDKInit.SDKInstance.GetAcquisitionTimings(ref expTime, ref accTime, ref kinTime);
            ThrowIfError(result, nameof(SDKInit.SDKInstance.GetAcquisitionTimings));

            timing = (ExposureTime: expTime, AccumulationCycleTime: accTime, KineticCycleTime: kinTime);

            return output;
        }


        /// <summary>
        /// Tries to set vertical speed. 
        /// Camera may not be active.
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <param name="speedIndex">Index of available speed that corresponds to VSpeed listed in <see cref="Camera.Properties"/>.VSSpeeds</param>
        public void SetVSSpeed(int speedIndex)
        {
            // Checks if Camera is OK
            CheckCamera(false);

            // Checks if camera actually supports changing vertical readout speed
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalReadoutSpeed))
            {
                // Available speeds max index
                int length = camera.Properties.VSSpeeds.Length;

                // If speed index is invalid
                if (speedIndex < 0 || speedIndex >= length)
                    throw new ArgumentOutOfRangeException($"{nameof(speedIndex)} is out of range (should be in [{0},  {length - 1}]).");


                // If success, updates VSSpeed field and 
                VSSpeed = (Index: speedIndex, Speed: camera.Properties.VSSpeeds[speedIndex]);


            }
            else
                throw new NotSupportedException("Camera does not support vertical readout speed control.");

        }

        /// <summary>
        /// Tries to set vertical speed to fastest recommended speed. 
        /// Requires camera to be active.
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        public void SetVSSpeed()
        {
            // Checks if Camera is OK
            CheckCamera(true);

            // Checks if camera actually supports changing vertical readout speed
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalReadoutSpeed))
            {
                // Stores retrieved recommended vertical speed
                int speedIndex = -1;
                float speedVal = 0;

                uint result = SDKInit.SDKInstance.GetFastestRecommendedVSSpeed(ref speedIndex, ref speedVal);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetFastestRecommendedVSSpeed));

                // Available speeds max index
                int length = camera.Properties.VSSpeeds.Length;

                // If speed index is invalid
                if (speedIndex < 0 || speedIndex >= length)
                    throw new ArgumentOutOfRangeException($"Fastest recommended vertical speed index({nameof(speedIndex)}) " +
                        $"is out of range (should be in [{0},  {length - 1}]).");

                // Calls overloaded version of current method with obtained speedIndex as argument
                SetVSSpeed(speedIndex);

            }
            else
                throw new NotSupportedException("Camera does not support vertical readout speed control.");

        }

        /// <summary>
        /// Sets the vertical clock voltage amplitude (if camera supports it).
        /// Camera may be not active.
        /// </summary>
        /// <param name="amplitude">New amplitude </param>
        public void SetVSAmplitude(VSAmplitude amplitude)
        {
            // Checks if Camera is OK
            CheckCamera(false);

            // Checks if Camera supports vertical clock voltage amplitude changes
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalClockVoltage))
            {
                VSAmplitude = amplitude;
            }
            else
                throw new NotSupportedException("Camera does not support vertical clock voltage amplitude control.");
        }

        /// <summary>
        /// Sets Analogue-Digital converter.
        /// Does not require camera to be active
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="converterIndex"></param>
        public void SetADConverter(int converterIndex)
        {
            // Checks if Camera is OK
            CheckCamera(false);

            if (converterIndex < 0 || converterIndex >= camera.Properties.ADConverters.Length)
                throw new ArgumentOutOfRangeException($"AD converter index {converterIndex} if out of range " +
                    $"(should be in [{0}, {camera.Properties.ADConverters.Length - 1}]).");

            ADConverter = (Index: converterIndex, BitDepth: camera.Properties.ADConverters[converterIndex]);

        }

        /// <summary>
        /// Sets output amplifier. 
        /// Does not require camera to be active.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="amplifier"></param>
        public void SetOutputAmplifier(OutputAmplification amplifier)
        {
            // Checks camera object
            CheckCamera(false);

            // Queries available amplifiers, looking for the one, which type mathces input parameter
            var query = from amp
                        in camera.Properties.Amplifiers
                        where amp.Item2 == amplifier
                        select amp;

            // If no mathces found, throws ana excepetion
            if (query.Count() == 0)
                throw new ArgumentOutOfRangeException($"Provided amplifier i sout of range " +
                    $"{(Enum.IsDefined(typeof(OutputAmplification), amplifier) ? Enum.GetName(typeof(OutputAmplification), amplifier) : "Unknown")}.");

            // Otherwise, assigns name and type of the amplifier 
            var element = query.First();

            Amplifier = (Name: element.Item1, Amplifier: element.Item2, Index: camera.Properties.Amplifiers.IndexOf(element));

        }

        /// <summary>
        /// Returns a collection of available Horizonal Readout Speeds for currently selected Amplifier and AD Converter.
        /// Requires camera to be active.
        /// Note: <see cref="AcquisitionSettings.ADConverter"/> amd <see cref="AcquisitionSettings.Amplifier"/> should be set
        /// via <see cref="AcquisitionSettings.SetADConverter(int)"/> and <see cref="AcquisitionSettings.SetOutputAmplifier(OutputAmplification)"/>
        /// before calling this method.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <returns>An enumerable collection of speed indexes and respective speed values available.</returns>
        public IEnumerable<(int Index, float Speed)> GetAvailableHSSpeeds()
        {
            // Checks if camera is OK and is active
            CheckCamera(true);

            // Checks if camera support horizontal speed controls
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed))
            {
                // Checks if AD converter and Amplifier are already selected
                if (ADConverter == null || Amplifier == null)
                    throw new NullReferenceException($"Either AD converter ({nameof(ADConverter)}) or Amplifier ({nameof(Amplifier)}) are not set.");

                // Determines indexes of converter and amplifier
                int channel = ADConverter.Value.Index;
                int amp = Amplifier.Value.Index;
                int nSpeeds = 0;

                // Gets the number of availab;e speeds
                var result = SDKInit.SDKInstance.GetNumberHSSpeeds(channel, amp, ref nSpeeds);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetNumberHSSpeeds));

                // Checks if obtained value is valid
                if (nSpeeds < 0)
                    throw new ArgumentOutOfRangeException($"Returned number of available speeds is less than 0 ({nSpeeds}).");

                // Iterates through speed indexes
                for (int speedIndex = 0; speedIndex < nSpeeds; speedIndex++)
                {
                    float locSpeed = 0;

                    result = SDKInit.SDKInstance.GetHSSpeed(channel, amp, speedIndex, ref locSpeed);
                    ThrowIfError(result, nameof(SDKInit.SDKInstance.GetHSSpeed));

                    // Returns speed index and speed value for evvery subsequent call
                    yield return (Index: speedIndex, Speed: locSpeed);
                }

            }
            else
                throw new NotSupportedException("Camera does not support horizontal readout speed controls.");


        }

        /// <summary>
        /// Sets Horizontal Readout Speed for currently selected Amplifier and AD Converter.
        /// Requires camera to be active.
        /// Note: <see cref="AcquisitionSettings.ADConverter"/> and <see cref="AcquisitionSettings.Amplifier"/> should be set
        /// via <see cref="AcquisitionSettings.SetADConverter(int)"/> and <see cref="AcquisitionSettings.SetOutputAmplifier(OutputAmplification)"/>
        /// before calling this method.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <param name="speedIndex">Index of horizontal speed</param>
        public void SetHSSpeed(int speedIndex)
        {
            // Checks if camera is OK and is active
            CheckCamera(true);

            // Checks if camera supports horizontal readout speed control
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed))
            {
                // Checks if both AD converter and Amplifier are already set
                if (ADConverter == null || Amplifier == null)
                    throw new NullReferenceException($"Either AD converter ({nameof(ADConverter)}) or Amplifier ({nameof(Amplifier)}) are not set.");

                // Determines indexes of converter and amplifier
                int channel = ADConverter.Value.Index;
                int amp = Amplifier.Value.Index;
                int nSpeeds = 0;

                // Gets the number of availab;e speeds
                var result = SDKInit.SDKInstance.GetNumberHSSpeeds(channel, amp, ref nSpeeds);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetNumberHSSpeeds));

                // Checks if speedIndex is in allowed range
                if (speedIndex < 0 || speedIndex >= nSpeeds)
                    throw new ArgumentOutOfRangeException($"Horizontal speed index ({speedIndex}) is out of range (should be in [{0}, {speedIndex - 1}]).");

                float speed = 0;

                // Retrieves float value of currently selected horizontal speed
                result = SDKInit.SDKInstance.GetHSSpeed(channel, amp, speedIndex, ref speed);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetHSSpeed));

                // Assigns speed index and speed value
                HSSpeed = (Index: speedIndex, Speed: speed);

            }
            else
                throw new NotSupportedException("Camera does not support horizontal readout speed controls");
        }

        /// <summary>
        /// Returns a collection of available PreAmp gains for currently selected HSSpeed, Amplifier, Converter.
        /// Requires camera to be active.
        /// Note: <see cref="AcquisitionSettings.ADConverter"/>, <see cref="AcquisitionSettings.HSSpeed"/>
        /// and <see cref="AcquisitionSettings.Amplifier"/> should be set
        /// via <see cref="AcquisitionSettings.SetADConverter(int)"/>, <see cref="AcquisitionSettings.SetHSSpeed(int)"/>
        /// and <see cref="AcquisitionSettings.SetOutputAmplifier(OutputAmplification)"/>.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="NotSupportedException"/>
        /// <returns>Available PreAmp gains</returns>
        public IEnumerable<(int Index, string Name)> GetAvailablePreAmpGain()
        {
            // Checks if camera is OK and is active
            CheckCamera(true);

            // Checks if camera supports PreAmp Gain control
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.PreAmpGain))
            {
                // Check if all required settings are already set
                if (HSSpeed == null || Amplifier == null || ADConverter == null)
                    throw new NullReferenceException($"One of the following settings are not set: AD Converter ({nameof(ADConverter)})," +
                        $"Amplifier ({nameof(Amplifier)}), Vertical Speed ({nameof(VSSpeed)}).");


                // Stores indexes of ADConverter, Amplifier and Horizontal Speed
                int channel = ADConverter.Value.Index;
                int amp = Amplifier.Value.Index;
                int speed = HSSpeed.Value.Index;

                // Total number of gain settings available
                int gainNumber = camera.Properties.PreAmpGains.Length;


                for (int gainIndex = 0; gainIndex < gainNumber; gainIndex++)
                {
                    int status = -1;

                    uint result = SDKInit.SDKInstance.IsPreAmpGainAvailable(channel, amp, speed, gainIndex, ref status);
                    ThrowIfError(result, nameof(SDKInit.SDKInstance.IsPreAmpGainAvailable));

                    // If status of a certain combination of settings is 1, return it
                    if (status == 1)
                        yield return (Index: gainIndex, Name: camera.Properties.PreAmpGains[gainIndex]);
                }

            }
            else
                throw new NotSupportedException("Camera does not support Pre Amp Gain controls.");
        }

        /// <summary>
        /// Sets PreAmp gain for currently selected HSSpeed, Amplifier, Converter.
        /// Requires camera to be active.
        /// Note: <see cref="AcquisitionSettings.ADConverter"/>, <see cref="AcquisitionSettings.HSSpeed"/>
        /// and <see cref="AcquisitionSettings.Amplifier"/> should be set
        /// via <see cref="AcquisitionSettings.SetADConverter(int)"/>, <see cref="AcquisitionSettings.SetHSSpeed(int)"/>
        /// and <see cref="AcquisitionSettings.SetOutputAmplifier(OutputAmplification)"/>.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="gainIndex">Index of gain</param>
        public void SetPreAmpGain(int gainIndex)
        {
            // Checks if camera is OK and is active
            CheckCamera(true);

            // Checks if camera supports PreAmp Gain control
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.PreAmpGain))
            {
                // Check if all required settings are already set
                if (HSSpeed == null || Amplifier == null || ADConverter == null)
                    throw new NullReferenceException($"One of the following settings are not set: AD Converter ({nameof(ADConverter)})," +
                        $"Amplifier ({nameof(Amplifier)}), Vertical Speed ({nameof(VSSpeed)}).");


                // Stores indexes of ADConverter, Amplifier and Horizontal Speed
                int channel = ADConverter.Value.Index;
                int amp = Amplifier.Value.Index;
                int speed = HSSpeed.Value.Index;

                // Total number of gain settings available
                int gainNumber = camera.Properties.PreAmpGains.Length;

                // Checks if argument is in valid range
                if (gainIndex < 0 || gainIndex >= gainNumber)
                    throw new ArgumentOutOfRangeException($"Gain index (nameof{gainIndex}) is out of range (should be in [{0}, {gainNumber}]).");

                int status = -1;

                uint result = SDKInit.SDKInstance.IsPreAmpGainAvailable(channel, amp, speed, gainIndex, ref status);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.IsPreAmpGainAvailable));

                if (status != 1)
                    throw new ArgumentOutOfRangeException($"Pre amp gain index ({gainIndex}) is out of range.");

                PreAmpGain = (Index: gainIndex, Name: camera.Properties.PreAmpGains[gainIndex]);
            }
        }

        /// <summary>
        /// Sets acquisition mode. 
        /// Camera may be inactive.
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <param name="mode">Acquisition mode</param>
        public void SetAcquisitionMode(AcquisitionMode mode)
        {
            // Checks if camera is OK
            CheckCamera(false);

            // Checks if camera supports specifed mode
            if (!camera.Capabilities.AcquisitionModes.HasFlag(mode))
                throw new NotSupportedException($"Camera does not support specified regime ({Extensions.GetEnumNames(typeof(AcquisitionMode), mode).FirstOrDefault()})");

            // If there are no matches in the pre-defined table, then this mode cannot be set explicitly
            if (!EnumConverter.AcquisitionModeTable.ContainsKey(mode))
                throw new InvalidOperationException($"Cannot explicitly set provided acquisition mode ({Extensions.GetEnumNames(typeof(AcquisitionMode), mode).FirstOrDefault()})");

            AcquisitionMode = mode;
        }

        /// <summary>
        /// Sets trigger mode. 
        /// Camera may be inactive.
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <param name="mode">Trigger mode</param>
        public void SetTriggerMode(TriggerMode mode)
        {
            // Checks if camera is OK
            CheckCamera(false);

            // Checks if camera supports specifed mode
            if (!camera.Capabilities.TriggerModes.HasFlag(mode))
                throw new NotSupportedException($"Camera does not support specified regime ({Extensions.GetEnumNames(typeof(TriggerMode), mode).FirstOrDefault()})");

            // If there are no matches in the pre-defined table, then this mode cannot be set explicitly
            if (!EnumConverter.TriggerModeTable.ContainsKey(mode))
                throw new InvalidOperationException($"Cannot explicitly set provided acquisition mode ({Extensions.GetEnumNames(typeof(TriggerMode), mode).FirstOrDefault()})");

            TriggerMode = mode;
        }

        /// <summary>
        /// Sets read mode.
        /// Camera may be inactive.
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <param name="mode">Read mode</param>
        public void SetReadoutMode(ReadMode mode)
        {
            // Checks camera
            CheckCamera(false);

            // Checks if camera support read mode control
            if(!camera.Capabilities.ReadModes.HasFlag(mode))
                throw new NotSupportedException($"Camera does not support specified regime ({Extensions.GetEnumNames(typeof(ReadMode), mode).FirstOrDefault()})");

            // If there are no matches in the pre-defiend table, then this mode cannot be set explicitly
            if (!EnumConverter.ReadModeTable.ContainsKey(mode))
                throw new InvalidOperationException($"Cannot explicitly set provided acquisition mode ({Extensions.GetEnumNames(typeof(ReadMode), mode).FirstOrDefault()})");


            ReadMode = mode;
        }

        /// <summary>
        /// Sets exposure time.
        /// Camera may be inactive.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="time">Exposure time</param>
        public void SetExposureTime(float time)
        {
            // If time is negative throws exception
            if (time < 0)
                throw new ArgumentOutOfRangeException($"Exposure time cannot be less than 0 (provided {time}).");


            ExposureTime = time;
        }

        /// <summary>
        /// Sets image area.
        /// Camera may be inactive.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="area">Image rectangle</param>
        public void SetImageArea(Rectangle area)
        {
            CheckCamera(false);

            if (area.X1 <= 0 || area.Y1 <= 0)
                throw new ArgumentOutOfRangeException($"Start position of rectangel cannot be to the lower-left of {new Point2D(1,1)} (provided {area.Start}).");



            if (camera.Capabilities.GetFunctions.HasFlag(GetFunction.DetectorSize))
            {
                var size = camera.Properties.DetectorSize;

                if (area.X2 > size.Horizontal)
                    throw new ArgumentOutOfRangeException($"Right boundary exceeds CCD size ({area.X2} >= {size.Horizontal}).");

                if(area.Y2 > size.Vertical)
                    throw new ArgumentOutOfRangeException($"Top boundary exceeds CCD size ({area.Y2} >= {size.Vertical}).");
            }

            ImageArea = area;
        }
    }
}
