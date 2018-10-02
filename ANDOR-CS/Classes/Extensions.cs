//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
//     
//     Permission is hereby granted, free of charge, to any person obtaining a copy
//     of this software and associated documentation files (the "Software"), to deal
//     in the Software without restriction, including without limitation the rights
//     to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//     copies of the Software, and to permit persons to whom the Software is
//     furnished to do so, subject to the following conditions:
//     
//     The above copyright notice and this permission notice shall be included in all
//     copies or substantial portions of the Software.
//     
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//     SOFTWARE.

using System;
using System.Collections.Generic;

namespace ANDOR_CS.Classes
{ 
    /// <summary>
    /// Helper class that can be used to retrieve all flags set in a given <see cref="Enum"/> mask
    /// </summary>
    public static class Extensions
    {
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
            using (var enumerate = collection.GetEnumerator())
            {
                // Default return value
                var index = -1;

                // While enumerator.MoveNext() returns true
                // Cycle loop, increment counter
                for (var counter = 0; enumerate.MoveNext(); counter++)
                {
                    // If according to default comparer current element equals to item
                    if (EqualityComparer<T>.Default.Equals(enumerate.Current, item))
                    {
                        index = counter;
                        break;
                    }

                    // Save the index and stop the loop
                }

                return index;
            }
        }
    }
}
