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
using System.Runtime.Serialization;
using System.Linq;
using System.Threading.Tasks;

namespace ImageDisplayLib
{
    [DataContract]
    public class Image
    {
        private static readonly int MaxImageSingleThreadSize = 512 * 768;
        private static readonly TypeCode[] allowedTypes =
         {
            TypeCode.Double,
            TypeCode.Single,
            TypeCode.UInt16,
            TypeCode.Int16,
            TypeCode.UInt32,
            TypeCode.Int32
        };

        [DataMember]
        private volatile bool IsParallelEnabled = true;

        [DataMember]
        private Array baseArray;

        [DataMember]
        private TypeCode typeCode;

        public static System.Collections.Generic.IReadOnlyCollection<TypeCode> AllowedTypes
            => allowedTypes as System.Collections.Generic.IReadOnlyCollection<TypeCode>;

        public TypeCode UnderlyingType => typeCode;

        [DataMember]
        public int Width
        {
            get;
            private set;
        }
        [DataMember]
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
            object val = initialArray.GetValue(0);

            if (!AllowedTypes.Contains(Type.GetTypeCode(val.GetType())))
                throw new ArgumentException($"Provided array's base type {val.GetType()} is not allowed.");

            switch (val)
            {
                case UInt32 x:
                    baseArray = new UInt32[width * height];
                    typeCode = TypeCode.UInt32;
                    break;
                case Int32 x:
                    baseArray = new Int32[width * height];
                    typeCode = TypeCode.Int32;
                    break;
                case UInt16 x:
                    baseArray = new UInt16[width * height];
                    typeCode = TypeCode.UInt16;
                    break;
                case Int16 x:
                    baseArray = new Int16[width * height];
                    typeCode = TypeCode.Int16;
                    break;
                case Single x:
                    baseArray = new Single[width * height];
                    typeCode = TypeCode.Single;
                    break;
                case Double x:
                    baseArray = new Double[width * height];
                    typeCode = TypeCode.Double;
                    break;
                default:
                    throw new Exception();
            }

            Array.Copy(initialArray, baseArray, width * height);

            Width = width;
            Height = height;
        }

        public Image(byte[] initialArray, int width, int height, TypeCode type)
        {
            if (!Enum.IsDefined(typeof(TypeCode), type))
                throw new ArgumentException($"Parameter type ({type}) is not defined in {typeof(TypeCode)}.");

            if (!AllowedTypes.Contains(type))
                throw new ArgumentException($"Specified type {type} is not allowed.");

            int size = 0;


            if (type == TypeCode.UInt16)
            {
                size = sizeof(UInt16);
                baseArray = new UInt16[width * height];
                Width = width;
                Height = height;
                this.typeCode = type;

                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Set<UInt16>(BitConverter.ToUInt16(initialArray, (i * width + j) * size), i, j);
            }
            else if (type == TypeCode.Int16)
            {
                size = sizeof(Int16);
                baseArray = new Int16[width * height];
                Width = width;
                Height = height;
                this.typeCode = type;

                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Set<Int16>(BitConverter.ToInt16(initialArray, (i * width + j) * size), i, j);
            }
            else if (type == TypeCode.UInt32)
            {
                size = sizeof(UInt32);
                baseArray = new UInt32[width * height];
                Width = width;
                Height = height;
                this.typeCode = type;

                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Set<UInt32>(BitConverter.ToUInt32(initialArray, (i * width + j) * size), i, j);
            }
            else if (type == TypeCode.Int32)
            {
                size = sizeof(Int32);
                baseArray = new Int32[width * height];
                Width = width;
                Height = height;
                this.typeCode = type;

                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Set<Int32>(BitConverter.ToInt32(initialArray, (i * width + j) * size), i, j);
            }
            else if (type == TypeCode.Single)
            {
                size = sizeof(Single);
                baseArray = new Single[width * height];
                Width = width;
                Height = height;
                this.typeCode = type;

                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Set<Single>(BitConverter.ToSingle(initialArray, (i * width + j) * size), i, j);
            }
            else if (type == TypeCode.Double)
            {
                size = sizeof(Double);
                baseArray = new Double[width * height];
                Width = width;
                Height = height;
                this.typeCode = type;

                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Set<Double>(BitConverter.ToDouble(initialArray, (i * width + j) * size), i, j);
            }
            else
                throw new Exception();
        }

        public byte[] GetBytes()
        {
            //var size = System.Runtime.InteropServices.Marshal.SizeOf(this[0, 0]);

            byte[] byteArray;
            if (typeCode == TypeCode.UInt16)
            {
                int size = sizeof(UInt16);
                byteArray = new byte[Width * Height * size];
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<UInt16>(i, j)), 0, byteArray, (i * Width + j) * size, size);
            }
            else if (typeCode == TypeCode.Int16)
            {
                int size = sizeof(Int16);
                byteArray = new byte[Width * Height * size];
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<Int16>(i, j)), 0, byteArray, (i * Width + j) * size, size);
            }
            else if (typeCode == TypeCode.UInt32)
            {
                int size = sizeof(UInt32);
                byteArray = new byte[Width * Height * size];
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<UInt32>(i, j)), 0, byteArray, (i * Width + j) * size, size);
            }
            else if (typeCode == TypeCode.Int32)
            {
                int size = sizeof(Int32);
                byteArray = new byte[Width * Height * size];
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<Int32>(i, j)), 0, byteArray, (i * Width + j) * size, size);
            }
            else if (typeCode == TypeCode.Single)
            {
                int size = sizeof(Single);
                byteArray = new byte[Width * Height * size];
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<Single>(i, j)), 0, byteArray, (i * Width + j) * size, size);
            }
            else if (typeCode == TypeCode.Double)
            {
                int size = sizeof(Double);
                byteArray = new byte[Width * Height * size];
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<Double>(i, j)), 0, byteArray, (i * Width + j) * size, size);
            }
            else throw new Exception();


            return byteArray;
        }

        public object Max()
        {
            object max = null;

            if (typeCode == TypeCode.UInt16)
            {
                UInt16 localMax = UInt16.MinValue;
                UInt16 localVal = localMax;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<UInt16>(i, j)) > localMax)
                            localMax = localVal;

                max = localMax;
            }
            else if (typeCode == TypeCode.Int16)
            {
                Int16 localMax = Int16.MinValue;
                Int16 localVal = localMax;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<Int16>(i, j)) > localMax)
                            localMax = localVal;

                max = localMax;
            }
            else if (typeCode == TypeCode.UInt32)
            {
                UInt32 localMax = UInt32.MinValue;
                UInt32 localVal = localMax;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<UInt32>(i, j)) > localMax)
                            localMax = localVal;

                max = localMax;
            }
            else if (typeCode == TypeCode.Int32)
            {
                Int32 localMax = Int32.MinValue;
                Int32 localVal = localMax;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<Int32>(i, j)) > localMax)
                            localMax = localVal;

                max = localMax;
            }
            else if (typeCode == TypeCode.Single)
            {
                Single localMax = Single.MinValue;
                Single localVal = localMax;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<Single>(i, j)) > localMax)
                            localMax = localVal;

                max = localMax;
            }
            else if (typeCode == TypeCode.Double)
            {
                Double localMax = Double.MinValue;
                Double localVal = localMax;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<Double>(i, j)) > localMax)
                            localMax = localVal;

                max = localMax;
            }
            else throw new Exception();



            return max;
        }

        public object Min()
        {
            var enm = baseArray.GetEnumerator();

            object min = null;

            if (typeCode == TypeCode.UInt16)
            {
                UInt16 localMin = UInt16.MaxValue;
                UInt16 localVal = localMin;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<UInt16>(i, j)) < localMin)
                            localMin = localVal;

                min = localMin;
            }
            else if (typeCode == TypeCode.Int16)
            {
                Int16 localMin = Int16.MaxValue;
                Int16 localVal = localMin;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<Int16>(i, j)) < localMin)
                            localMin = localVal;

                min = localMin;
            }
            else if (typeCode == TypeCode.Int32)
            {
                Int32 localMin = Int32.MaxValue;
                Int32 localVal = localMin;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<Int32>(i, j)) < localMin)
                            localMin = localVal;

                min = localMin;
            }
            else if (typeCode == TypeCode.UInt32)
            {
                UInt32 localMin = UInt32.MaxValue;
                UInt32 localVal = localMin;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<UInt32>(i, j)) < localMin)
                            localMin = localVal;

                min = localMin;
            }
            else if (typeCode == TypeCode.Single)
            {
                Single localMin = Single.MaxValue;
                Single localVal = localMin;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<Single>(i, j)) < localMin)
                            localMin = localVal;

                min = localMin;
            }
            else if (typeCode == TypeCode.Double)
            {
                Double localMin = Double.MaxValue;
                Double localVal = localMin;
                for (int i = 0; i < Height; i++)
                    for (int j = 0; j < Width; j++)
                        if ((localVal = Get<Double>(i, j)) < localMin)
                            localMin = localVal;

                min = localMin;
            }
            else throw new Exception();



            return min;
        }

        public Image Copy()
            => new Image(baseArray, Width, Height);

        public void CopyTo(Image im)
        {
            if (im.typeCode != typeCode || im.baseArray.Length != baseArray.Length)
                im.baseArray = Array.CreateInstance(
                    Type.GetType("System." + typeCode.ToString(), true, true),
                    baseArray.Length);

            Array.Copy(baseArray, im.baseArray, baseArray.Length);

            im.Width = Width;
            im.Height = Height;
            im.typeCode = typeCode;
        }

        public void Clamp(double low, double high)
        {
            if (typeCode == TypeCode.UInt16)
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
            else if (typeCode == TypeCode.Int16)
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
            else if (typeCode == TypeCode.UInt32)
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
            else if (typeCode == TypeCode.Int32)
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
            else if (typeCode == TypeCode.Single)
            {
                Single locLow = (Single)(low);
                Single locHigh = (Single)(high);
                for (int i = 0; i < this.Height; i++)
                    for (int j = 0; j < this.Width; j++)
                        if (this.Get<Single>(i, j) < locLow)
                            this.Set<Single>(locLow, i, j);
                        else if (this.Get<Single>(i, j) > locHigh)
                            this.Set<Single>(locHigh, i, j);
            }
            else if (typeCode == TypeCode.Double)
            {
                Double locLow = (Double)(low);
                Double locHigh = (Double)(high);
                for (int i = 0; i < this.Height; i++)
                    for (int j = 0; j < this.Width; j++)
                        if (this.Get<Double>(i, j) < locLow)
                            this.Set<Double>(locLow, i, j);
                        else if (this.Get<Double>(i, j) > locHigh)
                            this.Set<Double>(locHigh, i, j);
            }
            else throw new Exception();
        }

        public void Scale(double gMin, double gMax)
        {

            var min = this.Min();
            var max = this.Max();

            if (typeCode == TypeCode.UInt16)
            {
                UInt16 globMin = Convert.ToUInt16(gMin);
                UInt16 globMax = Convert.ToUInt16(gMax);
                UInt16 locMax = (UInt16)max;
                UInt16 locMin = (UInt16)min;

                Action<int> worker = (k) =>
                    {
                        for (int j = 0; j < Width; j++)
                            Set<UInt16>((UInt16)Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (Get<UInt16>(k, j) - locMin)), k, j);
                    };

                if (IsParallelEnabled && Width * Height > MaxImageSingleThreadSize)
                    Parallel.For(0, Height, worker);
                else
                    for (int i = 0; i < Height; i++)
                        worker(i);


            }
            else if (typeCode == TypeCode.Int16)
            {
                Int16 globMin = Convert.ToInt16(gMin);
                Int16 globMax = Convert.ToInt16(gMax);
                Int16 locMax = (Int16)max;
                Int16 locMin = (Int16)min;

                Action<int> worker = (k) =>
                {
                    for (int j = 0; j < Width; j++)
                        Set<Int16>((Int16)Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (Get<Int16>(k, j) - locMin)), k, j);
                };

                if (IsParallelEnabled && Width * Height > MaxImageSingleThreadSize)
                    Parallel.For(0, Height, worker);
                else
                    for (int i = 0; i < Height; i++)
                        worker(i);
            }
            else if (typeCode == TypeCode.UInt32)
            {
                UInt32 globMin = Convert.ToUInt32(gMin);
                UInt32 globMax = Convert.ToUInt32(gMax);
                UInt32 locMax = (UInt32)max;
                UInt32 locMin = (UInt32)min;

                Action<int> worker = (k) =>
                {
                    for (int j = 0; j < Width; j++)
                        Set<UInt32>((UInt32)Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (Get<UInt32>(k, j) - locMin)), k, j);
                };

                if (IsParallelEnabled && Width * Height > MaxImageSingleThreadSize)
                    Parallel.For(0, Height, worker);
                else
                    for (int i = 0; i < Height; i++)
                        worker(i);
            }
            else if (typeCode == TypeCode.Int32)
            {
                Int32 globMin = Convert.ToInt32(gMin);
                Int32 globMax = Convert.ToInt32(gMax);
                Int32 locMax = (Int32)max;
                Int32 locMin = (Int32)min;

                Action<int> worker = (k) =>
                {
                    for (int j = 0; j < Width; j++)
                        Set<Int32>((Int32)Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (Get<Int32>(k, j) - locMin)), k, j);
                };

                if (IsParallelEnabled && Width * Height > MaxImageSingleThreadSize)
                    Parallel.For(0, Height, worker);
                else
                    for (int i = 0; i < Height; i++)
                        worker(i);
            }
            else if (typeCode == TypeCode.Single)
            {
                Single globMin = Convert.ToSingle(gMin);
                Single globMax = Convert.ToSingle(gMax);
                Single locMax = (Single)max;
                Single locMin = (Single)min;

                Action<int> worker = (k) =>
                {
                    for (int j = 0; j < Width; j++)
                        Set<Single>((Single)Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (Get<Single>(k, j) - locMin)), k, j);
                };

                if (IsParallelEnabled && Width * Height > MaxImageSingleThreadSize)
                    Parallel.For(0, Height, worker);
                else
                    for (int i = 0; i < Height; i++)
                        worker(i);
            }
            else if (typeCode == TypeCode.Double)
            {
                Double globMin = Convert.ToDouble(gMin);
                Double globMax = Convert.ToDouble(gMax);
                Double locMax = (Double)max;
                Double locMin = (Double)min;

                Action<int> worker = (k) =>
                {
                    for (int j = 0; j < Width; j++)
                        Set<Double>(Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (Get<Double>(k, j) - locMin)), k, j);
                };

                if (IsParallelEnabled && Width * Height > MaxImageSingleThreadSize)
                    Parallel.For(0, Height, worker);
                else
                    for (int i = 0; i < Height; i++)
                        worker(i);
            }
            else throw new Exception();
        }

        public double Percentile(double lvl)
        {
            if (lvl < 0 | lvl > 1.0)
                throw new ArgumentOutOfRangeException($"{nameof(lvl)} parameter is out of range ({lvl} should be in [0, 1]).");

            if (typeCode == TypeCode.UInt16)
            {
                if (Math.Abs(lvl) < Double.Epsilon)
                    return (UInt16)Min();
                else if (Math.Abs(lvl - 1) < Double.Epsilon)
                    return (UInt16)Max();
                else
                {
                    var query = ((UInt16[])baseArray).OrderBy((x) => x);

                    int length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();
                }

            }
            else if (typeCode == TypeCode.Int16)
            {
                if (Math.Abs(lvl) < Double.Epsilon)
                    return (Int16)Min();
                else if (Math.Abs(lvl - 1) < Double.Epsilon)
                    return (Int16)Max();
                else
                {
                    var query = ((Int16[])baseArray).OrderBy((x) => x);

                    int length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();
                }

            }
            else if (typeCode == TypeCode.UInt32)
            {
                if (Math.Abs(lvl) < Double.Epsilon)
                    return (UInt32)Min();
                else if (Math.Abs(lvl - 1) < Double.Epsilon)
                    return (UInt32)Max();
                else
                {
                    var query = ((UInt32[])baseArray).OrderBy((x) => x);

                    int length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();
                }

            }
            else if (typeCode == TypeCode.Int32)
            {
                if (Math.Abs(lvl) < Double.Epsilon)
                    return (Int32)Min();
                else if (Math.Abs(lvl - 1) < Double.Epsilon)
                    return (Int32)Max();
                else
                {
                    var query = ((Int32[])baseArray).OrderBy((x) => x);

                    int length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();
                }

            }
            else if (typeCode == TypeCode.Single)
            {
                if (Math.Abs(lvl) < Double.Epsilon)
                    return (Single)Min();
                else if (Math.Abs(lvl - 1) < Double.Epsilon)
                    return (Single)Max();
                else
                {
                    var query = ((Single[])baseArray).OrderBy((x) => x);

                    int length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();
                }

            }
            else if (typeCode == TypeCode.Double)
            {
                if (Math.Abs(lvl) < Double.Epsilon)
                    return (Double)Min();
                else if (Math.Abs(lvl - 1) < Double.Epsilon)
                    return (Double)Max();
                else
                {
                    var query = ((Double[])baseArray).OrderBy((x) => x);

                    int length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();
                }

            }
            else throw new Exception();
        }

        public void AddScalar(double value)
        {
            if (UnderlyingType == TypeCode.UInt16)
            {
                UInt16[] data = (UInt16[])baseArray;
                for (int i = 0; i < data.Length; i++)
                    data[i] = Convert.ToUInt16(data[i] + value);
            }
            else if (UnderlyingType == TypeCode.Int16)
            {
                Int16[] data = (Int16[])baseArray;
                for (int i = 0; i < data.Length; i++)
                    data[i] = Convert.ToInt16(data[i] + value);
            }
            else if (UnderlyingType == TypeCode.UInt32)
            {
                UInt32[] data = (UInt32[])baseArray;
                for (int i = 0; i < data.Length; i++)
                    data[i] = Convert.ToUInt32(data[i] + value);
            }
            else if (UnderlyingType == TypeCode.Int32)
            {
                Int32[] data = (Int32[])baseArray;
                for (int i = 0; i < data.Length; i++)
                    data[i] = Convert.ToInt32(data[i] + value);
            }
            else if (UnderlyingType == TypeCode.Single)
            {
                Single[] data = (Single[])baseArray;
                for (int i = 0; i < data.Length; i++)
                    data[i] = Convert.ToSingle(data[i] + value);
            }
            else if (UnderlyingType == TypeCode.Double)
            {
                Double[] data = (Double[])baseArray;
                for (int i = 0; i < data.Length; i++)
                    data[i] = Convert.ToDouble(data[i] + value);
            }
            else
                throw new Exception();


        }

        public void MultiplyByScalar(double value)
        {
            if(UnderlyingType == TypeCode.UInt16)
            {
                UInt16[] data = (UInt16[])baseArray;
                for (int i = 0; i < data.Length; i++)
                    data[i] = Convert.ToUInt16(data[i] * value);
            }
            else if (UnderlyingType == TypeCode.Int16)
            {
                Int16[] data = (Int16[])baseArray;
                for (int i = 0; i < data.Length; i++)
                    data[i] = Convert.ToInt16(data[i] * value);
            }
            else if (UnderlyingType == TypeCode.UInt32)
            {
                UInt32[] data = (UInt32[])baseArray;
                for (int i = 0; i < data.Length; i++)
                    data[i] = Convert.ToUInt32(data[i] * value);
            }
            else if (UnderlyingType == TypeCode.Int32)
            {
                Int32[] data = (Int32[])baseArray;
                for (int i = 0; i < data.Length; i++)
                    data[i] = Convert.ToInt32(data[i] * value);
            }
            else if (UnderlyingType == TypeCode.Single)
            {
                Single[] data = (Single[])baseArray;
                for (int i = 0; i < data.Length; i++)
                    data[i] = Convert.ToSingle(data[i] * value);
            }
            else if (UnderlyingType == TypeCode.Double)
            {
                Double[] data = (Double[])baseArray;
                for (int i = 0; i < data.Length; i++)
                    data[i] = Convert.ToDouble(data[i] * value);
            }
            else
                throw new Exception();
        }

        public Image ReduceToDisplay(double min, double max)
        {

            Func<double, UInt16> convrtr = (x) => Convert.ToUInt16(1.0 * (x - min) * UInt16.MaxValue / (max - min));
            switch (UnderlyingType)
            {
                case TypeCode.Int16:
                    return new Image(((Int16[])baseArray)
                        .AsParallel()
                        .Select((x) => convrtr(x))
                        .ToArray(), Width, Height);
                case TypeCode.UInt16:
                    return this.Copy();
                case TypeCode.Int32:
                    return new Image(((Int32[])baseArray)
                        .AsParallel()
                        .Select((x) => convrtr(x))
                        .ToArray(), Width, Height);
                case TypeCode.UInt32:
                    return new Image(((UInt32[])baseArray)
                        .AsParallel()
                        .Select((x) => convrtr(x))
                        .ToArray(), Width, Height);
                case TypeCode.Single:
                    return new Image(((Single[])baseArray)
                        .AsParallel()
                        .Select((x) => convrtr(x))
                        .ToArray(), Width, Height);
                case TypeCode.Double:
                    return new Image(((Double[])baseArray)
                        .AsParallel()
                        .Select((x) => convrtr(x))
                        .ToArray(), Width, Height);
                default:
                    throw new Exception();
            }


        }

        public Image CastTo<Ts, Td>(Func<Ts, Td> cast)
           =>  (typeof(Ts) == Type.GetType("System." + UnderlyingType))                       
            ? new Image(((Ts[])baseArray)
                .AsParallel()
                .Select<Ts, Td>(cast)
                .ToArray(),
                Width, Height)
            : throw new TypeAccessException($"Source type {typeof(Ts)} differs from underlying type with code {UnderlyingType}.");
               
    }
}
