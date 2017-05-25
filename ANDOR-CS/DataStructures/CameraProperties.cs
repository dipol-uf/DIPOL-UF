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
        public (float Minimum, float Maximum) AllowedTemperatures
        {
            get;
            internal set;
        }

        /// <summary>
        /// If available, stores the size of a 2D detector (in pixels)
        /// </summary>
        public Size DetectorSize
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
        public (string Name, OutputAmplification Amplifier, float MaxSpeed)[] Amplifiers
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
