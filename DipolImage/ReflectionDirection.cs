using System;

namespace DipolImage
{
    [Flags]
    public enum ReflectionDirection : byte
    {
        NoReflection = 0,
        Horizontal = 1,
        Vertical = 2,
    }
}
