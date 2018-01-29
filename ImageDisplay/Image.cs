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
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DipolImage
{
    [DataContract]
    public class Image
    {
        private const int MaxImageSingleThreadSize = 512 * 768;

        private static readonly TypeCode[] AllowedTypes =
         {
            TypeCode.Double,
            TypeCode.Single,
            TypeCode.UInt16,
            TypeCode.Int16,
            TypeCode.UInt32,
            TypeCode.Int32
        };

        [DataMember]
        private volatile bool _isParallelEnabled = true;

        [DataMember]
        private Array _baseArray;

        [DataMember]
        private TypeCode _typeCode;

        public static IReadOnlyCollection<TypeCode> AllowedPixelTypes
            => Array.AsReadOnly(AllowedTypes);

        public TypeCode UnderlyingType => _typeCode;

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
            get => _baseArray.GetValue(i * Width + j);
            set => _baseArray.SetValue(value, i * Width + j);
        }

        public T Get<T>(int i, int j)
            => ((T[])_baseArray)[i * Width + j];
        public void Set<T>(T value, int i, int j)
            => ((T[])_baseArray)[i * Width + j] = value;

        public Image(Array initialArray, int width, int height)
        {
            if (initialArray == null)
                throw new ArgumentNullException("Argument is null: " + nameof(initialArray));

            if(width < 1 || height < 1)
                throw new ArgumentOutOfRangeException($"Image size is incorrect [{width}, {height}].");

            var val = initialArray.GetValue(0);

            if (!AllowedTypes.Contains(Type.GetTypeCode(val.GetType())))
                throw new ArgumentException($"Provided array's base type {val.GetType()} is not allowed.");
            
            _typeCode = Type.GetTypeCode(val.GetType());
            if (!AllowedTypes.Contains(_typeCode))
                throw new ArgumentException();

            _baseArray = Array.CreateInstance(val.GetType(), width * height);
            Array.Copy(initialArray, _baseArray, width * height);
            Width = width;
            Height = height;
        }

        public Image(byte[] initialArray, int width, int height, TypeCode type)
        {
            if (initialArray == null)
                throw new ArgumentNullException("Argument is null: " + nameof(initialArray));

            if (width < 1 || height < 1)
                throw new ArgumentOutOfRangeException($"Image size is incorrect [{width}, {height}].");

            if (!Enum.IsDefined(typeof(TypeCode), type))
                throw new ArgumentException($"Parameter type ({type}) is not defined in {typeof(TypeCode)}.");

            if (!AllowedTypes.Contains(type))
                throw new ArgumentException($"Specified type {type} is not allowed.");

            int size;
            Width = width;
            Height = height;
            _typeCode = type;
            var tp = Type.GetType("System." + _typeCode, true, true) ?? throw new ArgumentException();
            _baseArray = Array.CreateInstance(tp, width * height);
            switch (type)
            {
                case TypeCode.UInt16:
                    size = sizeof(ushort);

                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        Set(BitConverter.ToUInt16(initialArray, (i * width + j) * size), i, j);
                    break;
                case TypeCode.Int16:
                    size = sizeof(short);

                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        Set(BitConverter.ToInt16(initialArray, (i * width + j) * size), i, j);
                    break;
                case TypeCode.UInt32:
                    size = sizeof(uint);
                    
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        Set(BitConverter.ToUInt32(initialArray, (i * width + j) * size), i, j);
                    break;
                case TypeCode.Int32:
                    size = sizeof(int);
                   
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        Set(BitConverter.ToInt32(initialArray, (i * width + j) * size), i, j);
                    break;
                case TypeCode.Single:
                    size = sizeof(float);
                   
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        Set(BitConverter.ToSingle(initialArray, (i * width + j) * size), i, j);
                    break;
                case TypeCode.Double:
                    size = sizeof(double);
                   
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        Set(BitConverter.ToDouble(initialArray, (i * width + j) * size), i, j);
                    break;
                default:
                    throw new NotSupportedException();
            }

            

        }

        public byte[] GetBytes()
        {
            //var size = System.Runtime.InteropServices.Marshal.SizeOf(this[0, 0]);

            byte[] byteArray;
            switch (_typeCode)
            {
                case TypeCode.UInt16:
                {
                    const int size = sizeof(ushort);
                    byteArray = new byte[Width * Height * size];
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<ushort>(i, j)), 0, byteArray, (i * Width + j) * size, size);
                    break;
                }
                case TypeCode.Int16:
                {
                    const int size = sizeof(short);
                    byteArray = new byte[Width * Height * size];
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<short>(i, j)), 0, byteArray, (i * Width + j) * size, size);
                    break;
                }
                case TypeCode.UInt32:
                {
                    const int size = sizeof(uint);
                    byteArray = new byte[Width * Height * size];
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<uint>(i, j)), 0, byteArray, (i * Width + j) * size, size);
                    break;
                }
                case TypeCode.Int32:
                {
                    const int size = sizeof(int);
                    byteArray = new byte[Width * Height * size];
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<int>(i, j)), 0, byteArray, (i * Width + j) * size, size);
                    break;
                }
                case TypeCode.Single:
                {
                    const int size = sizeof(float);
                    byteArray = new byte[Width * Height * size];
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<float>(i, j)), 0, byteArray, (i * Width + j) * size, size);
                    break;
                }
                case TypeCode.Double:
                {
                    const int size = sizeof(double);
                    byteArray = new byte[Width * Height * size];
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        Array.Copy(BitConverter.GetBytes(Get<double>(i, j)), 0, byteArray, (i * Width + j) * size, size);
                    break;
                }
                default:
                    throw new NotSupportedException();
            }


            return byteArray;
        }

        public object Max()
        {
            object max;

            switch (_typeCode)
            {
                case TypeCode.UInt16:
                {
                    var localMax = ushort.MinValue;
                    ushort localVal;
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        if ((localVal = Get<ushort>(i, j)) > localMax)
                            localMax = localVal;

                    max = localMax;
                    break;
                }
                case TypeCode.Int16:
                {
                    var localMax = short.MinValue;
                    short localVal;
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        if ((localVal = Get<short>(i, j)) > localMax)
                            localMax = localVal;

                    max = localMax;
                    break;
                }
                case TypeCode.UInt32:
                {
                    var localMax = uint.MinValue;
                    uint localVal;
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        if ((localVal = Get<uint>(i, j)) > localMax)
                            localMax = localVal;

                    max = localMax;
                    break;
                }
                case TypeCode.Int32:
                {
                    var localMax = int.MinValue;
                    int localVal;
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        if ((localVal = Get<int>(i, j)) > localMax)
                            localMax = localVal;

                    max = localMax;
                    break;
                }
                case TypeCode.Single:
                {
                    var localMax = float.MinValue;
                    float localVal;
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        if ((localVal = Get<float>(i, j)) > localMax)
                            localMax = localVal;

                    max = localMax;
                    break;
                }
                case TypeCode.Double:
                {
                    var localMax = double.MinValue;
                    double localVal;
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        if ((localVal = Get<double>(i, j)) > localMax)
                            localMax = localVal;

                    max = localMax;
                    break;
                }
                default:
                    throw new NotSupportedException();
            }



            return max;
        }

        public object Min()
        {
            object min;

            switch (_typeCode)
            {
                case TypeCode.UInt16:
                    {
                        var localMin = ushort.MinValue;
                        ushort localVal;
                        for (var i = 0; i < Height; i++)
                            for (var j = 0; j < Width; j++)
                                if ((localVal = Get<ushort>(i, j)) < localMin)
                                    localMin = localVal;

                        min = localMin;
                        break;
                    }
                case TypeCode.Int16:
                    {
                        var localMin = short.MinValue;
                        short localVal;
                        for (var i = 0; i < Height; i++)
                            for (var j = 0; j < Width; j++)
                                if ((localVal = Get<short>(i, j)) < localMin)
                                    localMin = localVal;

                        min = localMin;
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        var localMin = uint.MinValue;
                        uint localVal;
                        for (var i = 0; i < Height; i++)
                            for (var j = 0; j < Width; j++)
                                if ((localVal = Get<uint>(i, j)) < localMin)
                                    localMin = localVal;

                        min = localMin;
                        break;
                    }
                case TypeCode.Int32:
                    {
                        var localMin = int.MinValue;
                        int localVal;
                        for (var i = 0; i < Height; i++)
                            for (var j = 0; j < Width; j++)
                                if ((localVal = Get<int>(i, j)) < localMin)
                                    localMin = localVal;

                        min = localMin;
                        break;
                    }
                case TypeCode.Single:
                    {
                        var localMin = float.MinValue;
                        float localVal;
                        for (var i = 0; i < Height; i++)
                            for (var j = 0; j < Width; j++)
                                if ((localVal = Get<float>(i, j)) < localMin)
                                    localMin = localVal;

                        min = localMin;
                        break;
                    }
                case TypeCode.Double:
                    {
                        var localMin = double.MinValue;
                        double localVal;
                        for (var i = 0; i < Height; i++)
                            for (var j = 0; j < Width; j++)
                                if ((localVal = Get<double>(i, j)) < localMin)
                                    localMin = localVal;

                        min = localMin;
                        break;
                    }
                default:
                    throw new NotSupportedException();
            }



            return min;
        }

        public Image Copy()
            => new Image(_baseArray, Width, Height);

        public void CopyTo(Image im)
        {
            if (im._typeCode != _typeCode || im._baseArray.Length != _baseArray.Length)
                im._baseArray = Array.CreateInstance(
                    Type.GetType("System." + _typeCode, true, true) ?? throw new ArgumentException(),
                    _baseArray.Length);

            Array.Copy(_baseArray, im._baseArray, _baseArray.Length);

            im.Width = Width;
            im.Height = Height;
            im._typeCode = _typeCode;
        }

        public void Clamp(double low, double high)
        {
            switch (_typeCode)
            {
                case TypeCode.UInt16:
                {
                    var locLow = (ushort)(Math.Floor(low));
                    var locHigh = (ushort)(Math.Ceiling(high));
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        if (Get<ushort>(i, j) < locLow)
                            Set(locLow, i, j);
                        else if (Get<ushort>(i, j) > locHigh)
                            Set(locHigh, i, j);
                    break;
                }
                case TypeCode.Int16:
                {
                    var locLow = (short)(Math.Floor(low));
                    var locHigh = (short)(Math.Ceiling(high));
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        if (Get<short>(i, j) < locLow)
                            Set(locLow, i, j);
                        else if (Get<short>(i, j) > locHigh)
                            Set(locHigh, i, j);
                    break;
                }
                case TypeCode.UInt32:
                {
                    var locLow = (uint)(Math.Floor(low));
                    var locHigh = (uint)(Math.Ceiling(high));
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        if (Get<uint>(i, j) < locLow)
                            Set(locLow, i, j);
                        else if (Get<uint>(i, j) > locHigh)
                            Set(locHigh, i, j);
                    break;
                }
                case TypeCode.Int32:
                {
                    var locLow = (int)(Math.Floor(low));
                    var locHigh = (int)(Math.Ceiling(high));
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        if (Get<int>(i, j) < locLow)
                            Set(locLow, i, j);
                        else if (Get<int>(i, j) > locHigh)
                            Set(locHigh, i, j);
                    break;
                }
                case TypeCode.Single:
                {
                    var locLow = (float)(low);
                    var locHigh = (float)(high);
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        if (Get<float>(i, j) < locLow)
                            Set(locLow, i, j);
                        else if (Get<float>(i, j) > locHigh)
                            Set(locHigh, i, j);
                    break;
                }
                case TypeCode.Double:
                {
                    var locLow = low;
                    var locHigh = high;
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        if (Get<double>(i, j) < locLow)
                            Set(locLow, i, j);
                        else if (Get<double>(i, j) > locHigh)
                            Set(locHigh, i, j);
                    break;
                }
                default:
                    throw new NotSupportedException();
            }
        }

        public void Scale(double gMin, double gMax)
        {

            var min = Min();
            var max = Max();

            switch (_typeCode)
            {
                case TypeCode.UInt16:
                {
                    var globMin = Convert.ToUInt16(gMin);
                    var globMax = Convert.ToUInt16(gMax);
                    var locMax = (ushort)max;
                    var locMin = (ushort)min;

                    void Worker(int k)
                    {
                        for (var j = 0; j < Width; j++) Set((ushort) Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (Get<ushort>(k, j) - locMin)), k, j);
                    }

                    if (_isParallelEnabled && Width * Height > MaxImageSingleThreadSize)
                        Parallel.For(0, Height, Worker);
                    else
                        for (var i = 0; i < Height; i++)
                            Worker(i);


                    break;
                }
                case TypeCode.Int16:
                {
                    var globMin = Convert.ToInt16(gMin);
                    var globMax = Convert.ToInt16(gMax);
                    var locMax = (short)max;
                    var locMin = (short)min;

                    void Worker(int k)
                    {
                        for (var j = 0; j < Width; j++) Set((short) Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (Get<short>(k, j) - locMin)), k, j);
                    }

                    if (_isParallelEnabled && Width * Height > MaxImageSingleThreadSize)
                        Parallel.For(0, Height, Worker);
                    else
                        for (var i = 0; i < Height; i++)
                            Worker(i);
                    break;
                }
                case TypeCode.UInt32:
                {
                    var globMin = Convert.ToUInt32(gMin);
                    var globMax = Convert.ToUInt32(gMax);
                    var locMax = (uint)max;
                    var locMin = (uint)min;

                        void Worker(int k)
                        {
                            for (var j = 0; j < Width; j++)
                                Set((uint)Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (Get<uint>(k, j) - locMin)), k, j);
                        }

                        if (_isParallelEnabled && Width * Height > MaxImageSingleThreadSize)
                        Parallel.For(0, Height, Worker);
                    else
                        for (var i = 0; i < Height; i++)
                            Worker(i);
                    break;
                }
                case TypeCode.Int32:
                {
                    var globMin = Convert.ToInt32(gMin);
                    var globMax = Convert.ToInt32(gMax);
                    var locMax = (int)max;
                    var locMin = (int)min;

                        void Worker(int k)
                        {
                            for (var j = 0; j < Width; j++)
                                Set((int)Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (Get<int>(k, j) - locMin)), k, j);
                        }

                        if (_isParallelEnabled && Width * Height > MaxImageSingleThreadSize)
                        Parallel.For(0, Height, Worker);
                    else
                        for (var i = 0; i < Height; i++)
                            Worker(i);
                    break;
                }
                case TypeCode.Single:
                {
                    var globMin = Convert.ToSingle(gMin);
                    var globMax = Convert.ToSingle(gMax);
                    var locMax = (float)max;
                    var locMin = (float)min;

                        void Worker(int k)
                        {
                            for (var j = 0; j < Width; j++)
                                Set((float)Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (Get<float>(k, j) - locMin)), k, j);
                        }

                        if (_isParallelEnabled && Width * Height > MaxImageSingleThreadSize)
                        Parallel.For(0, Height, Worker);
                    else
                        for (var i = 0; i < Height; i++)
                            Worker(i);
                    break;
                }
                case TypeCode.Double:
                {
                    var globMin = Convert.ToDouble(gMin);
                    var globMax = Convert.ToDouble(gMax);
                    var locMax = (double)max;
                    var locMin = (double)min;

                        void Worker(int k)
                        {
                            for (var j = 0; j < Width; j++)
                                Set(Math.Floor(globMin + 1.0 * (globMax - globMin) / (locMax - locMin) * (Get<double>(k, j) - locMin)), k, j);
                        }

                        if (_isParallelEnabled && Width * Height > MaxImageSingleThreadSize)
                        Parallel.For(0, Height, Worker);
                    else
                        for (var i = 0; i < Height; i++)
                            Worker(i);
                    break;
                }
                default:
                    throw new NotSupportedException();
            }
        }

        public double Percentile(double lvl)
        {
            if (lvl < 0 | lvl > 1.0)
                throw new ArgumentOutOfRangeException($"{nameof(lvl)} parameter is out of range ({lvl} should be in [0, 1]).");

            switch (_typeCode)
            {
                case TypeCode.UInt16:
                {
                    if (Math.Abs(lvl) < double.Epsilon)
                        return (ushort)Min();
                    if (Math.Abs(lvl - 1) < double.Epsilon)
                        return (ushort)Max();
                    var query = ((ushort[])_baseArray).OrderBy(x => x);

                    var length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();

                }
                case TypeCode.Int16:
                {
                    if (Math.Abs(lvl) < double.Epsilon)
                        return (short)Min();
                    if (Math.Abs(lvl - 1) < double.Epsilon)
                        return (short)Max();
                    var query = ((short[])_baseArray).OrderBy(x => x);

                    var length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();

                }
                case TypeCode.UInt32:
                {
                    if (Math.Abs(lvl) < double.Epsilon)
                        return (uint)Min();
                    if (Math.Abs(lvl - 1) < double.Epsilon)
                        return (uint)Max();
                    var query = ((uint[])_baseArray).OrderBy(x => x);

                    var length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();

                }
                case TypeCode.Int32:
                {
                    if (Math.Abs(lvl) < double.Epsilon)
                        return (int)Min();
                    if (Math.Abs(lvl - 1) < double.Epsilon)
                        return (int)Max();
                    var query = ((int[])_baseArray).OrderBy(x => x);

                    var length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();

                }
                case TypeCode.Single:
                {
                    if (Math.Abs(lvl) < double.Epsilon)
                        return (float)Min();
                    if (Math.Abs(lvl - 1) < double.Epsilon)
                        return (float)Max();
                    var query = ((float[])_baseArray).OrderBy(x => x);

                    var length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();

                }
                case TypeCode.Double:
                {
                    if (Math.Abs(lvl) < double.Epsilon)
                        return (double)Min();
                    if (Math.Abs(lvl - 1) < double.Epsilon)
                        return (double)Max();
                    var query = ((double[])_baseArray).OrderBy(x => x);

                    var length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();

                }
            }

            throw new NotSupportedException();
        }

        public void AddScalar(double value)
        {
            switch (UnderlyingType)
            {
                case TypeCode.UInt16:
                {
                    var data = (ushort[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToUInt16(data[i] + value);
                    break;
                }
                case TypeCode.Int16:
                {
                    var data = (short[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToInt16(data[i] + value);
                    break;
                }
                case TypeCode.UInt32:
                {
                    var data = (uint[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToUInt32(data[i] + value);
                    break;
                }
                case TypeCode.Int32:
                {
                    var data = (int[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToInt32(data[i] + value);
                    break;
                }
                case TypeCode.Single:
                {
                    var data = (float[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToSingle(data[i] + value);
                    break;
                }
                case TypeCode.Double:
                {
                    var data = (double[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToDouble(data[i] + value);
                    break;
                }
                default:
                    throw new NotSupportedException();
            }
        }

        public void MultiplyByScalar(double value)
        {
            switch (UnderlyingType)
            {
                case TypeCode.UInt16:
                {
                    var data = (ushort[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToUInt16(data[i] * value);
                    break;
                }
                case TypeCode.Int16:
                {
                    var data = (short[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToInt16(data[i] * value);
                    break;
                }
                case TypeCode.UInt32:
                {
                    var data = (uint[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToUInt32(data[i] * value);
                    break;
                }
                case TypeCode.Int32:
                {
                    var data = (int[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToInt32(data[i] * value);
                    break;
                }
                case TypeCode.Single:
                {
                    var data = (float[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToSingle(data[i] * value);
                    break;
                }
                case TypeCode.Double:
                {
                    var data = (double[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToDouble(data[i] * value);
                    break;
                }
                default:
                    throw new NotSupportedException();
            }
        }

        public Image ReduceToDisplay(double min, double max)
        {

            ushort Convrtr(double x) => Convert.ToUInt16(1.0 * (x - min) * ushort.MaxValue / (max - min));
            switch (UnderlyingType)
            {
                case TypeCode.Int16:
                    return new Image(((short[])_baseArray)
                        .AsParallel()
                        .Select(x => Convrtr(x))
                        .ToArray(), Width, Height);
                case TypeCode.UInt16:
                    return Copy();
                case TypeCode.Int32:
                    return new Image(((int[])_baseArray)
                        .AsParallel()
                        .Select(x => Convrtr(x))
                        .ToArray(), Width, Height);
                case TypeCode.UInt32:
                    return new Image(((uint[])_baseArray)
                        .AsParallel()
                        .Select(x => Convrtr(x))
                        .ToArray(), Width, Height);
                case TypeCode.Single:
                    return new Image(((float[])_baseArray)
                        .AsParallel()
                        .Select(x => Convrtr(x))
                        .ToArray(), Width, Height);
                case TypeCode.Double:
                    return new Image(((double[])_baseArray)
                        .AsParallel()
                        .Select(Convrtr)
                        .ToArray(), Width, Height);
                default:
                    throw new NotSupportedException();
            }


        }

        public Image CastTo<TS, TD>(Func<TS, TD> cast)
           =>  (typeof(TS) == Type.GetType("System." + UnderlyingType))                       
            ? new Image(((TS[])_baseArray)
                .AsParallel()
                .Select(cast)
                .ToArray(),
                Width, Height)
            : throw new TypeAccessException($"Source type {typeof(TS)} differs from underlying type with code {UnderlyingType}.");

        public Image Transpose()
        {
            var type = Type.GetType("System." + _typeCode, true, true)
                       ?? throw new NotSupportedException();

            var newArray = Array.CreateInstance(type, Width * Height);

            switch (_typeCode)
            {
                case TypeCode.Int16:
                {
                    var castArray = (short[]) newArray;

                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        castArray[j * Height + i] = Get<short>(i, j);
                    break;
                }

                case TypeCode.UInt16:
                {
                    var castArray = (ushort[]) newArray;
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        castArray[j * Height + i] = Get<ushort>(i, j);
                    break;
                }
                case TypeCode.Int32:
                {
                    var castArray = (int[]) newArray;
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        castArray[j * Height + i] = Get<int>(i, j);
                    break;
                }
                case TypeCode.UInt32:
                {
                    var castArray = (uint[])newArray;
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        castArray[j * Height + i] = Get<uint>(i, j);
                    break;
                }
                case TypeCode.Single:
                {
                    var castArray = (float[])newArray;
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        castArray[j * Height + i] = Get<float>(i, j);
                    break;
                }
                case TypeCode.Double:
                {
                    var castArray = (double[])newArray;
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        castArray[j * Height + i] = Get<double>(i, j);
                    break;
                }
                default:
                    throw new NotSupportedException();

            }

            // Notice the reversed order of Height Width
            return new Image(newArray, Height, Width);
        }
    }
}
