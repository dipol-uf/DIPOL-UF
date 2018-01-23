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

using System.Collections.Generic;
using ANDOR_CS.Enums;

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
            
        internal static readonly Dictionary<ReadMode, int> ReadModeTable = new Dictionary<Enums.ReadMode, int>
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
