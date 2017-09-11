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

namespace ImageDisplayLib
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
                case Int16 x:
                    baseArray = Array.CreateInstance(typeof(Int16), width * height);
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
                case Int16 x:
                    for (int i = 0; i < Height; i++)
                        for (int j = 0; j < Width; j++)
                            Array.Copy(BitConverter.GetBytes((Int16)this[i, j]), 0, byteArray, (i * Width + j) * size, size);
                    break;
                default:
                    throw new Exception();
            }

            return byteArray;
        }

        public object Max()
        {
            var enm = baseArray.GetEnumerator();

            dynamic max = null;

            switch (this[0, 0])
            {
                case UInt16 x:
                    max = UInt16.MinValue;
                    while (enm.MoveNext())
                        if ((UInt16)enm.Current > max)
                            max = (UInt16)enm.Current;
                    break;
                default:
                    throw new Exception();

            }

            return max;
        }

        public object Min()
        {
            var enm = baseArray.GetEnumerator();

            dynamic min = null;

            switch (this[0, 0])
            {
                case UInt16 x:
                    min = UInt16.MaxValue;
                    while (enm.MoveNext())
                        if ((UInt16)enm.Current < min)
                            min = (UInt16)enm.Current;
                    break;
                default:
                    throw new Exception();

            }

            return min;
        }

        public Image Copy()
            => new Image(baseArray, Width, Height);

        public Image Clamp(double low, double high)
        {
            var result = Copy();

            switch (result[0, 0])
            {
                case Int16 x:
                   dynamic locLow = (Int16)(Math.Floor(low));
                   dynamic locHigh = (Int16)(Math.Ceiling(high));
                    for (int i = 0; i < result.Width * result.Height; i++)
                        if ((Int16)result.baseArray.GetValue(i) < locLow)
                            result.baseArray.SetValue(locLow, i);
                        else if ((Int16)result.baseArray.GetValue(i) > locHigh)
                            result.baseArray.SetValue(locHigh, i);
                    break;
                case UInt16 x:
                    locLow = (UInt16)(Math.Floor(low));
                    locHigh = (UInt16)(Math.Ceiling(high));
                    for (int i = 0; i < result.Width * result.Height; i++)
                        if ((UInt16)result.baseArray.GetValue(i) < locLow)
                            result.baseArray.SetValue(locLow, i);
                        else if ((UInt16)result.baseArray.GetValue(i) > locHigh)
                            result.baseArray.SetValue(locHigh, i);
                    break;
                default:
                    throw new Exception();
            }

            return result;
        }

        public Image Scale()
        {
            var result = Copy();

            var min = result.Min();
            var max = result.Max();

            dynamic globMin = null;
            dynamic globMax = null;
            dynamic locMax = null;
            dynamic locMin = null;

            switch (this[0, 0])
            {
                case UInt16 x:
                    globMin = UInt16.MinValue;
                    globMax = UInt16.MaxValue;
                    locMax = (UInt16)max;
                    locMin = (UInt16)min;
                    Func<UInt16, UInt16> converter = (y) => (UInt16)Math.Floor(globMin + 1.0*(globMax - globMin) / (locMax - locMin) * (y - locMin));

                    //for (int i = 0; i < result.Width * result.Height; i++)
                    //    result.baseArray.SetValue(converter((UInt16)result.baseArray.GetValue(i)), i);
                    Parallel.For(0, result.Width * result.Height, (i) => result.baseArray.SetValue(converter((UInt16)result.baseArray.GetValue(i)), i));
                    break;
                default:
                    throw new Exception();
            }

            return result;
        }
    }
}
