using System.Runtime.Serialization;

namespace ANDOR_CS.Enums
{
    [DataContract]
    public enum TTLShutterSignal : int
    {
        [EnumMember]
        Low = 0,
        [EnumMember]
        High = 1
    }
}
