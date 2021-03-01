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
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Policy;
using MathNet.Numerics;

namespace DipolImage
{
    [DebuggerDisplay(@"\{Image ({Height} x {Width}) of type {UnderlyingType}\}")]
    [DataContract]
    public class Image : IEqualityComparer<Image>, IEquatable<Image>
    {
        // One row of standard i32 image is 4 * 512 = 2048 bytes
        private const int StackAllocByteLimit = 2048;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly TypeCode[] AllowedTypes =
         {
            TypeCode.Double,
            TypeCode.Single,
            TypeCode.UInt16,
            TypeCode.Int16,
            TypeCode.UInt32,
            TypeCode.Int32,
            TypeCode.Byte
        };

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Dictionary<TypeCode, int> TypeSizes = new Dictionary<TypeCode, int>
        {
            {TypeCode.Double, sizeof(double)},
            {TypeCode.Single, sizeof(float)},
            {TypeCode.UInt16, sizeof(ushort)},
            {TypeCode.Int16, sizeof(short)},
            {TypeCode.UInt32, sizeof(uint)},
            {TypeCode.Int32, sizeof(int)},
            {TypeCode.Byte, sizeof(byte)}
        };

        [DataMember]
        private readonly Array _baseArray;

        public static IReadOnlyCollection<TypeCode> AllowedPixelTypes
            => Array.AsReadOnly(AllowedTypes);

        [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [field: DataMember]
        public TypeCode UnderlyingType { get; }

        [field: DataMember]
        public int Width
        {
            get;
        }
        [field: DataMember]
        public int Height
        {
            get;
        }

        public Type Type => Type.GetType("System." + UnderlyingType);

        public object this[int i, int j]
        {
            get => _baseArray.GetValue(i * Width + j);
            set => _baseArray.SetValue(value, i * Width + j);
        }

        public T Get<T>(int i, int j) where T : unmanaged
            => ((T[])_baseArray)[i * Width + j];

        public void Set<T>(T value, int i, int j) where T : unmanaged
            => ((T[])_baseArray)[i * Width + j] = value;

        public Image(Array initialArray, int width, int height, bool copy = true)
        {
            if (initialArray == null)
                throw new ArgumentNullException("Argument is null: " + nameof(initialArray));

            if (width < 1 || height < 1)
                throw new ArgumentOutOfRangeException($"Image size is incorrect [{width}, {height}].");

            var val = initialArray.GetValue(0);

            if (!AllowedTypes.Contains(Type.GetTypeCode(val.GetType())))
                throw new ArgumentException($"Provided array's base type {val.GetType()} is not allowed.");

            UnderlyingType = Type.GetTypeCode(val.GetType());
            if (copy)
            {
                _baseArray = Array.CreateInstance(val.GetType(), width * height);
                Buffer.BlockCopy(initialArray, 0, _baseArray, 0, width * height * Marshal.SizeOf(val));
            }
            else _baseArray = initialArray;

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

            Width = width;
            Height = height;
            UnderlyingType = type;
            var tp = Type.GetType("System." + UnderlyingType, true, true);
            var size = Marshal.SizeOf(tp);
            _baseArray = Array.CreateInstance(tp, width * height);

            Buffer.BlockCopy(initialArray, 0, _baseArray, 0,
                Math.Min(initialArray.Length, width * height * size));
        }

        public byte[] GetBytes()
        {
            var size = Marshal.SizeOf(_baseArray.GetValue(0));
            var byteArray = new byte[Width * Height * size];

            Buffer.BlockCopy(_baseArray, 0, byteArray, 0, byteArray.Length);

            return byteArray;
        }

        public double Max()
        {
            double max;

            switch (UnderlyingType)
            {
                case TypeCode.Byte:
                {
                    var localMax = byte.MinValue;
                    var arr = (byte[]) _baseArray;
                    foreach (var item in arr)
                        if (item >= localMax)
                            localMax = item;

                    max = localMax;
                        break;
                }
                case TypeCode.UInt16:
                {
                    var localMax = ushort.MinValue;
                    var arr = (ushort[])_baseArray;
                    foreach (var item in arr)
                        if (item >= localMax)
                            localMax = item;

                        max = localMax;
                    break;
                }
                case TypeCode.Int16:
                {
                    var localMax = short.MinValue;
                    var arr = (short[])_baseArray;
                    foreach (var item in arr)
                        if (item >= localMax)
                            localMax = item;

                        max = localMax;
                    break;
                }
                case TypeCode.UInt32:
                {
                    var localMax = uint.MinValue;
                    var arr = (uint[])_baseArray;
                    foreach (var item in arr)
                        if (item >= localMax)
                            localMax = item;

                        max = localMax;
                    break;
                }
                case TypeCode.Int32:
                {
                    var localMax = int.MinValue;
                    var arr = (int[])_baseArray;
                    foreach (var item in arr)
                        if (item >= localMax)
                            localMax = item;

                        max = localMax;
                    break;
                }
                case TypeCode.Single:
                {
                    var localMax = float.MinValue;
                    var arr = (float[])_baseArray;
                    foreach (var item in arr)
                        if (item >= localMax)
                            localMax = item;

                        max = localMax;
                    break;
                }
                default:
                {
                    var localMax = double.MinValue;
                    var arr = (double[])_baseArray;
                    foreach (var item in arr)
                        if (item >= localMax)
                            localMax = item;

                    max = localMax;
                    break;
                }
              
            }

            return max;
        }

        public double Min()
        {
            double min;

            switch (UnderlyingType)
            {
                case TypeCode.Byte:
                {
                    var localMin = byte.MaxValue;
                    var arr = (byte[])_baseArray;
                    foreach (var item in arr)
                        if (item <= localMin)
                            localMin = item;

                        min = localMin;
                    break;
                }
                case TypeCode.UInt16:
                    {
                        var localMin = ushort.MaxValue;
                        var arr = (ushort[])_baseArray;
                        foreach (var item in arr)
                            if (item <= localMin)
                                localMin = item;

                        min = localMin;
                        break;
                    }
                case TypeCode.Int16:
                    {
                        var localMin = short.MaxValue;
                        var arr = (short[])_baseArray;
                        foreach (var item in arr)
                            if (item <= localMin)
                                localMin = item;

                        min = localMin;
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        var localMin = uint.MaxValue;
                        var arr = (uint[])_baseArray;
                        foreach (var item in arr)
                            if (item <= localMin)
                                localMin = item;

                        min = localMin;
                        break;
                    }
                case TypeCode.Int32:
                    {
                        var localMin = int.MaxValue;
                        var arr = (int[])_baseArray;
                        foreach (var item in arr)
                            if (item <= localMin)
                                localMin = item;

                        min = localMin;
                        break;
                    }
                case TypeCode.Single:
                    {
                        var localMin = float.MaxValue;
                        var arr = (float[])_baseArray;
                        foreach (var item in arr)
                            if (item <= localMin)
                                localMin = item;

                        min = localMin;
                        break;
                    }
                default:
                    {
                        var localMin = double.MaxValue;
                        var arr = (double[]) _baseArray;
                        foreach(var item in arr)
                            if (item <= localMin)
                                localMin = item;

                        min = localMin;
                        break;
                    }
            }



            return min;
        }

        public Image Copy()
            => new Image(_baseArray, Width, Height);

        public void Clamp(double low, double high)
        {
            if (high <= low)
                throw new ArgumentException(@"[high] should be greater than [low]");

            switch (UnderlyingType)
            {
                case TypeCode.Byte:
                {
                    var locLow = (byte) (Math.Floor(low));
                    var locHigh = (byte) (Math.Ceiling(high));
                    var arr = (byte[]) _baseArray;

                    for (var i = 0; i < arr.Length; i++)
                        if (arr[i] < locLow)
                            arr[i] = locLow;
                        else if (arr[i] > locHigh)
                            arr[i] = locHigh;
                    break;
                }

                case TypeCode.UInt16:
                {
                    var locLow = (ushort) (Math.Floor(low));
                    var locHigh = (ushort) (Math.Ceiling(high));
                    var arr = (ushort[]) _baseArray;

                    for (var i = 0; i < arr.Length; i++)
                        if (arr[i] < locLow)
                            arr[i] = locLow;
                        else if (arr[i] > locHigh)
                            arr[i] = locHigh;
                    break;
                }
                case TypeCode.Int16:
                {
                    var locLow = (short) (Math.Floor(low));
                    var locHigh = (short) (Math.Ceiling(high));
                    var arr = (short[]) _baseArray;

                    for (var i = 0; i < arr.Length; i++)
                        if (arr[i] < locLow)
                            arr[i] = locLow;
                        else if (arr[i] > locHigh)
                            arr[i] = locHigh;
                    break;
                }
                case TypeCode.UInt32:
                {
                    var locLow = (uint) (Math.Floor(low));
                    var locHigh = (uint) (Math.Ceiling(high));
                    var arr = (uint[]) _baseArray;

                    for (var i = 0; i < arr.Length; i++)
                        if (arr[i] < locLow)
                            arr[i] = locLow;
                        else if (arr[i] > locHigh)
                            arr[i] = locHigh;
                    break;
                }
                case TypeCode.Int32:
                {
                    var locLow = (int) (Math.Floor(low));
                    var locHigh = (int) (Math.Ceiling(high));
                    var arr = (int[]) _baseArray;

                    for (var i = 0; i < arr.Length; i++)
                        if (arr[i] < locLow)
                            arr[i] = locLow;
                        else if (arr[i] > locHigh)
                            arr[i] = locHigh;
                    break;
                }
                case TypeCode.Single:
                {
                    var locLow = (float) (low);
                    var locHigh = (float) (high);
                    var arr = (float[]) _baseArray;

                    for (var i = 0; i < arr.Length; i++)
                        if (arr[i] < locLow)
                            arr[i] = locLow;
                        else if (arr[i] > locHigh)
                            arr[i] = locHigh;
                    break;
                }
                default:
                {
                    var arr = (double[]) _baseArray;

                    for (var i = 0; i < arr.Length; i++)
                        if (arr[i] < low)
                            arr[i] = low;
                        else if (arr[i] > high)
                            arr[i] = high;
                    break;
                }

            }
        }

        public void Scale(double gMin, double gMax)
        {
            if (gMax <= gMin)
                throw new ArgumentException(@"[high] should be greater than [low]");

            var min = Min();
            var max = Max();

            var factor = 1.0 * (gMax - gMin) / (max - min);


            switch (UnderlyingType)
            {
                case TypeCode.Byte:
                {
                    var arr = (byte[]) _baseArray;

                    if (min.AlmostEqual(max))
                    {
                        var val = (byte) (0.5 * (gMin + gMax));
                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = val;
                    }
                    else

                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = (byte) (gMin + factor * (arr[i] - min));

                    break;
                }
                case TypeCode.UInt16:
                {
                    var arr = (ushort[]) _baseArray;

                    if (min.AlmostEqual(max))
                    {
                        var val = (ushort) (0.5 * (gMin + gMax));
                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = val;
                    }
                    else

                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = (ushort) (gMin + factor * (arr[i] - min));

                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = (ushort)(gMin + factor * (arr[i] - min));

                        break;
                }
                case TypeCode.Int16:
                {
                    var arr = (short[]) _baseArray;

                    if (min.AlmostEqual(max))
                    {
                        var val = (short) (0.5 * (gMin + gMax));
                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = val;
                    }
                    else

                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = (short) (gMin + factor * (arr[i] - min));

                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = (short)(gMin + factor * (arr[i] - min));

                        break;
                }
                case TypeCode.UInt32:
                {
                    var arr = (uint[]) _baseArray;

                    if (min.AlmostEqual(max))
                    {
                        var val = (uint) (0.5 * (gMin + gMax));
                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = val;
                    }
                    else

                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = (uint) (gMin + factor * (arr[i] - min));

                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = (uint)(gMin + factor * (arr[i] - min));
                        break;
                }
                case TypeCode.Int32:
                {
                    var arr = (int[]) _baseArray;

                    if (min.AlmostEqual(max))
                    {
                        var val = (int) (0.5 * (gMin + gMax));
                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = val;
                    }
                    else

                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = (int) (gMin + factor * (arr[i] - min));

                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = (int)(gMin + factor * (arr[i] - min));

                        break;
                }
                case TypeCode.Single:
                {
                    var arr = (float[]) _baseArray;

                    if (min.AlmostEqual(max))
                    {
                        var val = (float) (0.5 * (gMin + gMax));
                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = val;
                    }
                    else

                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = (float) (gMin + factor * (arr[i] - min));

                    break;
                }
                default:
                {
                    var arr = (double[]) _baseArray;

                    if (min.AlmostEqual(max))
                    {
                        var val = 0.5 * (gMin + gMax);
                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = val;
                    }

                    else
                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = gMin + factor * (arr[i] - min);

                    break;
                }
            }
        }

        public double Percentile(double lvl)
        {
            if (lvl < 0 | lvl > 1.0)
                throw new ArgumentOutOfRangeException($"{nameof(lvl)} parameter is out of range ({lvl} should be in [0, 1]).");

            switch (UnderlyingType)
            {
                case TypeCode.Byte:
                {
                    if (Math.Abs(lvl) < double.Epsilon)
                        return (byte)Min();
                    if (Math.Abs(lvl - 1) < double.Epsilon)
                        return (byte)Max();
                    var query = ((byte[])_baseArray).OrderBy(x => x);

                    var length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();

                }
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
                default:
                {
                    if (Math.Abs(lvl) < double.Epsilon)
                        return Min();
                    if (Math.Abs(lvl - 1) < double.Epsilon)
                        return Max();
                    var query = ((double[])_baseArray).OrderBy(x => x);

                    var length = (int)Math.Ceiling(lvl * Width * Height);


                    return query.Skip(length - 1).Take(1).First();

                }
            }

        }

        public void AddScalar(double value)
        {
            switch (UnderlyingType)
            {

                case TypeCode.Byte:
                {
                    var data = (byte[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = (byte)(data[i] + value);
                    break;
                }
                case TypeCode.UInt16:
                {
                    var data = (ushort[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = (ushort)(data[i] + value);
                    break;
                }
                case TypeCode.Int16:
                {
                    var data = (short[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = (short)(data[i] + value);
                    break;
                }
                case TypeCode.UInt32:
                {
                    var data = (uint[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = (uint)(data[i] + value);
                    break;
                }
                case TypeCode.Int32:
                {
                    var data = (int[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = (int)(data[i] + value);
                    break;
                }
                case TypeCode.Single:
                {
                    var data = (float[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = (float)(data[i] + value);
                    break;
                }
                default:
                {
                    var data = (double[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = data[i] + value;
                    break;
                }
               
            }
        }

        public void MultiplyByScalar(double value)
        {
            switch (UnderlyingType)
            {
                case TypeCode.Byte:
                {
                    var data = (byte[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToByte(data[i] * value);
                    break;
                }
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
                default:
                {
                    var data = (double[])_baseArray;
                    for (var i = 0; i < data.Length; i++)
                        data[i] = Convert.ToDouble(data[i] * value);
                    break;
                }
                
            }
        }

        public Image CastTo<TS, TD>(Func<TS, TD> cast)
            where TS : unmanaged
            where TD : unmanaged
            => (typeof(TS) == Type)
                ? new Image(((TS[]) _baseArray)
                    .AsParallel()
                    .Select(cast)
                    .ToArray(),
                    Width, Height)
                : throw new TypeAccessException(
                    $"Source type {typeof(TS)} differs from underlying type with code {UnderlyingType}.");

        public Image Transpose()
        {
            var type = Type.GetType("System." + UnderlyingType, true, true);

            var newArray = Array.CreateInstance(type, Width * Height);

            switch (UnderlyingType)
            {

                case TypeCode.Byte:
                {
                    var castArray = (byte[])newArray;

                    for (var i = 0; i < Height; i++)
                        for (var j = 0; j < Width; j++)
                            castArray[j * Height + i] = Get<byte>(i, j);
                    break;
                }
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
                default:
                {
                    var castArray = (double[])newArray;
                    for (var i = 0; i < Height; i++)
                    for (var j = 0; j < Width; j++)
                        castArray[j * Height + i] = Get<double>(i, j);
                    break;
                }
                
            }

            // Notice the reversed order of Height Width
            return new Image(newArray, Height, Width);
        }

        public Image Reflect(ReflectionDirection direction)
        {
            var size = TypeSizes[UnderlyingType];
            var width = Width;
            var height = Height;
            // Fast path for row swapping
            if (direction == ReflectionDirection.Vertical)
            {
                byte[] arrayBuff = null;
                try
                {
                    var buffer = width * size <= StackAllocByteLimit
                        ? stackalloc byte[width * size]
                        : (arrayBuff = ArrayPool<byte>.Shared.Rent(StackAllocByteLimit)).AsSpan(0, width * size);
                    
                    for (var rowId = 0; rowId < height / 2; rowId++)
                    {
                        // Swap lines, efficiently
                    }

                    
                }
                finally
                {
                    if (arrayBuff is {})
                    {
                        ArrayPool<byte>.Shared.Return(arrayBuff);
                    }
                }


            }
            throw new NotImplementedException();
        }

        public Image Rotate(RotateBy rotateBy, RotationDirection direction)
        {
            throw new NotImplementedException();
        }

        public bool Equals(Image x, Image y)
            => x?.Equals(y) ?? false;

        public int GetHashCode(Image obj)
            =>  obj.GetHashCode();

        public bool Equals(Image other)
        {
            if (UnderlyingType != other?.UnderlyingType ||
                Width != other.Width ||
                Height != other.Height)
                return false;

            switch (UnderlyingType)
            {

                case TypeCode.Byte:
                {
                    var thisArr = (byte[])_baseArray;
                    var otherArr = (byte[])other._baseArray;
                    for (var i = 0; i < Width * Height; i++)
                        if (thisArr[i] != otherArr[i])
                            return false;
                    return true;
                }
                case TypeCode.Int16:
                {
                    var thisArr = (short[]) _baseArray;
                    var otherArr = (short[]) other._baseArray;
                    for (var i = 0; i < Width * Height; i++)
                        if (thisArr[i] != otherArr[i])
                            return false;
                    return true;
                }
                case TypeCode.UInt16:
                {
                    var thisArr = (ushort[])_baseArray;
                    var otherArr = (ushort[])other._baseArray;
                    for (var i = 0; i < Width * Height; i++)
                        if (thisArr[i] != otherArr[i])
                            return false;
                    return true;
                }
                case TypeCode.Int32:
                {
                    var thisArr = (int[])_baseArray;
                    var otherArr = (int[])other._baseArray;
                    for (var i = 0; i < Width * Height; i++)
                        if (thisArr[i] != otherArr[i])
                            return false;
                    return true;
                }
                case TypeCode.UInt32:
                {
                    var thisArr = (uint[])_baseArray;
                    var otherArr = (uint[])other._baseArray;
                    for (var i = 0; i < Width * Height; i++)
                        if (thisArr[i] != otherArr[i])
                            return false;
                    return true;
                }
                case TypeCode.Single:
                {
                    var thisArr = (float[])_baseArray;
                    var otherArr = (float[])other._baseArray;
                    for (var i = 0; i < Width * Height; i++)
                        if (!thisArr[i].AlmostEqual(otherArr[i]))
                            return false;
                        return true;
                }
                default:
                {
                    var thisArr = (double[])_baseArray;
                    var otherArr = (double[])other._baseArray;
                    for (var i = 0; i < Width * Height; i++)
                        if(!thisArr[i].AlmostEqual(otherArr[i]))
                            return false;
                    return true;
                }
              
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Image im && im.Equals(this);
        }

        public override int GetHashCode()
        {
            switch (UnderlyingType)
            {
                case TypeCode.Byte:
                    return ((byte[])_baseArray).Aggregate(0, (sum, pix) => sum ^ (7 * pix % int.MaxValue));
                case TypeCode.Int16:
                    return ((short[]) _baseArray).Aggregate(0, (sum, pix) => sum ^ (7 * pix % int.MaxValue));
                case TypeCode.UInt16:
                    return ((ushort[])_baseArray).Aggregate(0, (sum, pix) => sum + (7 * pix % int.MaxValue));
                case TypeCode.Int32:
                    return ((int[])_baseArray).Aggregate(0, (sum, pix) => sum ^ (7 * pix % int.MaxValue));
                case TypeCode.UInt32:
                    return ((uint[])_baseArray).Aggregate(0, (sum, pix) => sum ^ (int)(7 * pix % int.MaxValue));
                case TypeCode.Single:
                    return ((float[]) _baseArray).Aggregate(0, (sum, pix) => sum ^ pix.GetHashCode());
                default:
                    return ((double[])_baseArray).Aggregate(0, (sum, pix) => sum ^ pix.GetHashCode());
            }
        }

    }
}
