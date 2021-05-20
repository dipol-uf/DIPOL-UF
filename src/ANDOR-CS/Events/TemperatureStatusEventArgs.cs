//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.


using System;
using System.Runtime.Serialization;

using ANDOR_CS.Enums;

namespace ANDOR_CS.Events
{

    /// <summary>
    /// Stores the event arguments for all Acquisition-based events
    /// </summary>
    [DataContract]
    public class TemperatureStatusEventArgs : EventArgs
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
        /// Camera temperature.
        /// </summary>
        [DataMember]
        public float Temperature
        {
            get;
            private set;
        }

        /// <summary>
        /// Camera status at the moment of the event
        /// </summary>
        [DataMember]
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
            EventTime = DateTimeOffset.UtcNow;

            Status = status;
            Temperature = temp;
        }
    }
}
