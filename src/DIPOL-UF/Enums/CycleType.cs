using System;
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
        [Description("Circular Polarimetry")]
        CircularPolarimetry,
        
        [EnumMember]
        [Description("Photometry")]
        Photometry
    }

    internal static class CycleTypeExtensions
    {
        public static bool IsPolarimetric(this CycleType @this) => @this is not CycleType.Photometry;
        public static bool IsPhotometric(this CycleType @this) => @this is CycleType.Photometry;
        public static string ToEnumName(this CycleType @this) => @this switch
        {
            CycleType.LinearPolarimetry => nameof(CycleType.LinearPolarimetry),
            CycleType.Photometry => nameof(CycleType.Photometry),
            CycleType.CircularPolarimetry => nameof(CycleType.CircularPolarimetry),
            _ => throw new ArgumentException(nameof(@this))
        };
    }
}
