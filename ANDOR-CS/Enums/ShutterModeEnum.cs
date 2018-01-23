using System.Runtime.Serialization;
using System.ComponentModel;

namespace ANDOR_CS.Enums
{
   [DataContract]
    public enum ShutterMode : int
    {
        [Description("Fully Automatic")]
        [EnumMember]
        FullyAuto = 0,
        [Description("Permanently Open")]
        [EnumMember]
        PermanentlyOpen = 1,
        [Description("Permanently Closed")]
        [EnumMember]
        PermanentlyClosed = 2,
        [Description("Open for Full Vertical Binning")]
        [EnumMember]
        OpenForFvbSeries = 4,
        [Description("Open for Any Series")]
        [EnumMember]
        OpenForAnySeries = 5
    }
}
