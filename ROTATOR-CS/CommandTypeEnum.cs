using System.Runtime.Serialization;

namespace ROTATOR_CS
{
    [DataContract]
    public enum CommandType : byte
    {
        [EnumMember]
        Unused = 0,

        [EnumMember]
        Absolute = 0,
        [EnumMember]
        Relative = 1,
        [EnumMember]
        Coordinates = 2,

        [EnumMember]
        Start = 0,
        [EnumMember]
        Stop = 1,
        [EnumMember]
        Status = 2,

        [EnumMember]
        Add = 0,
        [EnumMember]
        Subtract = 1,
        [EnumMember]
        Multiply = 2,
        [EnumMember]
        Divide = 3,
        [EnumMember]
        Modulo = 4,
        [EnumMember]
        And = 5,
        [EnumMember]
        Or = 6,
        [EnumMember]
        Xor = 7,
        [EnumMember]
        Not = 8,
        [EnumMember]
        Load = 9,
        [EnumMember]
        Swap = 10,

        [EnumMember]
        AllFlags = 0,
        [EnumMember]
        TimeoutFlag = 1,
        [EnumMember]
        AlarmFlag = 2,
        [EnumMember]
        DeviationFlag = 3,
        [EnumMember]
        PositionFlag = 4,
        [EnumMember]
        ShutdownFlag = 5
    }
}
