using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.DataStructures
{
    public struct ActualTiming
    {
        public float ExposureTime
        {
            get;
            private set;
        }

        public float AccumulateCycleTime
        {
            get;
            private set;
        }

        public float KineticCycleTime
        {
            get;
            private set;
        }

        public ActualTiming(float exposure, float accumulateCycle, float kineticCycle)
        {
            ExposureTime = exposure;
            AccumulateCycleTime = accumulateCycle;
            KineticCycleTime = kineticCycle;
        }
    }

}
