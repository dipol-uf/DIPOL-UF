using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class NonSerializedAttribute : Attribute
    {
    }
}
