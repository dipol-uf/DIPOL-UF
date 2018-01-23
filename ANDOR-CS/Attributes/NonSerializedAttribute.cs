using System;

namespace ANDOR_CS.Attributes
{
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NonSerializedAttribute : Attribute
    {
    }
}
