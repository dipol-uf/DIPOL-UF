using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.DataStructures
{
    /// <summary>
    /// Stores information about Andor software used
    /// </summary>
    public struct SoftwareVersion
    {
        /// <summary>
        /// EPROM
        /// </summary>
        public Version EPROM
        {
            get;
            internal set;
        }

        /// <summary>
        /// COF file
        /// </summary>
        public Version COFFile
        {
            get;
            internal set;
        }

        /// <summary>
        /// Version of the driver
        /// </summary>
        public Version Driver
        {
            get;
            internal set;
        }

        /// <summary>
        /// Version of the SDK dll used
        /// </summary>
        public Version Dll
        {
            get;
            internal set;
        }
    }
}
