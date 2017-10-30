using System;
using System.Runtime.Serialization;
using System.Linq;

namespace ROTATOR_CS
{
    /// <summary>
    /// Represents a reply from a step motor.
    /// </summary>
    [DataContract]
    public struct Reply
    {
        /// <summary>
        /// Reply byte length.
        /// </summary>
        public static readonly int ReplyLength = 9;

        /// <summary>
        /// Sddress of the reply.
        /// </summary>
        [DataMember]
        public byte ReplyAddress
        {
            get;
            private set;
        }

        /// <summary>
        /// Addres of the module.
        /// </summary>
        [DataMember]
        public byte ModuleAddress
        {
            get;
            private set;          
        }

        /// <summary>
        /// Status of a command execution.
        /// </summary>
        [DataMember]
        public ReturnStatus Status
        {
            get;
            private set;
        }

        /// <summary>
        /// Sent command.
        /// </summary>
        [DataMember]
        public Command Command
        {
            get;
            private set;       
        }

        /// <summary>
        /// Return value, Stores either value sent to motor or step motor parameter value if
        /// <see cref="CommandType"/> is Get***.
        /// </summary>
        [DataMember]
        public int ReturnValue
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructs reply from COM-port byte reply.
        /// </summary>
        /// <param name="replyData">COM-port raw reply.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException"/>
        public Reply(byte[] replyData)
        {
            // Throws if input is null
            if (replyData == null)
                throw new ArgumentNullException($"Input parameter ({nameof(replyData)}) is null.");
            // Throws if array is of wrong length
            else if (replyData.Length != ReplyLength)
                throw new ArgumentException($"Reply length (in bytes) is invalid (expected {ReplyLength}, got {replyData.Length}).");

            // Calculates checksum
            int checkSum = 0;
            for (int i = 0; i < ReplyLength - 1; i++)
                checkSum += replyData[i];

            // Checksum checked
            if ((checkSum & 0x00_00_00_FF) != replyData[ReplyLength - 1])
                throw new ArgumentException("Wrong checksum in the reply.");
            
            // 0-th byte is reply address
            ReplyAddress = replyData[0];
            // 1-st is module address
            ModuleAddress = replyData[1];
            // 2-nd is status (coerced to enum)
            Status = Enum.IsDefined(typeof(ReturnStatus), replyData[2]) ? (ReturnStatus)replyData[2] : ReturnStatus.UnknownError;
            // 3-rd is command for which respond is received (coerced to enum)
            Command = Enum.IsDefined(typeof(Command), replyData[3]) ? (Command)replyData[3] : Command.Unknown;
            // Bytes 4 to 7 are Int32 return value. 
            // Step motor returns it in Most Significant Byte First format, so 
            // for LittleEndian environemnts sequence should be reversed
            ReturnValue = BitConverter.IsLittleEndian
                ? BitConverter.ToInt32(new[] { replyData[7], replyData[6], replyData[5], replyData[4] }, 0)
                : BitConverter.ToInt32(replyData, 4);

        }

        /// <summary>
        /// Standard string representation, 47 symbols long.
        /// </summary>
        /// <returns>String representation of <see cref="Reply"/>.</returns>
        public override string ToString()
        {
            return String.Format("[{0,2};{1,2};{2,15};{3,12};{4,10}]", ReplyAddress, ModuleAddress, Command,
                Status, ReturnValue);
        }
    }
}
