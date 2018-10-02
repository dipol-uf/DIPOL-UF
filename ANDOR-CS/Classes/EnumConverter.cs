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

using System.Collections.Generic;
using ANDOR_CS.Enums;

#pragma warning disable 1591
namespace ANDOR_CS.Classes
{
    public static class EnumConverter
    {

        public static bool IsAcquisitionModeSupported(AcquisitionMode mode)
            => AcquisitionModeTable.ContainsKey(mode);

        public static bool IsReadModeSupported(ReadMode mode)
            => ReadModeTable.ContainsKey(mode);

        public static bool IsTriggerModeSupported(TriggerMode mode)
            => TriggerModeTable.ContainsKey(mode);

        internal static readonly Dictionary<AcquisitionMode, int> AcquisitionModeTable = new Dictionary<AcquisitionMode, int>
        {
            { AcquisitionMode.SingleScan, 1},
            {AcquisitionMode.Accumulation, 2 },
            {AcquisitionMode.Kinetic, 3 },
            {AcquisitionMode.FastKinetics, 4 },
            {AcquisitionMode.RunTillAbort, 5 }
        };
            
        internal static readonly Dictionary<ReadMode, int> ReadModeTable = new Dictionary<ReadMode, int>
        {
            {ReadMode.FullVerticalBinning, 0 },
            {ReadMode.MultiTrack, 1 },
            {ReadMode.RandomTrack, 2 },
            {ReadMode.SingleTrack, 3 },
            {ReadMode.FullImage, 4 },
            {ReadMode.SubImage, 5 }
        };

        internal static readonly Dictionary<TriggerMode, int> TriggerModeTable = new Dictionary<TriggerMode, int>
        {
            {TriggerMode.Internal, 0 },
            {TriggerMode.External, 1 },
            {TriggerMode.ExternalStart, 6 },
            {TriggerMode.ExternalExposure, 7 },
            {TriggerMode.ExternalFvbem, 9 },
            {TriggerMode.Continuous, 10 },
            {TriggerMode.ExternalChargeshifting, 12 }
        };
    }
}
