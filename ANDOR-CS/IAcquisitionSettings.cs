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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ANDOR_CS.Attributes;
using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;
using ANDOR_CS.Enums;
using FITS_CS;
using Serializers;

namespace ANDOR_CS
{
    public interface IAcquisitionSettings : IDisposable, INotifyPropertyChanged
    {
        bool IsDisposed { get; }
        CameraBase Camera { get; }

        [SerializationOrder(1)]
        [FitsKey("VSPEED", "usec", 1)]
        (int Index, float Speed)? VSSpeed { get; }

        [SerializationOrder(5)]
        [FitsKey("HSPEED", "MHz", 1)]
        (int Index, float Speed)? HSSpeed { get; }

        [SerializationOrder(3)]
        [FitsKey("ADCONV", index: 0)]
        [FitsKey("BITDEP", "bits", 1)]
        (int Index, int BitDepth)? ADConverter { get; }

        [SerializationOrder(2)]
        [FitsKey("CLOCKAMP")]
        VSAmplitude? VSAmplitude { get; }

        [SerializationOrder(4)]
        [FitsKey("AMPLIF", index: 2)]
        (OutputAmplification OutputAmplifier, string Name, int Index)? OutputAmplifier { get; }

        [SerializationOrder(6)]
        [FitsKey("AMPGAIN", index: 1)]
        (int Index, string Name)? PreAmpGain { get; }

        [SerializationOrder(7)]
        [FitsKey("MODE")]
        AcquisitionMode? AcquisitionMode { get; }

        [SerializationOrder(8)]
        [FitsKey("READOUT")]
        ReadMode? ReadoutMode { get; }

        [SerializationOrder(9)]
        [FitsKey("TRIGGER")]
        TriggerMode? TriggerMode { get; }

        [SerializationOrder(0)]
        [FitsKey("EXPTIME")]
        float? ExposureTime { get; }

        [SerializationOrder(11)]
        [FitsKey("CCDAREA", "x1; y1; x2; y2")]
        Rectangle? ImageArea { get; }

        [SerializationOrder(12, true)]
        [FitsKey("ACCUMN", index: 0)]
        [FitsKey("ACCUMT", "sec", 1)]
        (int Frames, float Time)? AccumulateCycle { get; }

        [SerializationOrder(13, true)]
        [FitsKey("KINETN", index: 0)]
        [FitsKey("KINETT", "sec", 1)]
        (int Frames, float Time)? KineticCycle { get; }

        [SerializationOrder(10)]
        [FitsKey("CCDGAIN")]
        int? EMCCDGain { get; }

        List<(int Index, float Speed)> GetAvailableHSSpeeds();
        List<(int Index, string Name)> GetAvailablePreAmpGain();
        List<(int Index, float Speed)> GetAvailableHSSpeeds(
            int adConverter,
            int amplifier);
        List<(int Index, string Name)> GetAvailablePreAmpGain(
            int adConverter,
            int amplifier,
            int hsSpeed);
        (int Low, int High) GetEmGainRange();

        void SetVSSpeed(int speedIndex);
        void SetVSAmplitude(VSAmplitude amplitude);
        void SetADConverter(int converterIndex);
        void SetOutputAmplifier(OutputAmplification amplifier);
        void SetHSSpeed(int speedIndex);
        void SetPreAmpGain(int gainIndex);
        void SetAcquisitionMode(AcquisitionMode mode);
        void SetTriggerMode(TriggerMode mode);
        void SetReadoutMode(ReadMode mode);
        void SetExposureTime(float time);
        void SetImageArea(Rectangle area);
        void SetEmCcdGain(int gain);
        void SetAccumulateCycle(int number, float time);
        void SetKineticCycle(int number, float time);

        bool IsHSSpeedSupported(int speedIndex, out float speed);
        bool IsHSSpeedSupported(
            int speedIndex,
            int adConverter,
            int amplifier,
            out float speed);

        HashSet<string> AllowedSettings();
        HashSet<string> SupportedSettings();

        IAcquisitionSettings MakeCopy();

        void Serialize(Stream stream);

        Task SerializeAsync(Stream stream, Encoding enc, CancellationToken token);
        ReadOnlyCollection<string> Deserialize(Stream stream);
        Task<ReadOnlyCollection<string>> DeserializeAsync(Stream stream, Encoding enc, CancellationToken token);

        List<FitsKey> ConvertToFitsKeys();
    }
}