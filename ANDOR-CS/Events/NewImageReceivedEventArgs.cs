using System;
using System.Runtime.Serialization;

namespace ANDOR_CS.Events
{
    [DataContract]
    public class NewImageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Time stamp of the event
        /// </summary>
        [DataMember]
        public DateTime EventTime
        {
            get;
            private set;
        }

        [DataMember]
        public int First
        {
            get;
            private set;
        }

        [DataMember]
        public int Last
        {
            get;
            private set;
        }

        public NewImageReceivedEventArgs(int first, int last)
        {
            First = first;
            Last = last;
            EventTime = DateTime.Now;
        }

    }
}
