using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.DataStructures
{
    /// <summary>
    /// Stoers information about currently used hardware versions
    /// </summary>
    public struct HardwareVersion
    {
        /// <summary>
        /// Plug-in card version
        /// </summary>
        public Version PCB
        {
            get;
            internal set;
        }

        /// <summary>
        /// Flex 10K file version
        /// </summary>
        public Version Decode
        {
            get;
            internal set;
        }

        /// <summary>
        /// Camera firmware version
        /// </summary>
        public Version CameraFirmware
        {
            get;
            internal set;
        }
    }

}
