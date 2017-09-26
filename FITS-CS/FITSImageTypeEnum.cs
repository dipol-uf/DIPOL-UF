using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITS_CS
{
    public enum FITSImageType : short
    {
        UInt8 = 8,
        Int16 = 16,
        Int32 = 32,
        Single = -32,
        Double = -64
    }
}
