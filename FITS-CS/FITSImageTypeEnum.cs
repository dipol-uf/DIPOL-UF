using System.Runtime.Serialization;

namespace FITS_CS
{
    [DataContract]
    public enum FITSImageType : short
    {
        [EnumMember]
        UInt8 = 8,
        [EnumMember]
        Int16 = 16,
        [EnumMember]
        Int32 = 32,
        [EnumMember]
        Single = -32,
        [EnumMember]
        Double = -64
    }
}
