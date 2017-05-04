using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS
{ 
    /// <summary>
    /// Helper class that can be used to retrieve all flags set in a given <see cref="Enum"/> mask
    /// </summary>
    public static class EnumNames
    {
        /// <summary>
        /// Returns all flags that are set in a given <see cref="Enum"/> mask
        /// </summary>
        /// <param name="enumType"><see cref="Type"/> of <see cref="Enum"/></param>
        /// <param name="value">Mask</param>
        /// <exception cref="ArgumentException"/>
        /// <returns>An array of <see cref="String"/> representations of <see cref="Enum"/> flags</returns>
        public static string[] GetName(Type enumType, Enum value)
        {
            if (!enumType.IsSubclassOf(typeof(Enum)))
                throw new ArgumentException($"{enumType} should be an Enum-based type.");

            return Enum.GetValues(enumType).Cast<Enum>().Where((val) => value.HasFlag(val)).Select((x) => x.ToString()).ToArray();
                        
        }
    }
}
