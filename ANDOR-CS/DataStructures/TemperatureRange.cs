using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.DataStructures
{
    /// <summary>
    /// Stores temperature range (min, max)
    /// </summary>
    public struct TemperatureRange
    {
        /// <summary>
        /// Minimum temperature
        /// </summary>
        public int Minimum
        {
            get;
            private set;
        }

        /// <summary>
        /// Maximum temperature
        /// </summary>
        public int Maximum
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes new instance of <see cref="TemperatureRange"/>
        /// </summary>
        /// <param name="min">A value to store in Minimum field</param>
        /// <param name="max">A value to store in Maximum field</param>
        public TemperatureRange(int min, int max)
        {
            Minimum = min;
            Maximum = max;
        }

    }

}
