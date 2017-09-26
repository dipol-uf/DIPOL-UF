using System.Runtime.Serialization;

namespace FITS_CS
{
    [DataContract]
    public enum FITSKeywordType : byte
    {
        [EnumMember]
        Logical = 1,
        [EnumMember]
        String = 2,
        [EnumMember]
        Integer = 3,
        [EnumMember]
        Float = 4,
        [EnumMember]
        Complex = 5,
        [EnumMember]
        Blank = 6,
        [EnumMember]
        Comment = 7
    }
}
