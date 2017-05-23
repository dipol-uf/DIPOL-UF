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
    public class AcquisitionStatusEventArgs : EventArgs
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
        /// Camera status at the moment of the event
        /// </summary>
        public CameraStatus Status
        {
            get;
            private set;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public AcquisitionStatusEventArgs(CameraStatus status)
            : base()
        {
            EventTime = DateTime.Now;

            Status = status;
        }
    }
}
