using System.Runtime.Serialization;

namespace ANDOR_CS.Enums
{
   [DataContract]
    public enum ShutterMode : int
    {
        [EnumMember]
        FullyAuto = 0,
        [EnumMember]
        PermanentlyOpen = 1,
        [EnumMember]
        PermanentlyClosed = 2,
        [EnumMember]
        OpenForFVBSeries = 4,
        [EnumMember]
        OpenForAnySeries = 5
    }
}
