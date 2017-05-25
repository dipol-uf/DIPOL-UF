//    This file is part of Dipol-3 Camera Manager.

//    Dipol-3 Camera Manager is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    Dipol-3 Camera Manager is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with Dipol-3 Camera Manager.  If not, see<http://www.gnu.org/licenses/>.
//
//    Copyright 2017, Ilia Kosenkov, Tuorla Observatory, Finland

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
