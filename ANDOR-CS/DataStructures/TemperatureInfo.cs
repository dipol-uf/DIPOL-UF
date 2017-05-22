using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Enums;

namespace ANDOR_CS.DataStructures
{

    public struct TemperatureInfo
    {
        public float Temperature
        {
            get;
            private set;
        }
        public TemperatureStatus Status
        {
            get;
            private set;
        }

        public TemperatureInfo(float temperature, TemperatureStatus status)
        {
            Temperature = temperature;
            Status = status;
        }
            
    }
}
