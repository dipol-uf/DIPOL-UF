using System;

namespace Serializers
{
    /// <inheritdoc />
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SerializationOrderAttribute : Attribute
    {
        /// <summary>
        /// Serialization index.
        /// </summary>
        public int Index { get; }

        public bool All { get; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="serializeAll"></param>
        public SerializationOrderAttribute(int index, bool serializeAll = false)
        {
            Index = index;
            All = serializeAll;
        }
    }
}
