using System;
using System.Runtime.Serialization;
using System.Linq;

namespace ROTATOR_CS
{
    [DataContract]
    public struct Reply
    {
        public static readonly int ReplyLength = 9;

        public byte ReplyAddress
        {
            get;
            private set;
        }

        public byte ModuleAddress
        {
            get;
            private set;          
        }

        public ReturnStatus Status
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

            int checkSum = replyData.Take(ReplyLength - 1).Aggregate((sm, x) => sm += x);

            if ((checkSum & 0x00FF) != replyData[ReplyLength - 1])
                throw new ArgumentException("Wrong checksum in the reply.");
            

            ReplyAddress = replyData[0];
            ModuleAddress = replyData[1];
            Status = Enum.IsDefined(typeof(ReturnStatus), replyData[2]) ? (ReturnStatus)replyData[2] : ReturnStatus.UnknownError;
            Command = Enum.IsDefined(typeof(Command), replyData[3]) ? (Command)replyData[3] : Command.Unknown;
            ReturnValue = BitConverter.IsLittleEndian
                ? BitConverter.ToInt32(new[] { replyData[7], replyData[6], replyData[5], replyData[4] }, 0)
                : BitConverter.ToInt32(replyData, 4);


        }

        public override string ToString()
        {
            return String.Format("[{0,4}{1,4}{2,15}{3,10}{4,12}]", ReplyAddress, ModuleAddress, Command, ReturnValue, Status);
        }
    }
}
