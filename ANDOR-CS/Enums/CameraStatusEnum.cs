using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SDK = ATMCD64CS.AndorSDK;

namespace ANDOR_CS.Enums
{
    public enum CameraStatus : uint
    {
        Idle = SDK.DRV_IDLE,
        TemperatureCycle = SDK.DRV_TEMPCYCLE,
        Acquiring = SDK.DRV_ACQUIRING,
        AccumulateCycleTimeNotMet = SDK.DRV_ACCUM_TIME_NOT_MET,
        KineticCycleTimeNotMet = SDK.DRV_KINETIC_TIME_NOT_MET,
        CommunicationError = SDK.DRV_ERROR_ACK,
        AcquisitionBufferRate = SDK.DRV_ACQ_BUFFER,
        CameraMemoryFull = SDK.DRV_ACQ_DOWNFIFO_FULL,
        SpoolBufferOverflow = SDK.DRV_SPOOLERROR
    }
}
