using System.ComponentModel;
using System.Runtime.Serialization;

namespace DIPOL_UF.Enums
{
    [DataContract]
    public enum CycleType : byte
    {
        [EnumMember]
        [Description("Linear Polarimetry")]
        LinearPolarimetry,
        
        [EnumMember]
        [Description("Photometry")]
        Photometry
    }
}
