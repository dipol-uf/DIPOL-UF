using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Enums;

namespace ANDOR_CS.Classes
{
    internal static class EnumConverter
    {
   
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
            {TriggerMode.ExternalFVBEM, 9 },
            {TriggerMode.Continuous, 10 },
            {TriggerMode.ExternalChargeshifting, 12 }
        };
    }
}
