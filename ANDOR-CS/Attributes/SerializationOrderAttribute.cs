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

        public bool All
        {
            get;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        public SerializationOrderAttribute(int index, bool serializeAll = false)
        {
            Index = index;
            All = serializeAll;
        }
    }
}
