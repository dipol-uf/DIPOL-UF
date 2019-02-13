//    This file is part of Dipol-3 Camera Manager.

//    Dipol-3 Camera Manager is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    Dipol-3 Camera Manager is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with Dipol-3 Camera Manager.  If not, see<http://www.gnu.org/licenses/>.
//
//    Copyright 2017, Ilia Kosenkov, Tuorla Observatory, Finland

using System;
using System.Runtime.Serialization;

using ANDOR_CS.Enums;

namespace ANDOR_CS.Events
{

    /// <summary>
    /// Stores the event arguments for all Acquisition-based events
    /// </summary>
    [DataContract]
    public class AcquisitionStatusEventArgs : EventArgs
    {
        /// <summary>
        /// Time stamp of the event
        /// </summary>
        [DataMember]
        public DateTimeOffset EventTime
        {
            get;
            private set;
        }

        /// <summary>
        /// Camera status at the moment of the event
        /// </summary>
        [DataMember]
        public CameraStatus Status
        {
            get;
            private set;
        }

        [DataMember]
        public int KineticId { get; }

        [DataMember]
        public int AccumulationId { get; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public AcquisitionStatusEventArgs(CameraStatus status)
        {
            EventTime = DateTimeOffset.UtcNow;
            Status = status;
            KineticId = 0;
            AccumulationId = 0;
        }

        public AcquisitionStatusEventArgs(
            CameraStatus status, DateTimeOffset eventTime,
            int kineticId, int accumulationId)
        {
            EventTime = eventTime;
            Status = status;
            KineticId = kineticId;
            AccumulationId = accumulationId;
        }
    }
}
