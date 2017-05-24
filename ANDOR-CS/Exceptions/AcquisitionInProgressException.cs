using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Classes;

namespace ANDOR_CS.Exceptions
{
    public class AcquisitionInProgressException : Exception
    {
        public AcquisitionInProgressException(string message) :
            base(message)
        { }

        public static void ThrowIfAcquiring(Camera cam)
        {
            if (cam.IsAcquiring)
                throw new AcquisitionInProgressException("Camera is acquiring image(s) at the moment.");
        }
        
    }
}
