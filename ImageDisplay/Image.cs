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

        private Type type;

        public Type UnderlyingType => type;        

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

        public T Get<T>(int i, int j)
            => ((T[])baseArray)[i * Width + j];
        public void Set<T>(T value, int i, int j)
            => ((T[])baseArray)[i * Width + j] = value;
            

        public Image(Array initialArray, int width, int height)
        {
            switch (initialArray.GetValue(0))
            {
                case UInt32 x:
                    baseArray = new UInt32[width * height];
                    type = typeof(UInt32);
                    break;
                case Int32 x:
                    baseArray = new Int32[width * height];
                    type = typeof(Int32);
                    break;
                case UInt16 x:
                    baseArray = new UInt16[width * height];
                    type = typeof(UInt16);
                    break;
                case Int16 x:
                    baseArray = new Int16[width * height];
                    type = typeof(Int16);
                    break;
                case Single x:
                    baseArray = new Single[width * height];
                    type = typeof(Single);
                    break;
                case Double x:
                    baseArray = new Double[width * height];
                    type = typeof(Double);
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
            //var size = System.Runtime.InteropServices.Marshal.SizeOf(this[0, 0]);

            byte[] byteArray;
            if (type == typeof(UInt16))
            {
                int size = sizeof(UInt16);
                byteArray = new byte[Width * Height * size];
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<UInt16>(i, j)), 0, byteArray, (i * Width + j) * size, size);
            }
            else if (type == typeof(Int16))
            {
                int size = sizeof(Int16);
                byteArray = new byte[Width * Height * size];
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<Int16>(i, j)), 0, byteArray, (i * Width + j) * size, size);
            }
            else if (type == typeof(UInt32))
            {
                int size = sizeof(UInt32);
                byteArray = new byte[Width * Height * size];
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<UInt32>(i, j)), 0, byteArray, (i * Width + j) * size, size);
            }
            else if (type == typeof(Int32))
            {
                int size = sizeof(Int32);
                byteArray = new byte[Width * Height * size];
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<Int32>(i, j)), 0, byteArray, (i * Width + j) * size, size);
            }
            else throw new Exception();
            

            return byteArray;
        }

        public object Max()
        {
            object max = null;

            if (type == typeof(UInt16))
            {
                UInt16 localMax = UInt16.MinValue;
                UInt16 localVal = localMax;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<UInt16>(i, j)) > localMax)
                            localMax = localVal;

                max = localMax;
            }
            else if (type == typeof(Int16))
            {
                Int16 localMax = Int16.MinValue;
                Int16 localVal = localMax;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<Int16>(i, j)) > localMax)
                            localMax = localVal;

                max = localMax;
            }
            else if (type == typeof(UInt32))
            {
                UInt32 localMax = UInt32.MinValue;
                UInt32 localVal = localMax;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<UInt32>(i, j)) > localMax)
                            localMax = localVal;

                max = localMax;
            }
            else if (type == typeof(Int32))
            {
                Int32 localMax = Int32.MinValue;
                Int32 localVal = localMax;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<Int32>(i, j)) > localMax)
                            localMax = localVal;

                max = localMax;
            }
            else throw new Exception();

            //switch (this[0, 0])
            //{
            //    case UInt16 x:
            //        max = UInt16.MinValue;
            //        while (enm.MoveNext())
            //            if ((UInt16)enm.Current > max)
            //                max = (UInt16)enm.Current;
            //        break;
            //    default:
            //        throw new Exception();

            //}

            return max;
        }

        public object Min()
        {
            var enm = baseArray.GetEnumerator();

            object min = null;

            if (type == typeof(UInt16))
            {
                UInt16 localMin = UInt16.MaxValue;
                UInt16 localVal = localMin;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<UInt16>(i, j)) < localMin)
                            localMin = localVal;

                min = localMin;
            }
            else if (type == typeof(Int16))
            {
                Int16 localMin = Int16.MinValue;
                Int16 localVal = localMin;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<Int16>(i, j)) < localMin)
                            localMin = localVal;

                min = localMin;
            }
            else if (type == typeof(Int32))
            {
                Int32 localMin = Int32.MinValue;
                Int32 localVal = localMin;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<Int32>(i, j)) < localMin)
                            localMin = localVal;

                min = localMin;
            }
            else if (type == typeof(UInt32))
            {
                UInt32 localMin = UInt32.MinValue;
                UInt32 localVal = localMin;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<UInt32>(i, j)) < localMin)
                            localMin = localVal;

                min = localMin;
            }
            else throw new Exception();

            //switch (this[0, 0])
            //{
            //    case UInt16 x:
            //        min = UInt16.MaxValue;
            //        while (enm.MoveNext())
            //            if ((UInt16)enm.Current < min)
            //                min = (UInt16)enm.Current;
            //        break;
            //    default:
            //        throw new Exception();

            //}

            return min;
        }

        public Image Copy()
            => new Image(baseArray, Width, Height);

        public void CopyTo(Image im)
        {
            if (im.type != type || im.baseArray.Length != baseArray.Length)
                im.baseArray = Array.CreateInstance(type, baseArray.Length);            

            Array.Copy(baseArray, im.baseArray, baseArray.Length);

            im.Width = Width;
            im.Height = Height;
            im.type = type;
        }

        public Image Clamp(double low, double high)
        {
            //var result = Copy();

            if (type == typeof(UInt16))
            {
                UInt16 locLow = (UInt16)(Math.Floor(low));
                UInt16 locHigh = (UInt16)(Math.Ceiling(high));
                for (int i = 0; i < this.Height; i++)
                    for (int j = 0; j < this.Width; j++)
                        if (this.Get<UInt16>(i, j) < locLow)
                            this.Set<UInt16>(locLow, i, j);
                        else if (this.Get<UInt16>(i, j) > locHigh)
                            this.Set<UInt16>(locHigh, i, j);
            }
            else if (type == typeof(Int16))
            {
                Int16 locLow = (Int16)(Math.Floor(low));
                Int16 locHigh = (Int16)(Math.Ceiling(high));
                for (int i = 0; i < this.Height; i++)
                    for (int j = 0; j < this.Width; j++)
                        if (this.Get<Int16>(i, j) < locLow)
                            this.Set<Int16>(locLow, i, j);
                        else if (this.Get<Int16>(i, j) > locHigh)
                            this.Set<Int16>(locHigh, i, j);
            }
            else if (type == typeof(UInt32))
            {
                UInt32 locLow = (UInt32)(Math.Floor(low));
                UInt32 locHigh = (UInt32)(Math.Ceiling(high));
                for (int i = 0; i < this.Height; i++)
                    for (int j = 0; j < this.Width; j++)
                        if (this.Get<UInt32>(i, j) < locLow)
                            this.Set<UInt32>(locLow, i, j);
                        else if (this.Get<UInt32>(i, j) > locHigh)
                            this.Set<UInt32>(locHigh, i, j);
            }
            else if (type == typeof(Int32))
            {
                Int32 locLow = (Int32)(Math.Floor(low));
                Int32 locHigh = (Int32)(Math.Ceiling(high));
                for (int i = 0; i < this.Height; i++)
                    for (int j = 0; j < this.Width; j++)
                        if (this.Get<Int32>(i, j) < locLow)
                            this.Set<Int32>(locLow, i, j);
                        else if (this.Get<Int32>(i, j) > locHigh)
                            this.Set<Int32>(locHigh, i, j);
            }
            else throw new Exception();

            //switch (result[0, 0])
            //{
            //    case Int16 x:
            //       dynamic locLow = (Int16)(Math.Floor(low));
            //       dynamic locHigh = (Int16)(Math.Ceiling(high));
            //        for (int i = 0; i < result.Width * result.Height; i++)
            //            if ((Int16)result.baseArray.GetValue(i) < locLow)
            //                result.baseArray.SetValue(locLow, i);
            //            else if ((Int16)result.baseArray.GetValue(i) > locHigh)
            //                result.baseArray.SetValue(locHigh, i);
            //        break;
            //    case UInt16 x:
            //        locLow = (UInt16)(Math.Floor(low));
            //        locHigh = (UInt16)(Math.Ceiling(high));
            //        for (int i = 0; i < result.Width * result.Height; i++)
            //            if ((UInt16)result.baseArray.GetValue(i) < locLow)
            //                result.baseArray.SetValue(locLow, i);
            //            else if ((UInt16)result.baseArray.GetValue(i) > locHigh)
            //                result.baseArray.SetValue(locHigh, i);
            //        break;
            //    default:
            //        throw new Exception();
            //}

            return this;
        }

        public Image Scale()
        {
           // var result = Copy();

            var min = this.Min();
            var max = this.Max();

            if (type == typeof(UInt16))
            {
                UInt16 globMin = UInt16.MinValue;
                UInt16 globMax = UInt16.MaxValue;
                UInt16 locMax = (UInt16)max;
                UInt16 locMin = (UInt16)min;
                Func<UInt16, UInt16> converter = (y) => (UInt16)Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (y - locMin));

                for (int i = 0; i < this.Height; i++)
                    for (int j = 0; j < this.Width; j++)
                        this.Set<UInt16>(converter(this.Get<UInt16>(i, j)), i, j);

            }
            else if (type == typeof(Int16))
            {
                Int16 globMin = Int16.MinValue;
                Int16 globMax = Int16.MaxValue;
                Int16 locMax = (Int16)max;
                Int16 locMin = (Int16)min;
                Func<Int16, Int16> converter = (y) => (Int16)Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (y - locMin));

                for (int i = 0; i < this.Height; i++)
                    for (int j = 0; j < this.Width; j++)
                        this.Set<Int16>(converter(this.Get<Int16>(i, j)), i, j);
            }
            else if (type == typeof(UInt32))
            {
                UInt32 globMin = UInt32.MinValue;
                UInt32 globMax = UInt32.MaxValue;
                UInt32 locMax = (UInt32)max;
                UInt32 locMin = (UInt32)min;
                Func<UInt32, UInt32> converter = (y) => (UInt32)Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (y - locMin));

                for (int i = 0; i < this.Height; i++)
                    for (int j = 0; j < this.Width; j++)
                        this.Set<UInt32>(converter(this.Get<UInt32>(i, j)), i, j);
            }
            else if (type == typeof(Int32))
            {
                Int32 globMin = Int32.MinValue;
                Int32 globMax = Int32.MaxValue;
                Int32 locMax = (Int32)max;
                Int32 locMin = (Int32)min;
                Func<Int32, Int32> converter = (y) => (Int32)Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (y - locMin));

                for (int i = 0; i < this.Height; i++)
                    for (int j = 0; j < this.Width; j++)
                        this.Set<Int32>(converter(this.Get<Int32>(i, j)), i, j);
            }
            else throw new Exception();

            //{
            //    dynamic globMin = null;
            //    dynamic globMax = null;
            //    dynamic locMax = null;
            //    dynamic locMin = null;

            //    switch (this[0, 0])
            //    {
            //        case UInt16 x:
            //            globMin = UInt16.MinValue;
            //            globMax = UInt16.MaxValue;
            //            locMax = (UInt16)max;
            //            locMin = (UInt16)min;
            //            Func<UInt16, UInt16> converter = (y) => (UInt16)Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (y - locMin));

            //            for (int i = 0; i < result.Width * result.Height; i++)
            //                result.baseArray.SetValue(converter((UInt16)result.baseArray.GetValue(i)), i);
            //            //Parallel.For(0, result.Width * result.Height, (i) => result.baseArray.SetValue(converter((UInt16)result.baseArray.GetValue(i)), i));
            //            break;
            //        default:
            //            throw new Exception();
            //    }
            //}
            return this;
        }
    }
}
