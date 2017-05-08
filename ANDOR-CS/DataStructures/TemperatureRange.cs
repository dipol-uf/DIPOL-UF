using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.DataStructures
{
    public struct TemperatureRange
    {
        public int Minimum
        {
            get;
            internal set;
        }
        public int Maximum
        {
            get;
            internal set;
        }

        public TemperatureRange(int min, int max)
        {
            Minimum = min;
            Maximum = max;
        }

    }

}
