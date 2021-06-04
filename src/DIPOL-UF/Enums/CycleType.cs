using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DIPOL_UF.Enums
{
    [DataContract]
    [DescriptionProvider("CycleType")]
    public enum CycleType : byte
    {
        [EnumMember]
        LinearPolarimetry,
        
        [EnumMember]
        CircularPolarimetry,
        
        [EnumMember]
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
