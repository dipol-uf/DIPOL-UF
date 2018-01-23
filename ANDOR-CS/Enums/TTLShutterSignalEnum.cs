using System.Runtime.Serialization;

namespace ANDOR_CS.Enums
{
    [DataContract]
    public enum TtlShutterSignal : int
    {
        [EnumMember]
        Low = 0,
        [EnumMember]
        High = 1
    }
}
