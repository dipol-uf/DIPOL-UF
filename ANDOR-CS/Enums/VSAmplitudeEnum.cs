using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.Enums
{
    /// <summary>
    /// Available vertical clock voltage amplitudes to set. 
    /// Not all camera support this feature, not all amplitudes may be available.
    /// </summary>
    public enum VSAmplitude : int
    {
        /// <summary>
        /// 0, default
        /// </summary>
        Normal = 0,

        /// <summary>
        /// +1
        /// </summary>
        Plus1 = 1,

        /// <summary>
        /// +2
        /// </summary>
        Plus2 = 2,

        /// <summary>
        /// +3
        /// </summary>
        Plus3 = 3,

        /// <summary>
        /// +4
        /// </summary>
        Plus4 = 4
    }
}
