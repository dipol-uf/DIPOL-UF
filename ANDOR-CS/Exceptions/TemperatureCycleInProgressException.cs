using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Classes;

namespace ANDOR_CS.Exceptions
{
    public class TemperatureCycleInProgressException : Exception
    {
        public TemperatureCycleInProgressException(string message) :
            base(message)
        { }

        public static void ThrowIfTempCycle(Camera cam)
        {
            if (cam.IsInTemperatureCycle)
                throw new AcquisitionInProgressException("Camera is in temperature cycle at the moment.");
        }
        
    }
}
