using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.DataStructures
{
    public struct CameraProperties
    {
        public TemperatureRange AllowedTemperatures
        {
            get;
            internal set;
        }
        public DetectorSize DetectorSize
        {
            get;
            internal set;
        }
        public bool HasInternalMechanicalShutter
        {
            get;
            internal set;
        }
        public int ADChannelNumber
        {
            get;
            internal set;
        }
        public int AmpNumber
        {
            get;
            internal set;
        }
        public int PreAmpGainMaximumNumber
        {
            get;
            internal set;
        }
        public float[] VSSpeeds
        {
            get;
            internal set;
        }
    }
}
