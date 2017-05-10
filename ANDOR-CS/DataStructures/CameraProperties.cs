using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public int[] ADConververts
        {
            get;
            internal set;
        }

        /// <summary>
        /// Number of different amplifiers available
        /// </summary>
        public int AmpNumber
        {
            get;
            internal set;
        }

        /// <summary>
        /// Maximum number of different gain settings available
        /// </summary>
        public int PreAmpGainMaximumNumber
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
