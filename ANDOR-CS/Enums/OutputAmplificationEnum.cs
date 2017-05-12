using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.Enums
{
    /// <summary>
    /// Different type of output amplifiers. Availalbe on iXon, Clara and Newton
    /// </summary>
    public enum OutputAmplification : uint
    {
        /// <summary>
        /// Electron multiplication. Not available for Clara 
        /// </summary>
        ElectromMultiplication = 0,

        /// <summary>
        /// Standard conventional 
        /// </summary>
        Conventional = 1,

        /// <summary>
        /// Only supported by Clara
        /// </summary>
        ExtendedNIR = 2
    }
}
