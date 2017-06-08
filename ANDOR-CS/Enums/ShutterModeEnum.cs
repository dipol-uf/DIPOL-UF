using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.Enums
{
    public enum ShutterMode : int
    {
        FullyAuto = 0,
        PermanentlyOpen = 1,
        PermanentlyClosed = 2,
        OpenForFVBSeries = 4,
        OpenForAnySeries = 5
    }
}
