using System;
using System.Runtime.Serialization;

namespace ROTATOR_CS
{
    [DataContract]
    public class RotatorEventArgs : EventArgs
    {
        [DataMember]
        public DateTime EventTime
        {
            get;
            private set;
        }

        [DataMember]
        public Reply Reply
        {
            get;
            private set;
        }

        public RotatorEventArgs(byte[] message)
        {
            Reply = new Reply(message);

            EventTime = DateTime.Now;
        }


    }
}
