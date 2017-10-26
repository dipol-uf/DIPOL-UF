using System.Runtime.Serialization;

namespace ROTATOR_CS
{
    [DataContract]
    enum ReturnStatus : byte
    {
        [EnumMember]
        UnknownError = 0,
        [EnumMember]
        Success = 100,
        [EnumMember]
        CommandLoadedIntoEEPROM = 101,
        [EnumMember]
        WrongChecksum = 1,
        [EnumMember]
        InvalidCommand = 2,
        [EnumMember]
        WrongType = 3,
        [EnumMember]
        InvalidValue = 4,
        [EnumMember]
        EEPROMLocked = 5,
        [EnumMember]
        CommandUnavailable = 6
    }
}
