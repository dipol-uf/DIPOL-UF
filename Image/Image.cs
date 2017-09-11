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

namespace DipolImage
{
    public class Image
    {
        private Array baseArray;

        public int Width
        {
            get;
            private set;
        }
        public int Height
        {
            get;
            private set;
        }

        public object this[int i, int j]
        {
            get => baseArray.GetValue(i * Width + j);
            set => baseArray.SetValue(value, i * Width + j);
        }

        public Image(Array initialArray, int width, int height)
        {
            switch (initialArray.GetValue(0))
            {
                case Int32 x:
                    baseArray = Array.CreateInstance(typeof(Int32), width * height);
                    break;
                case UInt16 x:
                    baseArray = Array.CreateInstance(typeof(UInt16), width * height);
                    break;
                default:
                    throw new Exception();
            }

            Array.Copy(initialArray, baseArray, width * height);

            Width = width;
            Height = height;
        }

        public byte[] GetBytes()
        {
            var size = System.Runtime.InteropServices.Marshal.SizeOf(this[0, 0]);

            var byteArray = new byte[Width * Height * size];

            switch (this[0, 0])
            {
                case Int32 x:
                    for (int i = 0; i < Height; i++)
                        for (int j = 0; j < Width; j++)
                            Array.Copy(BitConverter.GetBytes((Int32)this[i, j]), 0, byteArray, (i * Width + j) * size, size);
                    break;
                case UInt16 x:
                    for (int i = 0; i < Height; i++)
                        for (int j = 0; j < Width; j++)
                            Array.Copy(BitConverter.GetBytes((UInt16)this[i, j]), 0, byteArray, (i * Width + j) * size, size);
                    break;
                default:
                    throw new Exception();
            }

            return byteArray;
        }
    }
}
