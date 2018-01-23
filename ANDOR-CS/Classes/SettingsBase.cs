﻿//    This file is part of Dipol-3 Camera Manager.

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
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Globalization;

using ANDOR_CS.Enums;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Exceptions;
using ANDOR_CS.Attributes;

namespace ANDOR_CS.Classes
{
    public abstract class SettingsBase : IDisposable, IXmlSerializable
    {
        private static PropertyInfo[] SerializedProperties =
            typeof(SettingsBase)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p =>
                p.GetCustomAttribute<ANDOR_CS.Attributes.NonSerializedAttribute>(true) == null &&
                p.SetMethod != null &&
                p.GetMethod != null)
            .OrderBy(p => p.GetCustomAttribute<SerializationOrderAttribute>(true)?.Index ?? 0)
            .ToArray();

        private static MethodInfo[] DeserializationSetMethods =
            typeof(SettingsBase)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(mi => mi.Name.Contains("Set") && mi.ReturnType == typeof(void))
            .ToArray();

        private static Regex SetFnctionNameParser = new Regex(@"Set(.+)$");

        [ANDOR_CS.Attributes.NonSerialized]
        protected CameraBase camera = null;

        [ANDOR_CS.Attributes.NonSerialized]
        public int CameraIndex
            => camera.CameraIndex;

        /// <summary>
        /// Stores the value of currently set vertical speed
        /// </summary>
        [SerializationOrder(1)]
        public (int Index, float Speed)? VSSpeed
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stores the value of currently set horizontal speed
        /// </summary>
        [SerializationOrder(5)]
        public (int Index, float Speed)? HSSpeed
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stores the index of currently set Analogue-Digital Converter and its bit depth.
        /// </summary>
        [SerializationOrder(3)]
        public (int Index, int BitDepth)? ADConverter
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stores the value of currently set vertical clock voltage amplitude
        /// </summary>
        [SerializationOrder(2)]
        public VSAmplitude? VSAmplitude
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stores type of currentlt set OutputAmplifier
        /// </summary>
        [SerializationOrder(4)]
        public (OutputAmplification OutputAmplifier, string Name, int Index)? OutputAmplifier
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stores type of currently set PreAmp Gain
        /// </summary>
        [SerializationOrder(6)]
        public (int Index, string Name)? PreAmpGain
        {
            get;
            protected set;
        } = null;

        /// <summary>
        /// Stores currently set acquisition mode
        /// </summary>
        [SerializationOrder(7)]
        public AcquisitionMode? AcquisitionMode
        {
            get;
            protected set;
        } = null;

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
        } = null;

        /// <summary>
        /// Stoers seleced image area - part of the CCD from where data should be collected
        /// </summary>
        [SerializationOrder(11)]
        public Rectangle? ImageArea
        {
            get;
            protected set;
        } = null;

        [SerializationOrder(12)]
        public (int Frames, float Time)? AccumulateCycle
        {
            get;
            protected set;
        } = null;

        [SerializationOrder(13)]
        public (int Frames, float Time)? KineticCycle
        {
            get;
            protected set;
        } = null;

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
            HSSpeed = null;
            PreAmpGain = null;
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
                        in camera.Properties.OutputAmplifiers
                        where amp.OutputAmplifier == amplifier
                        select amp;

            // If no mathces found, throws an exception
            if (query.Count() == 0)
                throw new ArgumentOutOfRangeException($"Provided amplifier i sout of range " +
                    $"{(Enum.IsDefined(typeof(OutputAmplification), amplifier) ? Enum.GetName(typeof(OutputAmplification), amplifier) : "Unknown")}.");

            // Otherwise, assigns name and type of the amplifier 
            var element = query.First();

            OutputAmplifier = (OutputAmplifier: element.OutputAmplifier, Name: element.Name,Index: camera.Properties.OutputAmplifiers.IndexOf(element));
            HSSpeed = null;
            PreAmpGain = null;
            EMCCDGain = null;
        }

        /// <summary>
        /// Returns a collection of available Horizonal Readout Speeds for currently selected OutputAmplifier and AD Converter.
        /// Requires camera to be active.
        /// Note: <see cref="AcquisitionSettings.ADConverter"/> and <see cref="AcquisitionSettings.OutputAmplifier"/> should be set
        /// via <see cref="AcquisitionSettings.SetADConverter(int)"/> and <see cref="AcquisitionSettings.SetOutputOutputAmplifier(OutputAmplification)"/>
        /// before calling this method.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="NotSupportedException"/>
        /// <returns>An enumerable collection of speed indexes and respective speed values available.</returns>
        public virtual IEnumerable<(int Index, float Speed)> GetAvailableHSSpeeds()
        { 
            
            // Checks if camera is OK and is active
            CheckCamera();

            // Checks if camera support horizontal speed controls
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed))
            {
                // Checks if AD converter and OutputAmplifier are already selected
                if (ADConverter == null || OutputAmplifier == null)
                    throw new NullReferenceException($"Either AD converter ({nameof(ADConverter)}) or OutputAmplifier ({nameof(OutputAmplifier)}) are not set.");

                // Determines indexes of converter and amplifier
                int channel = ADConverter.Value.Index;
                int amp = OutputAmplifier.Value.Index;
                
                return GetAvailableHSSpeeds(channel, amp);
            }
            else
                throw new NotSupportedException("Camera does not support horizontal readout speed controls.");

        }

        /// <summary>
        /// Sets Horizontal Readout Speed for currently selected OutputAmplifier and AD Converter.
        /// Requires camera to be active.
        /// Note: <see cref="AcquisitionSettings.ADConverter"/> and <see cref="AcquisitionSettings.OutputAmplifier"/> should be set
        /// via <see cref="AcquisitionSettings.SetADConverter(int)"/> and <see cref="AcquisitionSettings.SetOutputOutputAmplifier(OutputAmplification)"/>
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
            {
                HSSpeed = (Index: speedIndex, Speed: speed);
                PreAmpGain = null;
            }
            //{
            //    // Checks if both AD converter and OutputAmplifier are already set
            //    if (ADConverter == null || OutputAmplifier == null)
            //        throw new NullReferenceException($"Either AD converter ({nameof(ADConverter)}) or OutputAmplifier ({nameof(OutputAmplifier)}) are not set.");

            //    // Determines indexes of converter and amplifier
            //    int channel = ADConverter.Value.Index;
            //    int amp = OutputAmplifier.Value.Index;
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
        /// Returns a collection of available PreAmp gains for currently selected HSSpeed, OutputAmplifier, Converter.
        /// Requires camera to be active.
        /// Note: <see cref="AcquisitionSettings.ADConverter"/>, <see cref="AcquisitionSettings.HSSpeed"/>
        /// and <see cref="AcquisitionSettings.OutputAmplifier"/> should be set
        /// via <see cref="AcquisitionSettings.SetADConverter(int)"/>, <see cref="AcquisitionSettings.SetHSSpeed(int)"/>
        /// and <see cref="AcquisitionSettings.SetOutputOutputAmplifier(OutputAmplification)"/>.
        /// </summary>
        /// <exception cref="NullReferenceException"/>
        /// <exception cref="NotSupportedException"/>
        /// <returns>Available PreAmp gains</returns>
        public virtual IEnumerable<(int Index, string Name)> GetAvailablePreAmpGain()
        {
            // Checks if camera is OK and is active
            CheckCamera();
            
            // Checks if camera supports PreAmp Gain control
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.PreAmpGain))
            {
                // Check if all required settings are already set
                if (HSSpeed == null || OutputAmplifier == null || ADConverter == null)
                    throw new NullReferenceException($"One of the following settings are not set: AD Converter ({nameof(ADConverter)})," +
                        $"OutputAmplifier ({nameof(OutputAmplifier)}), Vertical Speed ({nameof(VSSpeed)}).");



                return GetAvailablePreAmpGain(ADConverter.Value.Index, OutputAmplifier.Value.Index, HSSpeed.Value.Index);

            }
            else
                throw new NotSupportedException("Camera does not support Pre Amp Gain controls.");

        }

        /// <summary>
        /// Sets PreAmp gain for currently selected HSSpeed, OutputAmplifier, Converter.
        /// Requires camera to be active.
        /// Note: <see cref="AcquisitionSettings.ADConverter"/>, <see cref="AcquisitionSettings.HSSpeed"/>
        /// and <see cref="AcquisitionSettings.OutputAmplifier"/> should be set
        /// via <see cref="AcquisitionSettings.SetADConverter(int)"/>, <see cref="AcquisitionSettings.SetHSSpeed(int)"/>
        /// and <see cref="AcquisitionSettings.SetOutputOutputAmplifier(OutputAmplification)"/>.
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
                if (HSSpeed == null || OutputAmplifier == null || ADConverter == null)
                    throw new NullReferenceException($"One of the following settings are not set: AD Converter ({nameof(ADConverter)})," +
                        $"OutputAmplifier ({nameof(OutputAmplifier)}), Vertical Speed ({nameof(VSSpeed)}).");

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
            if (!EnumConverter.AcquisitionModeTable.ContainsKey(mode.HasFlag(Enums.AcquisitionMode.FrameTransfer)? mode^Enums.AcquisitionMode.FrameTransfer : mode))
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

                if (!OutputAmplifier.HasValue || !OutputAmplifier.Value.OutputAmplifier.HasFlag(OutputAmplification.ElectronMultiplication))
                    throw new NullReferenceException($"OutputAmplifier should be set to {OutputAmplification.Conventional} before accessing EMCCDGain.");

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

        public virtual bool IsHSSpeedSupported(int speedIndex, out float speed)
        {
            // Checks if camera is OK and is active
            CheckCamera();
            speed = 0;

            // Checks if camera supports horizontal readout speed control
            if (camera.Capabilities.SetFunctions.HasFlag(SetFunction.HorizontalReadoutSpeed))
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
            int ADConverter,
            int amplifier, 
            out float speed);

        public abstract IEnumerable<(int Index, float Speed)> GetAvailableHSSpeeds(
            int ADConverter, 
            int amplifier);

        public abstract IEnumerable<(int Index, string Name)> GetAvailablePreAmpGain(
           int ADConverter,
           int amplifier,
           int HSSpeed);

        protected virtual void CheckCamera()
        {
            // Checks if camera object is null
            if (camera == null)
                throw new NullReferenceException("Camera is null.");

            // Checks if camera is initialized
            if (!camera.IsInitialized)
                throw new AndorSDKException("Camera is not properly initialized.", null);
        }

        public virtual void Serialize(Stream stream, string name = "")
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

                str.WriteStartDocument();

                str.WriteStartElement("AcquisitionSettings");
                str.WriteAttributeString("Camera", camera?.CameraModel ?? "TEST");
                str.WriteAttributeString("Name", name);


                var elementsToWrite = from prop
                                      in this.GetType().GetProperties()
                                      where !Attribute.IsDefined(prop, typeof(ANDOR_CS.Attributes.NonSerializedAttribute))
                                      where prop.GetMethod.IsPublic && prop.SetMethod.IsFamily
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

                System.Threading.Thread.CurrentThread.CurrentCulture = sourceCulture;
            }
        }

        public virtual void Deserialize(Stream stream)
        {

            //(camera as Camera).SetActiveAndLock();

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

            if ((query = root.Elements().Where(e => e.Name == "OutputAmplifier")).Count() == 1)
            {
                element = query.First();

                var value = element.Elements().Where(e => e.Attribute(XName.Get("FieldName")).Value == "OutputAmplifier")
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

        public virtual void Dispose()
        {
            camera = null;
        }

        public XmlSchema GetSchema()
            => null;

        public void ReadXml(XmlReader reader)
        {
            //var result = SerializedProperties
            //    .Join(XMLParser.ReadXml(reader), x => x.Name, y => y.Key, (x, y) => y);

            foreach (var item in
                    from r in SerializedProperties
                        .Join(XMLParser.ReadXml(reader), x => x.Name, y => y.Key, (x, y) => y)
                    from m in DeserializationSetMethods
                    let regexName = new {Method = m, Regex = SetFnctionNameParser.Match(m.Name)}
                    where regexName.Regex.Success && regexName.Regex.Groups.Count == 2
                    where r.Key == regexName.Regex.Groups[1].Value
                    select new {regexName.Method, r.Key, r.Value})
                     
            {
            }
                    

        }

        public void WriteXml(XmlWriter writer)
            => XMLParser.WriteXml(writer, this);

    }   
}
