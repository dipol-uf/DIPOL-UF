using System.Runtime.Serialization;

namespace DIPOL_Remote.Enums
{
    [DataContract]
    public enum AcquisitionEventType : int
    {
        [EnumMember]
        Started = 1,
        [EnumMember]
        Finished = 2,
        [EnumMember]
        StatusChecked = 3,
        [EnumMember]
        ErrorReturned = 4,
        [EnumMember]
        Aborted = 5           
    }
}
