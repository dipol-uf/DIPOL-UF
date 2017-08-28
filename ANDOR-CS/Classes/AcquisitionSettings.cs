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
using System.Xml.Linq;
using System.IO;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Exceptions;
using ANDOR_CS.Interfaces;

using SDKInit = ANDOR_CS.Classes.AndorSDKInitialization;
using SDK = ATMCD64CS.AndorSDK;

using static ANDOR_CS.Exceptions.AndorSDKException;

namespace ANDOR_CS.Classes
{

    /// <summary>
    /// Represents all possible acuisition/camera settings that can be adjusted before taking any images
    /// </summary>
     public class AcquisitionSettings : ISettings
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
        /// Stores the value of currently set horizontal speed
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
                
        public (int Frames, float Time)? AccumulateCycle
        {
            get;
            private set;
        } = null;

        public (int Frames, float Time)? KineticCycle
        {
            get;
            private set;
        } = null;

        public int? EMCCDGain
        {
            get;
            private set;
        }

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
        private void CheckCamera()
        {
            // Checks if camera object is null
            if (camera == null)
                throw new NullReferenceException("Camera is null.");

            // Checks if camera is initialized
            if (!camera.IsInitialized)
                throw new AndorSDKException("Camera is not properly initialized.", null);

        }

        /// <summary>
        /// Applys currenlty selected settings to the camera.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <returns>Result of application of each non-null setting</returns>
        public List<(string Option, bool Success, uint ReturnCode)> ApplySettings(
            out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) timing)
        {
            List<(string Option, bool Success, uint ReturnCode)> output = new List<(string Option, bool Success, uint ReturnCode)>();

            CheckCamera();
            try
            {
                camera.SetActiveAndLock();

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
                    var mode = AcquisitionMode.Value;

                    if (mode.HasFlag(Enums.AcquisitionMode.FrameTransfer))
                    {
                        result = SDKInit.SDKInstance.SetFrameTransferMode(1);
                        ThrowIfError(result, nameof(SDKInit.SDKInstance.SetFrameTransferMode));
                        mode ^= Enums.AcquisitionMode.FrameTransfer;

                        output.Add(("Frame transfer", result == SDK.DRV_SUCCESS, result));
                    }
                    else
                        ThrowIfError(SDKInit.SDKInstance.SetFrameTransferMode(0), nameof(SDKInit.SDKInstance.SetFrameTransferMode));

                    result = SDKInit.SDKInstance.SetAcquisitionMode(EnumConverter.AcquisitionModeTable[mode]);

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

                if (AcquisitionMode.Value.HasFlag(Enums.AcquisitionMode.Accumulation))
                {
                    if (!AccumulateCycle.HasValue)
                        throw new ArgumentNullException($"Accumulation cycle should be set if acquisition mode is {AcquisitionMode.Value}.");

                    result = SDKInit.SDKInstance.SetNumberAccumulations(AccumulateCycle.Value.Frames);
                    // ThrowIfError(result, nameof(SDKInit.SDKInstance.SetNumberAccumulations));
                    output.Add(("Number of accumulations", result == SDK.DRV_SUCCESS, result));


                    result = SDKInit.SDKInstance.SetAccumulationCycleTime(AccumulateCycle.Value.Time);
                    //ThrowIfError(result, nameof(SDKInit.SDKInstance.SetAccumulationCycleTime));
                    output.Add(("Accumulation cycle time", result == SDK.DRV_SUCCESS, result));

                }


                if (AcquisitionMode.Value.HasFlag(Enums.AcquisitionMode.Kinetic))
                {
                    if (!AccumulateCycle.HasValue)
                        throw new ArgumentNullException($"Accumulation cycle should be set if acquisition mode is {AcquisitionMode.Value}.");
                    if (!KineticCycle.HasValue)
                        throw new ArgumentNullException($"Kinetic cycle should be set if acquisition mode is {AcquisitionMode.Value}.");

                    result = SDKInit.SDKInstance.SetNumberAccumulations(AccumulateCycle.Value.Frames);
                    //ThrowIfError(result, nameof(SDKInit.SDKInstance.SetNumberAccumulations));
                    output.Add(("Number of accumulations", result == SDK.DRV_SUCCESS, result));


                    result = SDKInit.SDKInstance.SetAccumulationCycleTime(AccumulateCycle.Value.Time);
                    //ThrowIfError(result, nameof(SDKInit.SDKInstance.SetAccumulationCycleTime));
                    output.Add(("Accumulation cycle time", result == SDK.DRV_SUCCESS, result));

                    result = SDKInit.SDKInstance.SetNumberKinetics(KineticCycle.Value.Frames);
                    //ThrowIfError(result, nameof(SDKInit.SDKInstance.SetNumberKinetics));
                    output.Add(("Number of kinetics", result == SDK.DRV_SUCCESS, result));


                    result = SDKInit.SDKInstance.SetKineticCycleTime(KineticCycle.Value.Time);
                    //ThrowIfError(result, nameof(SDKInit.SDKInstance.SetKineticCycleTime));
                    output.Add(("Kinetic cycle time", result == SDK.DRV_SUCCESS, result));
                }

                if (EMCCDGain.HasValue)
                {
                    if (!Amplifier.HasValue || !Amplifier.Value.Amplifier.HasFlag(OutputAmplification.Conventional))
                        throw new ArgumentNullException($"Amplifier should be set to {OutputAmplification.Conventional}");

                    result = SDKInit.SDKInstance.SetEMCCDGain(EMCCDGain.Value);
                    //ThrowIfError(result, nameof(SDKInit.SDKInstance.SetEMCCDGain));
                    output.Add(("EMCCDGain", result == SDK.DRV_SUCCESS, result));
                }

                float expTime = 0f;
                float accTime = 0f;
                float kinTime = 0f;
                int size = 0;

                result = SDKInit.SDKInstance.GetAcquisitionTimings(ref expTime, ref accTime, ref kinTime);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetAcquisitionTimings));

                result = SDKInit.SDKInstance.GetSizeOfCircularBuffer(ref size);
                ThrowIfError(result, nameof(SDKInit.SDKInstance.GetSizeOfCircularBuffer));

                timing = (ExposureTime: expTime, AccumulationCycleTime: accTime, KineticCycleTime: kinTime, BufferSize: size);

                camera.CurrentSettings = this;

                return output;
            }
            finally
            {
                camera.ReleaseLock();

            }
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
            CheckCamera();

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
            CheckCamera();
            try
            {
                camera.SetActiveAndLock();

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
            finally
            {
                camera.ReleaseLock();
            }
        }

        /// <summary>
        /// Sets the vertical clock voltage amplitude (if camera supports it).
        /// Camera may be not active.
        /// </summary>
        /// <param name="amplitude">New amplitude </param>
        public void SetVSAmplitude(VSAmplitude amplitude)
        {
            // Checks if Camera is OK
            CheckCamera();

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
            CheckCamera();

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
            CheckCamera();

            // Queries available amplifiers, looking for the one, which type mathces input parameter
            var query = from amp
                        in camera.Properties.Amplifiers
                        where amp.Item2 == amplifier
                        select amp;

            // If no mathces found, throws an exception
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
        /// Note: <see cref="AcquisitionSettings.ADConverter"/> and <see cref="AcquisitionSettings.Amplifier"/> should be set
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
            CheckCamera();
            try
            {
                camera.SetActiveAndLock();

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
            finally
            {
                camera.ReleaseLock();
            }
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
            CheckCamera();

            try
            {
                camera.SetActiveAndLock();
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
            finally
            {
                camera.ReleaseLock();
            }
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
            CheckCamera();

            try
            {
                camera.SetActiveAndLock();

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
            finally
            {
                camera.ReleaseLock();
            }
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
            CheckCamera();

            try
            {
                camera.SetActiveAndLock();

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
            finally
            {
                camera.ReleaseLock();
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
            CheckCamera();
                       

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
            CheckCamera();

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
            CheckCamera();

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
            CheckCamera();

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
            CheckCamera();

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

        public void SetEMCCDGain(int gain)
        {
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.EMCCDGain))
            {

                if (!Amplifier.HasValue || !Amplifier.Value.Amplifier.HasFlag(OutputAmplification.ElectronMultiplication))
                    throw new NullReferenceException($"Amplifier should be set to {OutputAmplification.Conventional} before accessing EMCCDGain.");

                var range = camera.Properties.EMCCDGainRange;

                if (gain > range.High || gain < range.Low)
                    throw new ArgumentOutOfRangeException($"Gain is out of range. (Provided value {gain} should be in [{range.Low}, {range.High}].)");

                EMCCDGain = gain;
            }
            else
                throw new NotSupportedException("EM CCD Gain feature is not supported.");
        }

        public void SetAccumulationCycle(int number, float time)
        {

            CheckCamera();

            if (!AcquisitionMode.HasValue ||
                AcquisitionMode.Value != Enums.AcquisitionMode.Accumulation &&
                AcquisitionMode.Value != Enums.AcquisitionMode.Kinetic &&
                AcquisitionMode.Value != Enums.AcquisitionMode.FastKinetics)
                throw new ArgumentException($"Current {nameof(AcquisitionMode)} ({AcquisitionMode}) does not support accumulation.");

            ThrowIfError(SDKInit.SDKInstance.SetNumberAccumulations(number), nameof(SDKInit.SDKInstance.SetNumberAccumulations));
            ThrowIfError(SDKInit.SDKInstance.SetAccumulationCycleTime(time), nameof(SDKInit.SDKInstance.SetAccumulationCycleTime));

            AccumulateCycle = (Frames: number, Time: time);

        }

        public void SetKineticCycle(int number, float time)
        {
            CheckCamera();

            if (!AcquisitionMode.HasValue ||
                AcquisitionMode.Value != Enums.AcquisitionMode.RunTillAbort &&
                AcquisitionMode.Value != Enums.AcquisitionMode.Kinetic &&
                AcquisitionMode.Value != Enums.AcquisitionMode.FastKinetics)
                throw new ArgumentException($"Current {nameof(AcquisitionMode)} ({AcquisitionMode}) does not support kinetic cycle.");

            ThrowIfError(SDKInit.SDKInstance.SetNumberKinetics(number), nameof(SDKInit.SDKInstance.SetNumberKinetics));
            ThrowIfError(SDKInit.SDKInstance.SetKineticCycleTime(time), nameof(SDKInit.SDKInstance.SetKineticCycleTime));

            KineticCycle = (Frames: number, Time: time);

        }

        public void Serialize(Stream stream, string name = "")
        {
            using (var str = System.Xml.XmlWriter.Create(
                stream,
                new System.Xml.XmlWriterSettings()
                { Indent = true,
                IndentChars = "\t"}))
            {
                str.WriteStartDocument();

                str.WriteStartElement("AcquisitionSettings");
                str.WriteAttributeString("Camera", camera?.CameraModel ?? "TEST");
                str.WriteAttributeString("Name", name);


                var elementsToWrite = from prop
                                      in this.GetType().GetProperties()
                                      where prop.GetMethod.IsPublic && prop.SetMethod.IsPrivate
                                      select prop;

                elementsToWrite = from prop
                                   in elementsToWrite
                                   where prop.GetMethod.Invoke(this, new object[] { }) != null
                                   select prop;

                foreach (var element in elementsToWrite)
                {
                    str.WriteStartElement(element.Name);

                    var t = element.PropertyType;
                    if (element.PropertyType.Name.Contains("Nullable"))
                        t = t.GenericTypeArguments[0];

                    if (t.Name.Contains("ValueTuple"))
                    {
                        str.WriteAttributeString("Tuple", "True");
                        var f = t.GetFields();
                        str.WriteAttributeString("TupleSize", f.Length.ToString());

                        var tupleNames = ((System.Collections.ICollection)
                            element
                            .CustomAttributes
                            .First()
                            .ConstructorArguments
                            .First()
                            .Value)
                        .Cast<System.Reflection.CustomAttributeTypedArgument>()
                        .Select((arg) => (string)arg.Value)
                        .ToArray();

                        var elementVal = element.GetValue(this);

                        var vals = elementVal.GetType().GetFields().Select((v) => v.GetValue(elementVal)).ToArray();

                        for (int i = 0; i < f.Length; i++)
                        {
                            str.WriteStartElement("TupleElement");
                            str.WriteAttributeString("FieldName", tupleNames[i]);
                            str.WriteAttributeString("FieldType", f[i].FieldType.ToString());
                            str.WriteValue("\r\n\t\t\t" + vals[i].ToString() + "\r\n\t\t");
                            
                            str.WriteEndElement();
                                
                        }
                    }
                    else
                    {
                        str.WriteAttributeString("Tuple", "False");
                        str.WriteAttributeString("Type", element.PropertyType.GenericTypeArguments.First().ToString());
                        str.WriteValue("\r\n\t\t" + element.GetValue(this).ToString() + "\r\n\t");
                    }


                   
                    str.WriteEndElement();
           
                }

                str.WriteEndElement();

                str.WriteEndDocument();
            }
        }

        public void Deserialize(Stream stream)
        {
            try
            {
                camera.SetActiveAndLock();

                XDocument doc = XDocument.Load(stream);

                if (doc.Elements().Count() < 1)
                    throw new Exception();

                var root = doc.Elements().First();

                var cameraName = root.Attribute(XName.Get("Camera"))?.Value;
                var settingsName = root.Attribute(XName.Get("Name"))?.Value;

                XElement element;
                var query = root.Elements().Where(e => e.Name == "VSSpeed");
                if (query.Count() == 1)
                {
                    element = query.First();

                    var value = element.Elements().Where(e => e.Attribute(XName.Get("FieldName")).Value == "Index")
                        .FirstOrDefault()
                        ?.Value
                        .Trim();
                    if (value != null)
                    {
                        int index = int.Parse(value, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo);

                        SetVSSpeed(index);
                    }
                }


                if ((query = root.Elements().Where(e => e.Name == "VSAmplitude")).Count() == 1)
                {
                    var value = query.First()?.Value;

                    if (value != null)
                    {
                        Enums.VSAmplitude vsAmp = (Enums.VSAmplitude)Enum.Parse(typeof(Enums.VSAmplitude), value);

                        SetVSAmplitude(vsAmp);
                    }
                }

                if ((query = root.Elements().Where(e => e.Name == "ADConverter")).Count() == 1)
                {
                    element = query.First();

                    var value = element.Elements().Where(e => e.Attribute(XName.Get("FieldName")).Value == "Index")
                        .FirstOrDefault()
                        ?.Value
                        .Trim();
                    if (value != null)
                    {
                        int index = int.Parse(value, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo);

                        SetADConverter(index);
                    }
                }

                if ((query = root.Elements().Where(e => e.Name == "Amplifier")).Count() == 1)
                {
                    element = query.First();

                    var value = element.Elements().Where(e => e.Attribute(XName.Get("FieldName")).Value == "Amplifier")
                        .FirstOrDefault()
                        ?.Value
                        .Trim();
                    if (value != null)
                    {
                        var amp = (Enums.OutputAmplification)Enum.Parse(typeof(Enums.OutputAmplification), value);

                        SetOutputAmplifier(amp);
                    }
                }


                if ((query = root.Elements().Where(e => e.Name == "HSSpeed")).Count() == 1)
                {
                    element = query.First();

                    var value = element.Elements().Where(e => e.Attribute(XName.Get("FieldName")).Value == "Index")
                        .FirstOrDefault()
                        ?.Value
                        .Trim();
                    if (value != null)
                    {
                        int index = int.Parse(value, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo);

                        SetHSSpeed(index);
                    }
                }

                if ((query = root.Elements().Where(e => e.Name == "PreAmpGain")).Count() == 1)
                {
                    element = query.First();

                    var value = element.Elements().Where(e => e.Attribute(XName.Get("FieldName")).Value == "Index")
                        .FirstOrDefault()
                        ?.Value
                        .Trim();
                    if (value != null)
                    {
                        int index = int.Parse(value, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo);

                        SetPreAmpGain(index);
                    }
                }

                if ((query = root.Elements().Where(e => e.Name == "AcquisitionMode")).Count() == 1)
                {
                    var value = query.First()?.Value;

                    if (value != null)
                    {
                        var mode = (Enums.AcquisitionMode)Enum.Parse(typeof(Enums.AcquisitionMode), value);

                        SetAcquisitionMode(mode);
                    }
                }

                if ((query = root.Elements().Where(e => e.Name == "ReadMode")).Count() == 1)
                {
                    var value = query.First()?.Value;

                    if (value != null)
                    {
                        var mode = (Enums.ReadMode)Enum.Parse(typeof(Enums.ReadMode), value);

                        SetReadoutMode(mode);
                    }
                }

                if ((query = root.Elements().Where(e => e.Name == "TriggerMode")).Count() == 1)
                {
                    var value = query.First()?.Value;

                    if (value != null)
                    {
                        var mode = (Enums.TriggerMode)Enum.Parse(typeof(Enums.TriggerMode), value);

                        SetTriggerMode(mode);
                    }
                }

                if ((query = root.Elements().Where(e => e.Name == "ExposureTime")).Count() == 1)
                {
                    var value = query.First()?.Value;

                    if (value != null)
                    {
                        var time = System.Single.Parse(value, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo);

                        SetExposureTime(time);
                    }
                }

                if ((query = root.Elements().Where(e => e.Name == "ImageArea")).Count() == 1)
                {
                    var value = query.First()?.Value;

                    if (value != null)
                    {
                        var rect = Rectangle.Parse(value);

                        SetImageArea(rect);
                    }
                }

                if ((query = root.Elements().Where(e => e.Name == "AccumulateCycle")).Count() == 1)
                {
                    element = query.First();

                    var value1 = element.Elements().Where(e => e.Attribute(XName.Get("FieldName")).Value == "Frames")
                        .FirstOrDefault()
                        ?.Value
                        .Trim();

                    var value2 = element.Elements().Where(e => e.Attribute(XName.Get("FieldName")).Value == "Time")
                        .FirstOrDefault()
                        ?.Value
                        .Trim();

                    if (value1 != null && value2 != null)
                    {
                        int number = int.Parse(value1, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo);
                        float time = System.Single.Parse(value2, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo);

                        SetAccumulationCycle(number, time);
                    }


                }

                if ((query = root.Elements().Where(e => e.Name == "KineticCycle")).Count() == 1)
                {
                    element = query.First();

                    var value1 = element.Elements().Where(e => e.Attribute(XName.Get("FieldName")).Value == "Frames")
                        .FirstOrDefault()
                        ?.Value
                        .Trim();

                    var value2 = element.Elements().Where(e => e.Attribute(XName.Get("FieldName")).Value == "Time")
                        .FirstOrDefault()
                        ?.Value
                        .Trim();

                    if (value1 != null && value2 != null)
                    {
                        int number = int.Parse(value1, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo);
                        float time = System.Single.Parse(value2, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo);

                        SetKineticCycle(number, time);
                    }


                }
            }
            finally
            {
                camera.ReleaseLock();
            }
        }

        /// <summary>
        /// SERIALIZATION TEST CONSTRUCTOR; DO NOT USE
        /// </summary>
        internal AcquisitionSettings()
        {
            this.AccumulateCycle = (1, 10f);
            this.AcquisitionMode = Enums.AcquisitionMode.FastKinetics;
            this.ADConverter = (0, 16);
            this.Amplifier = ("Amp", OutputAmplification.Conventional, 0);
            this.ExposureTime = 123;
            this.HSSpeed = (0, 12);
            this.ImageArea = new Rectangle(1, 1, 128, 256);

            this.KineticCycle = (12, 23);
            this.PreAmpGain = (0, "Gain");
            this.ReadMode = Enums.ReadMode.MultiTrack;

            this.TriggerMode = Enums.TriggerMode.Bulb;
            this.VSAmplitude = Enums.VSAmplitude.Plus2;
            this.VSSpeed = (0, 500);
        }

    }
}
