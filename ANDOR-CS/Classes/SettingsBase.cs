using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Exceptions;

namespace ANDOR_CS.Classes
{
    public abstract class SettingsBase
    {
        protected CameraBase camera = null;

        public int CameraIndex
            => camera.CameraIndex;

        /// <summary>
        /// Stores the value of currently set vertical speed
        /// </summary>
        public (int Index, float Speed)? VSSpeed
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stores the value of currently set horizontal speed
        /// </summary>
        public (int Index, float Speed)? HSSpeed
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stores the index of currently set Analogue-Digital Converter and its bit depth.
        /// </summary>
        public (int Index, int BitDepth)? ADConverter
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stores the value of currently set vertical clock voltage amplitude
        /// </summary>
        public VSAmplitude? VSAmplitude
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stores type of currentlt set Amplifier
        /// </summary>
        public (string Name, OutputAmplification Amplifier, int Index)? Amplifier
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stores type of currently set PreAmp Gain
        /// </summary>
        public (int Index, string Name)? PreAmpGain
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stores currently set acquisition mode
        /// </summary>
        public AcquisitionMode? AcquisitionMode
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stores currently set read mode
        /// </summary>
        public ReadMode? ReadMode
        {
            get;
            protected set;
        }

        /// <summary>
        /// Stores currently set trigger mode
        /// </summary>
        public TriggerMode? TriggerMode
        {
            get;
            protected set;
        }

        /// <summary>
        /// Stores exposure time
        /// </summary>
        public float? ExposureTime
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stoers seleced image area - part of the CCD from where data should be collected
        /// </summary>
        public Rectangle? ImageArea
        {
            get;
            protected set;
        } = null;

        public (int Frames, float Time)? AccumulateCycle
        {
            get;
            protected set;
        } = null;

        public (int Frames, float Time)? KineticCycle
        {
            get;
            protected set;
        } = null;

        public int? EMCCDGain
        {
            get;
            protected set;
        }

        public abstract List<(string Option, bool Success, uint ReturnCode)> ApplySettings(
            out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) timing);


        /// <summary>
        /// Tries to set vertical speed. 
        /// Camera may not be active.
        /// </summary>
        /// <exception cref="AndorSDKException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <param name="speedIndex">Index of available speed that corresponds to VSpeed listed in <see cref="Camera.Properties"/>.VSSpeeds</param>
        public virtual void SetVSSpeed(int speedIndex)
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
        /// Sets the vertical clock voltage amplitude (if camera supports it).
        /// Camera may be not active.
        /// </summary>
        /// <param name="amplitude">New amplitude </param>
        public virtual void SetVSAmplitude(VSAmplitude amplitude)
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
        public virtual void SetADConverter(int converterIndex)
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
        public virtual void SetOutputAmplifier(OutputAmplification amplifier)
        {
            // Checks camera object
            CheckCamera();

            // Queries available amplifiers, looking for the one, which type mathces input parameter
            var query = from amp
                        in camera.Properties.Amplifiers
                        where amp.Amplifier == amplifier
                        select amp;

            // If no mathces found, throws an exception
            if (query.Count() == 0)
                throw new ArgumentOutOfRangeException($"Provided amplifier i sout of range " +
                    $"{(Enum.IsDefined(typeof(OutputAmplification), amplifier) ? Enum.GetName(typeof(OutputAmplification), amplifier) : "Unknown")}.");

            // Otherwise, assigns name and type of the amplifier 
            var element = query.First();

            Amplifier = (Name: element.Name, Amplifier: element.Amplifier, Index: camera.Properties.Amplifiers.IndexOf(element));
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
        public abstract IEnumerable<(int Index, float Speed)> GetAvailableHSSpeeds();

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
        public virtual void SetHSSpeed(int speedIndex)
        {
            // Checks if camera is OK and is active
            CheckCamera();

            //try
            //{
            //    (camera as Camera).SetActiveAndLock();
            // Checks if camera supports horizontal readout speed control
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed)
                && IsHSSpeedSupported(speedIndex, out float speed))
                HSSpeed = (Index: speedIndex, Speed: speed);
            //{
            //    // Checks if both AD converter and Amplifier are already set
            //    if (ADConverter == null || Amplifier == null)
            //        throw new NullReferenceException($"Either AD converter ({nameof(ADConverter)}) or Amplifier ({nameof(Amplifier)}) are not set.");

            //    // Determines indexes of converter and amplifier
            //    int channel = ADConverter.Value.Index;
            //    int amp = Amplifier.Value.Index;
            //    int nSpeeds = 0;

            //    // Gets the number of availab;e speeds
            //    var result = SDKInit.SDKInstance.GetNumberHSSpeeds(channel, amp, ref nSpeeds);
            //    ThrowIfError(result, nameof(SDKInit.SDKInstance.GetNumberHSSpeeds));

            //    // Checks if speedIndex is in allowed range
            //    if (speedIndex < 0 || speedIndex >= nSpeeds)
            //        throw new ArgumentOutOfRangeException($"Horizontal speed index ({speedIndex}) is out of range (should be in [{0}, {speedIndex - 1}]).");

            //    float speed = 0;

            //    // Retrieves float value of currently selected horizontal speed
            //    result = SDKInit.SDKInstance.GetHSSpeed(channel, amp, speedIndex, ref speed);
            //    ThrowIfError(result, nameof(SDKInit.SDKInstance.GetHSSpeed));

            //    // Assigns speed index and speed value
            //    HSSpeed = (Index: speedIndex, Speed: speed);

            //}
            else
                throw new NotSupportedException("Camera does not support horizontal readout speed controls");
            //}
            //finally
            //{
            //    (camera as Camera).ReleaseLock();
            //}

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
        public abstract IEnumerable<(int Index, string Name)> GetAvailablePreAmpGain();

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
        public virtual void SetPreAmpGain(int gainIndex)
        {
            // Checks if camera is OK and is active
            CheckCamera();           

            // Checks if camera supports PreAmp Gain control
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.PreAmpGain))
            {
                // Check if all required settings are already set
                if (HSSpeed == null || Amplifier == null || ADConverter == null)
                    throw new NullReferenceException($"One of the following settings are not set: AD Converter ({nameof(ADConverter)})," +
                        $"Amplifier ({nameof(Amplifier)}), Vertical Speed ({nameof(VSSpeed)}).");

                // Total number of gain settings available
                int gainNumber = camera.Properties.PreAmpGains.Length;

                // Checks if argument is in valid range
                if (gainIndex < 0 || gainIndex >= gainNumber)
                    throw new ArgumentOutOfRangeException($"Gain index (nameof{gainIndex}) is out of range (should be in [{0}, {gainNumber}]).");

                if (GetAvailablePreAmpGain().Where(item => item.Index == gainIndex).Count() > 0)
                    PreAmpGain = (Index: gainIndex, Name: camera.Properties.PreAmpGains[gainIndex]);
                else
                    throw new ArgumentOutOfRangeException($"Pre amp gain index ({gainIndex}) is out of range.");

            }
            
        }

        /// <summary>
        /// Sets acquisition mode. 
        /// Camera may be inactive.
        /// </summary>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <param name="mode">Acquisition mode</param>
        public virtual void SetAcquisitionMode(AcquisitionMode mode)
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
        public virtual void SetTriggerMode(TriggerMode mode)
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
        public virtual void SetReadoutMode(ReadMode mode)
        {
            // Checks camera
            CheckCamera();

            // Checks if camera support read mode control
            if (!camera.Capabilities.ReadModes.HasFlag(mode))
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
        public virtual void SetExposureTime(float time)
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
        public virtual void SetImageArea(Rectangle area)
        {
            CheckCamera();

            if (area.X1 <= 0 || area.Y1 <= 0)
                throw new ArgumentOutOfRangeException($"Start position of rectangel cannot be to the lower-left of {new Point2D(1, 1)} (provided {area.Start}).");



            if (camera.Capabilities.GetFunctions.HasFlag(GetFunction.DetectorSize))
            {
                var size = camera.Properties.DetectorSize;

                if (area.X2 > size.Horizontal)
                    throw new ArgumentOutOfRangeException($"Right boundary exceeds CCD size ({area.X2} >= {size.Horizontal}).");

                if (area.Y2 > size.Vertical)
                    throw new ArgumentOutOfRangeException($"Top boundary exceeds CCD size ({area.Y2} >= {size.Vertical}).");
            }

            ImageArea = area;
        }

        public virtual void SetEMCCDGain(int gain)
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

        public virtual void SetAccumulationCycle(int number, float time)
        {
            CheckCamera();

            if (!AcquisitionMode.HasValue ||
                AcquisitionMode.Value != Enums.AcquisitionMode.Accumulation &&
                AcquisitionMode.Value != Enums.AcquisitionMode.Kinetic &&
                AcquisitionMode.Value != Enums.AcquisitionMode.FastKinetics)
                throw new ArgumentException($"Current {nameof(AcquisitionMode)} ({AcquisitionMode}) does not support accumulation.");

            AccumulateCycle = (Frames: number, Time: time);
        }

        public virtual void SetKineticCycle(int number, float time)
        {
            CheckCamera();

            if (!AcquisitionMode.HasValue ||
                AcquisitionMode.Value != Enums.AcquisitionMode.RunTillAbort &&
                AcquisitionMode.Value != Enums.AcquisitionMode.Kinetic &&
                AcquisitionMode.Value != Enums.AcquisitionMode.FastKinetics)
                throw new ArgumentException($"Current {nameof(AcquisitionMode)} ({AcquisitionMode}) does not support kinetic cycle.");


            KineticCycle = (Frames: number, Time: time);
        }

        public abstract bool IsHSSpeedSupported(int speedIndex, out float speed);

        protected virtual void CheckCamera()
        {
            // Checks if camera object is null
            if (camera == null)
                throw new NullReferenceException("Camera is null.");

            // Checks if camera is initialized
            if (!camera.IsInitialized)
                throw new AndorSDKException("Camera is not properly initialized.", null);
        }
    }
}
