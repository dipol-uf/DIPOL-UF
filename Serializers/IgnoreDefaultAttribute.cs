using System;

namespace Serializers
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class IgnoreDefaultAttribute : Attribute
    {
    }
}
