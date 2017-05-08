using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.DataStructures
{
    public struct DetectorSize
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

        public DetectorSize(int h, int v)
        {
            Horizontal = h;
            Vertical = v;
        }
    }
}
