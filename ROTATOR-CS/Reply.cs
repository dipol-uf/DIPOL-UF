using System;
using System.Runtime.Serialization;
using System.Linq;

namespace ROTATOR_CS
{
    [DataContract]
    public struct Reply
    {
        public static readonly int ReplyLength = 9;

        public int ReplyAddress
        {
            get;
            private set;
        }

        public int ModuleAddress
        {
            get;
            private set;          
        }

        public object Status
        {
            get;
            private set;
        }

        public Command Command
        {
            get;
            private set;       
        }

        public int ReturnValue
        {
            get;
            private set;
        }

        public Reply(byte[] replyData)
        {
            if (replyData == null)
                throw new ArgumentNullException($"Input parameter ({nameof(replyData)}) is null.");
            else if (replyData.Length != ReplyLength)
                throw new ArgumentException($"Reply length (in bytes) is invalid (expected {ReplyLength}, got {replyData.Length}).");

            int checkSum = 0;

            for (int i = 0; i < ReplyLength - 1; i++)
                checkSum += replyData[i];

            if ((checkSum & 0x00FF) != replyData[ReplyLength - 1])
                throw new ArgumentException("Wrong checksum in the reply.");
            

            ReplyAddress = replyData[0];
            ModuleAddress = replyData[1];
            // Status is not implemented yet!
            Status = null;
            Command = Enum.IsDefined(typeof(Command), replyData[3]) ? (Command)replyData[3] : Command.Unknown;
            ReturnValue = BitConverter.IsLittleEndian
                ? BitConverter.ToInt32(replyData, 4)
                : BitConverter.ToInt32(new[] { replyData[7], replyData[6], replyData[5], replyData[4] }, 0);


        }
    }
}
