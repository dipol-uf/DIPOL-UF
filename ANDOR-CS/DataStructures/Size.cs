using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.DataStructures
{
    public struct Size
    {
        public int Horizontal
        {
            get;
            internal set;
        }
        public int Vertical
        {
            get;
            internal set;
        }

        public Size(int width, int height)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException($"{nameof(width)} should be greater than or equal to {0} ({width} >= {0})");

            if (height < 0)
                throw new ArgumentOutOfRangeException($"{nameof(height)} should be greater than or equal to {0} ({height} >= {0})");

            Horizontal = width;
            Vertical = height;
        }
    }
}
