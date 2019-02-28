//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
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
using System.Linq;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Exceptions;
using ANDOR_CS.Attributes;
using FITS_CS;
using SettingsManager;

// ReSharper disable InconsistentNaming
#pragma warning disable 1591

namespace ANDOR_CS.Classes
{
    /// <summary>
    /// Base class for all the settings profiles
    /// </summary>
    public abstract class SettingsBase : IDisposable, IXmlSerializable, INotifyPropertyChanged
    {
        private static readonly PropertyInfo[] SerializedProperties =
            typeof(SettingsBase)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p =>
                p.GetCustomAttribute<Attributes.NonSerializedAttribute>(true) == null &&
                p.SetMethod != null &&
                p.GetMethod != null)
            .OrderBy(p => p.GetCustomAttribute<SerializationOrderAttribute>(true)?.Index ?? 0)
            .ToArray();

        private static readonly MethodInfo[] DeserializationSetMethods =
            typeof(SettingsBase)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(mi => mi.Name.Contains("Set") && mi.ReturnType == typeof(void))
            .ToArray();

        private static readonly Regex SetFunctionNameParser = new Regex(@"Set(.+)$");

        private (int Index, float Speed)? _VSSpeed;
        private (int Index, float Speed)? _HSSpeed;
        private (int Index, int BitDepth)? _ADConverter;
        private VSAmplitude? _VSAmplitude;
        private (OutputAmplification OutputAmplifier, string Name, int Index)? _OutputAmplifier;
        private (int Index, string Name)? _PreAmpGain;
        private AcquisitionMode? _AcquisitionMode;
        private ReadMode? _ReadoutMode;
        private TriggerMode? _TriggerMode;
        private float? _ExposureTime;
        private Rectangle? _ImageArea;
        private (int Frames, float Time)? _AccumulateCycle;
        private (int Frames, float Time)? _KineticCycle;
        private int? _EmCcdGain;

        public  event PropertyChangedEventHandler PropertyChanged;

        [Attributes.NonSerialized]
        public CameraBase Camera
        {
            get;
            protected set;
        }

        /// <summary>
        /// Stores the value of currently set vertical speed
        /// </summary>
        [SerializationOrder(1)]
        [FitsKey("VSPEED", "usec", 1)]
        public (int Index, float Speed)? VSSpeed
        {
            get => _VSSpeed;
            protected set
            {
                _VSSpeed = value;
                RaisePropertyChanged();
            }
        } 

        /// <summary>
        /// Stores the value of currently set horizontal speed
        /// </summary>
        [SerializationOrder(5)]
        [FitsKey("HSPEED", "MHz", 1)]
        public (int Index, float Speed)? HSSpeed
        {
            get => _HSSpeed;
            protected set
            {
                _HSSpeed = value;
                RaisePropertyChanged();
            }
        } 

        /// <summary>
        /// Stores the index of currently set Analogue-Digital Converter and its bit depth.
        /// </summary>
        [SerializationOrder(3)]
        [FitsKey("ADCONV", index: 0)]
        [FitsKey("BITDEP", "bits", 1)]
        public (int Index, int BitDepth)? ADConverter
        {
            get => _ADConverter;
            protected set
            {
                _ADConverter = value;
                RaisePropertyChanged();
            }
        } 

        /// <summary>
        /// Stores the value of currently set vertical clock voltage amplitude
        /// </summary>
        [SerializationOrder(2)]
        [FitsKey("CLOCKAMP")]
        public VSAmplitude? VSAmplitude
        {
            get => _VSAmplitude;
            protected set
            {
                _VSAmplitude = value;
                RaisePropertyChanged();
            }
        } 

        /// <summary>
        /// Stores type of currently set OutputAmplifier
        /// </summary>
        [SerializationOrder(4)]
        [FitsKey("AMPLIF", index: 2)]
        public (OutputAmplification OutputAmplifier, string Name, int Index)? OutputAmplifier
        {
            get => _OutputAmplifier;
            protected set
            {
                _OutputAmplifier = value;
                RaisePropertyChanged();
            }
        } 

        /// <summary>
        /// Stores type of currently set PreAmp Gain
        /// </summary>
        [SerializationOrder(6)]
        [FitsKey("AMPGAIN", index: 2)]
        public (int Index, string Name)? PreAmpGain
        {
            get => _PreAmpGain;
            protected set
            {
                _PreAmpGain = value;
                RaisePropertyChanged();
            }
        } 

        /// <summary>
        /// Stores currently set acquisition mode
        /// </summary>
        [SerializationOrder(7)]
        [FitsKey("MODE")]
        public AcquisitionMode? AcquisitionMode
        {
            get => _AcquisitionMode;
            protected set
            {
                _AcquisitionMode = value;
                RaisePropertyChanged();
            }
        } 

        /// <summary>
        /// Stores currently set read mode
        /// </summary>
        [SerializationOrder(8)]
        [FitsKey("READOUT")]
        public ReadMode? ReadoutMode
        {
            get => _ReadoutMode;
            protected set
            {
                _ReadoutMode = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Stores currently set trigger mode
        /// </summary>
        [SerializationOrder(9)]
        [FitsKey("TRIGGER")]
        public TriggerMode? TriggerMode
        {
            get => _TriggerMode;
            protected set
            {
                _TriggerMode = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Stores exposure time
        /// </summary>
        [SerializationOrder(0)]
        [FitsKey("EXPTIME")]
        public float? ExposureTime
        {
            get => _ExposureTime;
            protected set
            {
                _ExposureTime = value;
                RaisePropertyChanged();
            }
        } 

        /// <summary>
        /// Stores selected image area - part of the CCD from where data should be collected
        /// </summary>
        [SerializationOrder(11)]
        [FitsKey("CCDAREA", "x1; y1; x2; y2")]
        public Rectangle? ImageArea
        {
            get => _ImageArea;
            protected set
            {
                _ImageArea = value;
                RaisePropertyChanged();
            }
        } 

        [SerializationOrder(12, true)]
        [FitsKey("ACCUMN", index: 0)]
        [FitsKey("ACCUMT", "sec", 1)]
        public (int Frames, float Time)? AccumulateCycle
        {
            get => _AccumulateCycle;
            protected set
            {
                _AccumulateCycle = value;
                RaisePropertyChanged();
            }
        } 

        [SerializationOrder(13, true)]
        [FitsKey("KINETN", index: 0)]
        [FitsKey("KINETT", "sec", 1)]
        public (int Frames, float Time)? KineticCycle
        {
            get => _KineticCycle;
            protected set
            {
                _KineticCycle = value;
                RaisePropertyChanged();
            }
        } 

        [SerializationOrder(10)]
        [FitsKey("CCDGAIN")]
        public int? EMCCDGain
        {
            get => _EmCcdGain;
            protected set
            {
                _EmCcdGain = value;
                RaisePropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
            => PropertyChanged?.Invoke(sender, e);

        protected virtual void RaisePropertyChanged(
            [CallerMemberName] string name = "")
            => OnPropertyChanged(this, new PropertyChangedEventArgs(name));

        //public virtual List<(string Option, bool Success, uint ReturnCode)> ApplySettings(
        //    out (float ExposureTime, float AccumulationCycleTime, float KineticCycleTime, int BufferSize) timing)
        //{
        //    Camera.CurrentSettings = this;
        //    timing = default((float, float, float, int));
        //    Camera.Timings = (timing.ExposureTime, timing.AccumulationCycleTime, timing.KineticCycleTime);
        //    //Camera.SettingsFitsKeys 
        //    return null;
        //}

        /// <summary>
        /// Tries to set vertical speed. 
        /// Camera may not be active.
        /// </summary>
        /// <exception cref="AndorSdkException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <param name="speedIndex">Index of available speed that corresponds to VSpeed listed in
        /// <see cref="CameraProperties"/>.VSSpeeds</param>
        public virtual void SetVSSpeed(int speedIndex)
        {
            // Checks if Camera is OK
            CheckCamera();

            // Checks if Camera actually supports changing vertical readout speed
            if (!Camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalReadoutSpeed))
                throw new NotSupportedException("Camera does not support vertical readout speed control.");
            
            // Available speeds max index
            var length = Camera.Properties.VSSpeeds.Length;

            // If speed index is invalid
            if (speedIndex < 0 || speedIndex >= length)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(speedIndex)} is out of range (should be in [{0},  {length - 1}]).");


            // If success, updates VSSpeed field and 
            VSSpeed = (Index: speedIndex, Speed: Camera.Properties.VSSpeeds[speedIndex]);
        }

        /// <summary>
        /// Sets the vertical clock voltage amplitude (if Camera supports it).
        /// Camera may be not active.
        /// </summary>
        /// <param name="amplitude">New amplitude </param>
        public virtual void SetVSAmplitude(VSAmplitude amplitude)
        {
            // Checks if Camera is OK
            CheckCamera();

            // Checks if Camera supports vertical clock voltage amplitude changes
            VSAmplitude = Camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalClockVoltage)
                ? amplitude
                : throw new NotSupportedException("Camera does not support vertical clock voltage amplitude control.");
        }

        /// <summary>
        /// Sets Analogue-Digital converter.
        /// Does not require Camera to be active
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="converterIndex"></param>
        public virtual void SetADConverter(int converterIndex)
        {
            // Checks if Camera is OK
            CheckCamera();

            if (converterIndex < 0 || converterIndex >= Camera.Properties.ADConverters.Length)
                throw new ArgumentOutOfRangeException($"AD converter index {converterIndex} if out of range " +
                    $"(should be in [{0}, {Camera.Properties.ADConverters.Length - 1}]).");

            ADConverter = (Index: converterIndex, BitDepth: Camera.Properties.ADConverters[converterIndex]);
            HSSpeed = null;
            PreAmpGain = null;
        }

        /// <summary>
        /// Sets output amplifier. 
        /// Does not require Camera to be active.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="amplifier"></param>
        public virtual void SetOutputAmplifier(OutputAmplification amplifier)
        {
            // Checks Camera object
            CheckCamera();

            // Queries available amplifiers, looking for the one, which type matches input parameter
            var query = (from amp
                        in Camera.Properties.OutputAmplifiers
                        where amp.OutputAmplifier == amplifier
                        select amp)
                .ToList();

            // If no matches found, throws an exception
            if (query.Count == 0)
                throw new ArgumentOutOfRangeException("Provided amplifier i sout of range " +
                    $"{(Enum.IsDefined(typeof(OutputAmplification), amplifier) ? Enum.GetName(typeof(OutputAmplification), amplifier) : "Unknown")}.");

            // Otherwise, assigns name and type of the amplifier 
            var element = query[0];

            OutputAmplifier = (element.OutputAmplifier, element.Name,Index: Camera.Properties.OutputAmplifiers.IndexOf(element));
            HSSpeed = null;
            PreAmpGain = null;
            EMCCDGain = null;
        }

        /// <summary>
        /// Returns a collection of available Horizontal Readout Speeds for currently selected OutputAmplifier and AD Converter.
        /// Requires Camera to be active.
        /// Note: <see cref="ADConverter"/> and <see cref="SettingsBase.OutputAmplifier"/> should be set
        /// via <see cref="SetADConverter"/> and <see cref="SettingsBase.SetOutputAmplifier(OutputAmplification)"/>
        /// before calling this method.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <returns>An enumerable collection of speed indexes and respective speed values available.</returns>
        public virtual IEnumerable<(int Index, float Speed)> GetAvailableHSSpeeds()
        { 
            
            // Checks if Camera is OK and is active
            CheckCamera();

            // Checks if Camera support horizontal speed controls
            if (!Camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed))
                throw new NotSupportedException("Camera does not support horizontal readout speed controls.");

            // Checks if AD converter and OutputAmplifier are already selected
            if (ADConverter == null || OutputAmplifier == null)
                throw new NullReferenceException($"Either AD converter ({nameof(ADConverter)}) or OutputAmplifier ({nameof(OutputAmplifier)}) are not set.");

            // Determines indexes of converter and amplifier
            var channel = ADConverter.Value.Index;
            var amp = OutputAmplifier.Value.Index;
                
            return GetAvailableHSSpeeds(channel, amp);

        }

        /// <summary>
        /// Sets Horizontal Readout Speed for currently selected OutputAmplifier and AD Converter.
        /// Requires Camera to be active.
        /// Note: <see cref="ADConverter"/> and <see cref="SettingsBase.OutputAmplifier"/> should be set
        /// via <see cref="SetADConverter"/> and <see cref="SettingsBase.SetOutputAmplifier(OutputAmplification)"/>
        /// before calling this method.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <param name="speedIndex">Index of horizontal speed</param>
        public virtual void SetHSSpeed(int speedIndex)
        {
            CheckCamera();


            if (!Camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed) ||
                !IsHSSpeedSupported(speedIndex, out var speed))
                throw new NotSupportedException("Camera does not support horizontal readout speed controls");
            else
            {
                HSSpeed = (Index: speedIndex, Speed: speed);
                PreAmpGain = null;
            }
        }

        /// <summary>
        /// Returns a collection of available PreAmp gains for currently selected HSSpeed, OutputAmplifier, Converter.
        /// Requires Camera to be active.
        /// Note: <see cref="ADConverter"/>, <see cref="HSSpeed"/>
        /// and <see cref="SettingsBase"/> should be set
        /// via <see cref="SetADConverter"/>, <see cref="SetHSSpeed"/>
        /// and <see cref="SettingsBase"/>.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="NotSupportedException"/>
        /// <returns>Available PreAmp gains</returns>
        public virtual IEnumerable<(int Index, string Name)> GetAvailablePreAmpGain()
        {
            // Checks if Camera is OK and is active
            CheckCamera();
            
            // Checks if Camera supports PreAmp Gain control
            if (!Camera.Capabilities.SetFunctions.HasFlag(SetFunction.PreAmpGain))
                throw new NotSupportedException("Camera does not support Pre Amp Gain controls.");

            // Check if all required settings are already set
            if (HSSpeed == null || OutputAmplifier == null || ADConverter == null)
                throw new NullReferenceException($"One of the following settings are not set: AD Converter ({nameof(ADConverter)})," +
                                                 $"OutputAmplifier ({nameof(OutputAmplifier)}), Vertical Speed ({nameof(VSSpeed)}).");

            return GetAvailablePreAmpGain(ADConverter.Value.Index, OutputAmplifier.Value.Index, HSSpeed.Value.Index);

        }

        /// <summary>
        /// Sets PreAmp gain for currently selected HSSpeed, OutputAmplifier, Converter.
        /// Requires Camera to be active.
        /// Note: <see cref="ADConverter"/>, <see cref="HSSpeed"/>
        /// and <see cref="SettingsBase.OutputAmplifier"/> should be set
        /// via <see cref="SetADConverter"/>, <see cref="SetHSSpeed"/>
        /// and <see cref="SettingsBase.SetOutputAmplifier(OutputAmplification)"/>.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <param name="gainIndex">Index of gain</param>
        public virtual void SetPreAmpGain(int gainIndex)
        {
            // Checks if Camera is OK and is active
            CheckCamera();           

            // Checks if Camera supports PreAmp Gain control
            if (!Camera.Capabilities.SetFunctions.HasFlag(SetFunction.PreAmpGain)) 
                throw new AndorSdkException("Pre Amp Gain is not supported", null);
            // Check if all required settings are already set
            if (HSSpeed == null || OutputAmplifier == null || ADConverter == null)
                throw new NullReferenceException($"One of the following settings are not set: AD Converter ({nameof(ADConverter)})," +
                                                 $"OutputAmplifier ({nameof(OutputAmplifier)}), Vertical Speed ({nameof(VSSpeed)}).");

            // Total number of gain settings available
            var gainNumber = Camera.Properties.PreAmpGains.Length;

            // Checks if argument is in valid range
            if (gainIndex < 0 || gainIndex >= gainNumber)
                throw new ArgumentOutOfRangeException($"Gain index (nameof{gainIndex}) is out of range (should be in [{0}, {gainNumber}]).");

            if (GetAvailablePreAmpGain().Any(item => item.Index == gainIndex))
                PreAmpGain = (Index: gainIndex, Name: Camera.Properties.PreAmpGains[gainIndex]);
            else
                throw new ArgumentOutOfRangeException($"Pre amp gain index ({gainIndex}) is out of range.");

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
            // Checks if Camera is OK
            CheckCamera();


            // Checks if Camera supports specified mode
            if (!Camera.Capabilities.AcquisitionModes.HasFlag(mode))
                throw new NotSupportedException($"Camera does not support specified regime ({mode})");


            // If there are no matches in the pre-defined table, then this mode cannot be set explicitly
            if (!EnumConverter.AcquisitionModeTable.ContainsKey(mode.HasFlag(Enums.AcquisitionMode.FrameTransfer)? mode^Enums.AcquisitionMode.FrameTransfer : mode))
                throw new InvalidOperationException($"Cannot explicitly set provided acquisition mode ({mode})");

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
            // Checks if Camera is OK
            CheckCamera();

            // Checks if Camera supports specified mode
            if (!Camera.Capabilities.TriggerModes.HasFlag(mode))
                throw new NotSupportedException($"Camera does not support specified regime ({mode})");

            // If there are no matches in the pre-defined table, then this mode cannot be set explicitly
            if (!EnumConverter.TriggerModeTable.ContainsKey(mode))
                throw new InvalidOperationException($"Cannot explicitly set provided acquisition mode ({mode})");

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
            // Checks Camera
            CheckCamera();

            // Checks if Camera support read mode control
            if (!Camera.Capabilities.ReadModes.HasFlag(mode))
                throw new NotSupportedException($"Camera does not support specified regime ({mode})");

            // If there are no matches in the pre-defined table, then this mode cannot be set explicitly
            if (!EnumConverter.ReadModeTable.ContainsKey(mode))
                throw new InvalidOperationException($"Cannot explicitly set provided acquisition mode ({mode})");


            ReadoutMode = mode;
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



            if (Camera.Capabilities.GetFunctions.HasFlag(GetFunction.DetectorSize))
            {
                var size = Camera.Properties.DetectorSize;

                if (area.X2 > size.Horizontal)
                    throw new ArgumentOutOfRangeException($"Right boundary exceeds CCD size ({area.X2} >= {size.Horizontal}).");

                if (area.Y2 > size.Vertical)
                    throw new ArgumentOutOfRangeException($"Top boundary exceeds CCD size ({area.Y2} >= {size.Vertical}).");
            }

            ImageArea = area;
        }

        public virtual void SetEMCCDGain(int gain)
        {
            if (Camera.Capabilities.SetFunctions.HasFlag(SetFunction.EMCCDGain))
            {

                if (!OutputAmplifier.HasValue || !OutputAmplifier.Value.OutputAmplifier.HasFlag(OutputAmplification.ElectronMultiplication))
                    throw new NullReferenceException($"OutputAmplifier should be set to {OutputAmplification.Conventional} before accessing EMCCDGain.");

                var range = Camera.Properties.EMCCDGainRange;

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
                !AcquisitionMode.Value.HasFlag(Enums.AcquisitionMode.Accumulation) &&
                !AcquisitionMode.Value.HasFlag(Enums.AcquisitionMode.Kinetic) &&
                !AcquisitionMode.Value.HasFlag(Enums.AcquisitionMode.FastKinetics))
                throw new ArgumentException($"Current {nameof(AcquisitionMode)} ({AcquisitionMode}) does not support accumulation.");

            AccumulateCycle = (Frames: number, Time: time);
        }

        public virtual void SetKineticCycle(int number, float time)
        {
            CheckCamera();

            if (!AcquisitionMode.HasValue ||
                !AcquisitionMode.Value.HasFlag(Enums.AcquisitionMode.RunTillAbort) &&
                !AcquisitionMode.Value.HasFlag(Enums.AcquisitionMode.Kinetic) &&
                !AcquisitionMode.Value.HasFlag(Enums.AcquisitionMode.FastKinetics))
                throw new ArgumentException($"Current {nameof(AcquisitionMode)} ({AcquisitionMode}) does not support kinetic cycle.");


            KineticCycle = (Frames: number, Time: time);
        }

        public virtual bool IsHSSpeedSupported(int speedIndex, out float speed)
        {
            // Checks if Camera is OK and is active
            CheckCamera();
            speed = 0;

            // Checks if Camera supports horizontal readout speed control
            if (Camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed))
            {
                if (ADConverter == null || OutputAmplifier == null)
                    return false;

                return IsHSSpeedSupported(speedIndex, ADConverter.Value.Index, OutputAmplifier.Value.Index, out speed);
            }
            else
                return false;
        
        }

        public virtual HashSet<string> AllowedSettings()
        {
            if (SettingsProvider.Settings.TryGet("AllowedSettingsPath", out string pathStr))
            {
                var path = Path.GetFullPath(pathStr);
                using (var str = new StreamReader(path))
                {
                    var setts = new JsonSettings(str.ReadToEnd());
                    var allowedItems = setts.GetArray<string>("AllowedSettings");

                    return new HashSet<string>(allowedItems.Select(x => x.ToLowerInvariant()));
                }
            }
            return new HashSet<string>(SerializedProperties.Select(x => x.Name.ToLowerInvariant()));
        }

        public virtual HashSet<string> SupportedSettings()
        {
            var settings = new HashSet<string>();

            if(Camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalReadoutSpeed))
				settings.Add(nameof(VSSpeed).ToLowerInvariant());
            if(Camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalClockVoltage) )
				settings.Add(nameof(VSAmplitude).ToLowerInvariant());
            if(Camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed) )
				settings.Add(nameof(HSSpeed).ToLowerInvariant());
            if(Camera.Capabilities.SetFunctions.HasFlag(SetFunction.PreAmpGain) )
				settings.Add(nameof(PreAmpGain).ToLowerInvariant());
            if(Camera.Capabilities.SetFunctions.HasFlag(SetFunction.EMCCDGain) )
				settings.Add(nameof(EMCCDGain).ToLowerInvariant());

            // TODO: Check individual support of these features
            settings.Add(nameof(ADConverter).ToLowerInvariant());
            settings.Add(nameof(OutputAmplifier).ToLowerInvariant());
            settings.Add(nameof(AcquisitionMode).ToLowerInvariant());
            settings.Add("frametransfer");
            settings.Add(nameof(ReadoutMode).ToLowerInvariant());
            settings.Add(nameof(TriggerMode).ToLowerInvariant());
            settings.Add(nameof(ExposureTime).ToLowerInvariant());
            settings.Add(nameof(ImageArea).ToLowerInvariant());

            return settings;
        }

        public abstract bool IsHSSpeedSupported(
            int speedIndex, 
            int adConverter,
            int amplifier, 
            out float speed);

        public abstract IEnumerable<(int Index, float Speed)> GetAvailableHSSpeeds(
            int adConverter, 
            int amplifier);

        public abstract IEnumerable<(int Index, string Name)> GetAvailablePreAmpGain(
           int adConverter,
           int amplifier,
           int hsSpeed);

        protected virtual void CheckCamera()
        {
            // Checks if Camera object is null
            if (Camera == null)
                throw new NullReferenceException("Camera is null.");

            // Checks if Camera is initialized
            if (!Camera.IsInitialized)
                throw new AndorSdkException("Camera is not properly initialized.", null);
        }

        public virtual void Serialize(Stream stream)
        {
            var str = new StreamWriter(stream);
            WriteJson(str);
        }

        public virtual List<string> Deserialize(Stream stream)
        {
            //using (var str = XmlReader.Create(stream))
            //    return ReadXml(str);

            return ReadJson(new StreamReader(stream));
        }

        public virtual void Dispose()
        {
            Camera = null;
        }

        public XmlSchema GetSchema()
            => null;

        public List<string> ReadXml(XmlReader reader)
        {
            var output = new List<string>(SerializedProperties.Length);

            var result = XmlParser.ReadXml(reader);

            if (result.Any(x => 
                x.Key == @"CompatibleDevice" &&
                (CameraType)x.Value != Camera.Capabilities.CameraType))
                throw new AndorSdkException("Failed to deserialize acquisition settings: " +
                    "device type mismatch. Check attribute \"CompatibleDevice\" of Settings node", null);

            foreach (var item in
                    from r in SerializedProperties
                        .Join(result, x => x.Name, y => y.Key, (x, y) => y)
                    from m in DeserializationSetMethods
                    let regexName = new {Method = m, Regex = SetFunctionNameParser.Match(m.Name)}
                    where regexName.Regex.Success && regexName.Regex.Groups.Count == 2
                    where r.Key == regexName.Regex.Groups[1].Value
                    select new {regexName.Method, r.Key, r.Value})
                     
            {
                try
                {
                    var pars = item.Method.GetParameters();
                    if (item.Value is ITuple tuple &&
                        pars.Length <= tuple.Length)
                    {
                        var tupleVals = new object[pars.Length];

                        if (pars.Where((t, i) => (tupleVals[i] = tuple[i]).GetType() != t.ParameterType).Any())
                            throw new TargetParameterCountException(@"Setter method argument type does not match type of provided item.");

                        item.Method.Invoke(this, tupleVals);

                    }
                    else if (pars.Length == 1 && pars[0].ParameterType == item.Value.GetType())
                        item.Method.Invoke(this, new[] { item.Value });
                    else
                        throw new TargetParameterCountException(@"Setter method signature does not match provided parameters.");
                }
                catch (TargetInvocationException)
                { 
                }

                output.Add(item.Key);
            }

            return output;

        }

        public void WriteXml(XmlWriter writer)
            => XmlParser.WriteXml(writer, this);

        public void WriteJson(StreamWriter writer)
            => JsonParser.WriteJson(writer, this);

        public List<string> ReadJson(StreamReader str)
        {
            var result = JsonParser.ReadJson(str);
            var props = new List<string>();
            //if (data.Any(x =>
            //    x.Key == @"CompatibleDevice" &&
            //    (x.Value == null ||
            //        (CameraType)x.Value != Camera.Capabilities.CameraType)))
            //    throw new AndorSdkException("Failed to deserialize acquisition settings: " +
            //                                "device type mismatch. Check attribute \"CompatibleDevice\" of Settings node", null);

            foreach (var item in
                from r in SerializedProperties
                    .Join(result, x => x.Name, y => y.Key, (x, y) => y)
                from m in DeserializationSetMethods
                let regexName = new {Method = m, Regex = SetFunctionNameParser.Match(m.Name)}
                where regexName.Regex.Success && regexName.Regex.Groups.Count == 2
                where r.Key == regexName.Regex.Groups[1].Value
                select new {regexName.Method, r.Key, r.Value})
            {
                var pars = item.Method.GetParameters();
                try
                {
                    if (item.Value is object[] coll)
                    {
                        if (pars.Length == coll.Length)
                        {
                            for (var i = 0; i < pars.Length; i++)
                                coll[i] = Convert.ChangeType(coll[i], pars[i].ParameterType);

                            item.Method.Invoke(this, coll);
                        }
                        else
                            throw new TargetParameterCountException(
                                @"Setter method signature does not match types of provided items.");
                    }
                    else if (pars.Length == 1 && pars[0].ParameterType.IsEnum)
                    {
                        if (item.Value is string enumStr)
                        {
                            var enumResult = Enum.Parse(pars[0].ParameterType, enumStr);
                            item.Method.Invoke(this, new[] { enumResult });
                        }
                    }
                    else if (pars.Length == 1 && item.Value.GetType().IsValueType)
                    {
                        item.Method.Invoke(this, new[]
                        {
                            Convert.ChangeType(item.Value, pars[0].ParameterType)
                        });
                    }
                    else if (item.Key == nameof(ImageArea) &&
                             item.Value is Dictionary<string, object> dict &&
                             dict.Count == 2)
                    {
                        var startColl = dict["Start"] as Dictionary<string, object>;
                        var endColl = dict["End"] as Dictionary<string, object>;
                        var area = new Rectangle(
                            new Point2D(
                                (int)startColl["X"],
                                (int)startColl["Y"]
                                ), 
                            new Point2D(
                                (int)endColl["X"],
                                (int)endColl["Y"])
                            );

                        item.Method.Invoke(this, new object[] {area});
                    }
                    else
                        throw  new ArgumentException("Deserialized value cannot be parsed.");
                }
                catch (TargetInvocationException)
                {
                }

                props.Add(item.Key);
            }

            return props;
        }

        public List<FitsKey> ConvertToFitsKeys()
        {
            List<FitsKey> GetValue(string name, object value, List<FitsKeyAttribute> attrs)
            {
                var result = new List<FitsKey>();

                if (attrs is null)
                    attrs = new List<FitsKeyAttribute>() {null};

                foreach (var attr in attrs)
                {
                    var header = string.IsNullOrWhiteSpace(attr?.Header) ? name : attr.Header;
                    header = header.Length > FitsKey.KeyHeaderSize
                        ? header.Substring(0, FitsKey.KeyHeaderSize)
                        : header;

                    var val = value;

                    if (val is ITuple tuple && attr?.Index is int index)
                        val = tuple[index];

                    if(val is int)
                        result.Add(new FitsKey(header.ToUpperInvariant(), FitsKeywordType.Integer, val, attr?.Comment ?? ""));
                    else if (val is float || val is double)
                        result.Add(new FitsKey(header.ToUpperInvariant(), FitsKeywordType.Float, val, attr?.Comment ?? ""));
                    else
                    {
                        var str = val.ToString();
                        str = str.Length < 60 ? str : str.Substring(0, 60);

                        result.Add(new FitsKey(header.ToUpperInvariant(), FitsKeywordType.String, str,
                            attr?.Comment ?? ""));
                    }
                }

                return result;
            }

            var values = SerializedProperties
                .Select(x => new
                {
                    Name = x.Name.Substring(0, Math.Min(x.Name.Length, FitsKey.KeyHeaderSize)),
                    Value = x.GetValue(this),
                    FitsDesc = x.GetCustomAttributes<FitsKeyAttribute>().ToList()
                })
                .Where(x => !(x.Value is null))
                .Select(x => GetValue(x.Name, x.Value, x.FitsDesc))
                .Aggregate(new List<FitsKey>(), (old, @new) =>
                {
                    old.AddRange(@new);
                    return old;
                });

            return values;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            ReadXml(reader);
        }

    }   
}
