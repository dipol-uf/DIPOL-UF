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
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Runtime.CompilerServices;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Exceptions;
using ANDOR_CS.Attributes;

namespace ANDOR_CS.Classes
{
    public abstract class SettingsBase : IDisposable, IXmlSerializable
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

        private static readonly Regex SetFnctionNameParser = new Regex(@"Set(.+)$");

       
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
        public (int Index, float Speed)? VSSpeed
        {
            get;
            protected set;
        } 

        /// <summary>
        /// Stores the value of currently set horizontal speed
        /// </summary>
        [SerializationOrder(5)]
        public (int Index, float Speed)? HSSpeed
        {
            get;
            protected set;
        } 

        /// <summary>
        /// Stores the index of currently set Analogue-Digital Converter and its bit depth.
        /// </summary>
        [SerializationOrder(3)]
        public (int Index, int BitDepth)? ADConverter
        {
            get;
            protected set;
        } 

        /// <summary>
        /// Stores the value of currently set vertical clock voltage amplitude
        /// </summary>
        [SerializationOrder(2)]
        public VsAmplitude? VsAmplitude
        {
            get;
            protected set;
        } 

        /// <summary>
        /// Stores type of currentlt set OutputAmplifier
        /// </summary>
        [SerializationOrder(4)]
        public (OutputAmplification OutputAmplifier, string Name, int Index)? OutputAmplifier
        {
            get;
            protected set;
        } 

        /// <summary>
        /// Stores type of currently set PreAmp Gain
        /// </summary>
        [SerializationOrder(6)]
        public (int Index, string Name)? PreAmpGain
        {
            get;
            protected set;
        } 

        /// <summary>
        /// Stores currently set acquisition mode
        /// </summary>
        [SerializationOrder(7)]
        public AcquisitionMode? AcquisitionMode
        {
            get;
            protected set;
        } 

        /// <summary>
        /// Stores currently set read mode
        /// </summary>
        [SerializationOrder(8)]
        public ReadMode? ReadoutMode
        {
            get;
            protected set;
        }

        /// <summary>
        /// Stores currently set trigger mode
        /// </summary>
        [SerializationOrder(9)]
        public TriggerMode? TriggerMode
        {
            get;
            protected set;
        }

        /// <summary>
        /// Stores exposure time
        /// </summary>
        [SerializationOrder(0)]
        public float? ExposureTime
        {
            get;
            protected set;
        } 

        /// <summary>
        /// Stoers seleced image area - part of the CCD from where data should be collected
        /// </summary>
        [SerializationOrder(11)]
        public Rectangle? ImageArea
        {
            get;
            protected set;
        } 

        [SerializationOrder(12)]
        public (int Frames, float Time)? AccumulateCycle
        {
            get;
            protected set;
        } 

        [SerializationOrder(13)]
        public (int Frames, float Time)? KineticCycle
        {
            get;
            protected set;
        } 

        [SerializationOrder(10)]
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
        /// <exception cref="AndorSdkException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <param name="speedIndex">Index of available speed that corresponds to VSpeed listed in <see cref="Camera.Properties"/>.VSSpeeds</param>
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
        public virtual void SetVsAmplitude(VsAmplitude amplitude)
        {
            // Checks if Camera is OK
            CheckCamera();

            // Checks if Camera supports vertical clock voltage amplitude changes
            VsAmplitude = Camera.Capabilities.SetFunctions.HasFlag(SetFunction.VerticalClockVoltage)
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

            // Queries available amplifiers, looking for the one, which type mathces input parameter
            var query = (from amp
                        in Camera.Properties.OutputAmplifiers
                        where amp.OutputAmplifier == amplifier
                        select amp)
                .ToList();

            // If no mathces found, throws an exception
            if (query.Count == 0)
                throw new ArgumentOutOfRangeException($"Provided amplifier i sout of range " +
                    $"{(Enum.IsDefined(typeof(OutputAmplification), amplifier) ? Enum.GetName(typeof(OutputAmplification), amplifier) : "Unknown")}.");

            // Otherwise, assigns name and type of the amplifier 
            var element = query[0];

            OutputAmplifier = (OutputAmplifier: element.OutputAmplifier, Name: element.Name,Index: Camera.Properties.OutputAmplifiers.IndexOf(element));
            HSSpeed = null;
            PreAmpGain = null;
            EMCCDGain = null;
        }

        /// <summary>
        /// Returns a collection of available Horizonal Readout Speeds for currently selected OutputAmplifier and AD Converter.
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
        /// and <see cref="AcquisitionSettings.OutputAmplifier"/> should be set
        /// via <see cref="SetADConverter"/>, <see cref="SetHSSpeed"/>
        /// and <see cref="AcquisitionSettings.SetOutputOutputAmplifier(OutputAmplification)"/>.
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


            // Checks if Camera supports specifed mode
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

            // Checks if Camera supports specifed mode
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

            // If there are no matches in the pre-defiend table, then this mode cannot be set explicitly
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
            using (var str = System.Xml.XmlWriter.Create(
                stream,
                new System.Xml.XmlWriterSettings()
                {
                    Indent = true,
                    IndentChars = "\t"
                }))
            {
                var sourceCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
                System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                WriteXml(str);

                System.Threading.Thread.CurrentThread.CurrentCulture = sourceCulture;
            }
        }

        public virtual void Deserialize(Stream stream)
        {
            using (var str = XmlReader.Create(stream))
                ReadXml(str);
        }

        public virtual void Dispose()
        {
            Camera = null;
        }

        public XmlSchema GetSchema()
            => null;

        public void ReadXml(XmlReader reader)
        {
            var result = XmlParser.ReadXml(reader);

            if (result.Any(x => 
                x.Key == @"CompatibleDevice" &&
                (Enums.CameraType)x.Value != Camera.Capabilities.CameraType))
                throw new AndorSdkException("Failed to deserialize acquisition settings: " +
                    "device type mismatch. Check attribute \"CompatibleDevice\" of Settings node", null);

            foreach (var item in
                    from r in SerializedProperties
                        .Join(result, x => x.Name, y => y.Key, (x, y) => y)
                    from m in DeserializationSetMethods
                    let regexName = new {Method = m, Regex = SetFnctionNameParser.Match(m.Name)}
                    where regexName.Regex.Success && regexName.Regex.Groups.Count == 2
                    where r.Key == regexName.Regex.Groups[1].Value
                    select new {regexName.Method, r.Key, r.Value})
                     
            {
                var pars = item.Method.GetParameters();
                if (item.Value is ITuple tuple &&
                    pars.Length <= tuple.Length)
                {
                    var tupleVals = new object[pars.Length];

                    if (pars.Where((t, i) => (tupleVals[i] = tuple[i]).GetType() != t.ParameterType).Any())
                        throw new TargetParameterCountException(@"Setter method argumetn type does not match type of provided tuple item.");

                    item.Method.Invoke(this, tupleVals);

                }
                else if (pars.Length == 1 && pars[0].ParameterType == item.Value.GetType())
                    item.Method.Invoke(this, new[] { item.Value });
                else
                    throw new TargetParameterCountException(@"Setter method signature does not match provided parameters.");
                
            }
                    

        }

        public void WriteXml(XmlWriter writer)
            => XmlParser.WriteXml(writer, this);

    }   
}
