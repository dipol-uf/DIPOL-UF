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

namespace Image
{
    public class Image<T> where T: struct
    {
        private T[] baseArray;

        public int Width
        {
            get;
            set;
        }

        public int Height
        {
            get;
            set;
        }

        public Image(T[] data, int width, int height)
        {
            var type = typeof(T);

            if (type != typeof(Int16) |
                type != typeof(Int32) |
                type != typeof(UInt16) |
                type != typeof(UInt32) |
                type != typeof(Byte) |
                type != typeof(Single) |
                type != typeof(Double))
                throw new ArgumentException($"Provided type {type} is not supported");

            baseArray = new T[data.Length];
            Array.Copy(data, baseArray, data.Length);

            Width = width;
            Height = height;
        }
    }
}
