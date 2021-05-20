#nullable enable
#pragma warning disable 1591
using System.Runtime.Serialization;

namespace ANDOR_CS.AcquisitionMetadata
{
    [DataContract]
    public enum FrameType : byte
    {
        [EnumMember]
        Light,
        [EnumMember]
        Dark,
        [EnumMember]
        Bias
    }
}
