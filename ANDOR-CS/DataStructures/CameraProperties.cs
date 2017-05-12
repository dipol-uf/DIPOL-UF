using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Enums;

namespace ANDOR_CS.DataStructures
{
    /// <summary>
    /// Stores information about camera-specific properties
    /// </summary>
    public struct CameraProperties
    {
        /// <summary>
        /// If available, stores the range of temperatures to which camera can be cooled
        /// </summary>
        public TemperatureRange AllowedTemperatures
        {
            get;
            internal set;
        }

        /// <summary>
        /// If available, stores the size of a 2D detector (in pixels)
        /// </summary>
        public DetectorSize DetectorSize
        {
            get;
            internal set;
        }

        /// <summary>
        /// If available, indicates if camera has internal mechanical shutter
        /// </summary>
        public bool HasInternalMechanicalShutter
        {
            get;
            internal set;
        }

        /// <summary>
        /// An array of bit-depths for each available AD converter
        /// </summary>
        public int[] ADConverters
        {
            get;
            internal set;
        }

        /// <summary>
        /// An array of available amplifiers with respective maximum allowed horizontal speed
        /// </summary>
        public Tuple<string, OutputAmplification, float>[] Amplifiers
        {
            get;
            internal set;
        }

        /// <summary>
        /// An array of available pre amp gain settings
        /// </summary>
        public string[] PreAmpGains
        {
            get;
            internal set;
        }

          /// <summary>
        /// An array of available Vertical Speeds (in us)
        /// </summary>
        public float[] VSSpeeds
        {
            get;
            internal set;
        }
    }
}
