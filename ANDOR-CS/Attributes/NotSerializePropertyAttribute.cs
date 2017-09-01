using System;
using System.Reflection;

namespace ANDOR_CS.Attributes
{
   [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class NotSerializePropertyAttribute : Attribute
    {
    }
}
