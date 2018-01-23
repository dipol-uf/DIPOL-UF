using System;

namespace ANDOR_CS.Attributes
{
    /// <inheritdoc />
    [AttributeUsage(AttributeTargets.Property)]
    public class SerializationOrderAttribute : Attribute
    {
        /// <summary>
        /// Serialization index.
        /// </summary>
        public int Index
        {
            get;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        public SerializationOrderAttribute(int index)
            => Index = index;
    }
}
