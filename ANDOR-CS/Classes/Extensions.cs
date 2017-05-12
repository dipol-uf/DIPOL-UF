using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ANDOR_CS.Classes
{ 
    /// <summary>
    /// Helper class that can be used to retrieve all flags set in a given <see cref="Enum"/> mask
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Returns all flags that are set in a given <see cref="Enum"/> mask
        /// </summary>
        /// <param name="enumType"><see cref="Type"/> of <see cref="Enum"/></param>
        /// <param name="value">Mask</param>
        /// <exception cref="ArgumentException"/>
        /// <returns>An array of <see cref="String"/> representations of <see cref="Enum"/> flags</returns>
        public static string[] GetEnumNames(Type enumType, Enum value)
        {
            if (!enumType.IsSubclassOf(typeof(Enum)))
                throw new ArgumentException($"{enumType} should be an Enum-based type.");

            return Enum.GetValues(enumType).Cast<Enum>().Where((val) => value.HasFlag(val)).Select((x) => x.ToString()).ToArray();
                        
        }


        /// <summary>
        /// Returns the index of item in specified collection.
        /// </summary>
        /// <typeparam name="T">Any type that supports comparison using <see cref="EqualityComparer{T}.Equals(T, T)"/></typeparam>
        /// <param name="collection">Collection where to look for an item</param>
        /// <param name="item">An item of which to find index</param>
        /// <returns>-1 if item is not in the collection, index, otherwise</returns>
        public static int IndexOf<T>(this IEnumerable<T> collection, T item)
        {
            // Gets enumerator
            var enumer = collection.GetEnumerator();

            // Default return value
            int index = -1;

            // While enumerator.MoveNext() returns true
            // Cycle loop, increment counter
            for (int counter = 0; enumer.MoveNext(); counter++)
            {
                // If according to default comparer current element equals to item
                if (EqualityComparer<T>.Default.Equals(enumer.Current, item))
                {
                    // Save the index and stop the loop
                    index = counter;
                    break;
                }
            }

            return index;

        }
    }
}
