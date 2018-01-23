using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SerializationOrderAttribute : Attribute
    {
        public int Index
        {
            get;
            private set;
        }

        public SerializationOrderAttribute(int index)
            => Index = index;
    }
}
