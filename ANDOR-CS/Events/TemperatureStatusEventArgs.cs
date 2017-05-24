using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ANDOR_CS.Enums;

namespace ANDOR_CS.Events
{

    /// <summary>
    /// Stores the event arguments for all Acquisition-based events
    /// </summary>
    public class TemperatureStatusEventArgs : EventArgs
    {
        /// <summary>
        /// Time stamp of the event
        /// </summary>
        public DateTime EventTime
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Camera temperature.
        /// </summary>
        public float Temperature
        {
            get;
            private set;
        }

        /// <summary>
        /// Camera status at the moment of the event
        /// </summary>
        public TemperatureStatus Status
        {
            get;
            private set;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TemperatureStatusEventArgs(TemperatureStatus status, float temp)
            : base()
        {
            EventTime = DateTime.Now;

            Status = status;
            Temperature = temp;
        }
    }
}
