using System.Runtime.Serialization;

namespace DIPOL_UF.Enums
{
    [DataContract]
    public enum CycleType : byte
    {
        [EnumMember]
        Polarimetric,
        [EnumMember]
        Photometric
    }
}
