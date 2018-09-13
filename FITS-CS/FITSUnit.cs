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
using System.Linq;

namespace FITS_CS
{
    public class FitsUnit
    {
        public static readonly int UnitSizeInBytes = 2880;

        public byte[] Data { get; } = new byte[UnitSizeInBytes];

        public FitsUnit(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException($"{nameof(data)} is null");
            if (data.Length != UnitSizeInBytes)
                throw new ArgumentException($"{nameof(data)} has wrong length");

            Array.Copy(data, Data, data.Length);
        }

        public bool IsKeywords
            => Enumerable.Range(0, UnitSizeInBytes / FITSKey.KeySize)
            .Select(i => FITSKey.IsFitsKey(Data, i * FITSKey.KeySize))
            .Aggregate(true, (old, nv) => old & nv);

        public bool IsData
            => Enumerable.Range(0, UnitSizeInBytes / FITSKey.KeySize)
            .Select(i => FITSKey.IsFitsKey(Data, i * FITSKey.KeySize))
            .Contains(false);

        public bool TryGetKeys(out List<FITSKey> keys)
        {
            keys = null;
            if (IsData)
                return false;

            var n = UnitSizeInBytes / FITSKey.KeySize;

            keys = new List<FITSKey>(n);

            try
            {

                var currKey = new FITSKey(Data);
                var nextKey = new FITSKey(Data, FITSKey.KeySize);
                var i = 1;
                while (i < n - 1)
                {
                    //currKey = new FITSKey(array, i * FITSKey.KeySize);
                    //nextKey = i < n-1 ? new FITSKey(array, (i+1) * FITSKey.KeySize) : null;

                    if ((nextKey?.IsExtension ?? false) &&
                        currKey != null)
                    {
                        currKey.Extension = nextKey.KeyString;
                        keys.Add(currKey);
                        if (i < n - 1)
                            currKey = new FITSKey(Data, (++i) * FITSKey.KeySize);
                    }
                    else
                    {
                        keys.Add(currKey);
                        currKey = nextKey;
                    }
                    nextKey = ++i < n ? new FITSKey(Data, i * FITSKey.KeySize) : null;
                }
                if (!currKey?.IsExtension ?? false)
                    keys.Add(currKey);
                //keys = keys.Where(k => !k.IsEmpty).ToList();
                return true;
            }
            catch
            {
                keys = null;
                return false;
            }
        }

        public T[] GetData<T>() where T : struct
        {

            T[] Worker<TReturn>(FITSImageType type, Func<byte[], int, TReturn> converter)
            {
                var size = Math.Abs((short)type) / 8;
                var n = UnitSizeInBytes / size;
                var locData = new byte[size];
                var result = new T[n];
                for (var i = 0; i < n; i++)
                {
                    Array.Copy(Data, i * size, locData, 0, size);
                    Array.Reverse(locData);
                    dynamic val = converter(locData, 0);
                    result[i] = val;
                }

                return result;
            }

            if (typeof(T) == typeof(double))
                return Worker(FITSImageType.Double, BitConverter.ToDouble);
            if (typeof(T) == typeof(float))
                return Worker(FITSImageType.Single, BitConverter.ToSingle);
            if (typeof(T) == typeof(int))
                return Worker(FITSImageType.Int32, BitConverter.ToInt32);
            if (typeof(T) == typeof(short))
                return Worker(FITSImageType.Int16, BitConverter.ToInt16);
            if (typeof(T) == typeof(byte))
                return Worker(FITSImageType.UInt8, (arr, ind) => arr[0]);
            throw new NotSupportedException($"Provided type {typeof(T)} is not supported by FITS format.");
            
        }

        public static IEnumerable<T> JoinData<T>(params FitsUnit[] units) where T: struct
        {
            foreach (var unit in units)
            {
                if (!unit.IsData)
                    throw new ArgumentException($"One of the {nameof(FitsUnit)} is not data.");
                foreach (var item in unit.GetData<T>())
                    yield return item;
            }
        }

        public static IEnumerable<FitsUnit> GenerateFromKeywords(params FITSKey[] keys)
        {
            var keysPerUnit = UnitSizeInBytes / FITSKey.KeySize;

            var nUnits = (int)Math.Ceiling(1.0 * keys.Length / keysPerUnit);

            var nEmpty = nUnits * keysPerUnit - keys.Length;

            var buffer = new byte[UnitSizeInBytes];

            for (var iUnit = 0; iUnit < nUnits; iUnit++)
            {

                if (iUnit != nUnits - 1)
                {
                    for (var iKey = 0; iKey < keysPerUnit; iKey++)
                        Array.Copy(keys[iUnit * keysPerUnit + iKey].Data, 0, buffer, iKey * FITSKey.KeySize, FITSKey.KeySize);

                    yield return new FitsUnit(buffer);
                }
                else
                {
                    for (var iKey = 0; iKey < keysPerUnit - nEmpty; iKey++)
                        Array.Copy(keys[iUnit * keysPerUnit + iKey].Data, 0, buffer, iKey * FITSKey.KeySize, FITSKey.KeySize);
                    for (var iKey = keysPerUnit - nEmpty; iKey < keysPerUnit; iKey++)
                        Array.Copy(FITSKey.Empty.Data, 0, buffer, iKey * FITSKey.KeySize, FITSKey.KeySize);

                    yield return new FitsUnit(buffer);

                }
            }


        }

        public static IEnumerable<FitsUnit> GenerateFromArray(byte[] array, FITSImageType type)
        {
            var size = Math.Abs((short)type) / 8;
            var n = (int)Math.Ceiling(1.0 * array.Length / UnitSizeInBytes);
            var mappedArray = new byte[n * UnitSizeInBytes];
            Array.Copy(array, mappedArray, array.Length);

            if (BitConverter.IsLittleEndian)
                for (var i = 0; i < array.Length / size; i++)
                    Array.Reverse(mappedArray, i * size, size);
           

            var buffer = new byte[UnitSizeInBytes];

            for (var iUnit = 0; iUnit < n; iUnit++)
            {
                Array.Copy(mappedArray, iUnit * UnitSizeInBytes, buffer, 0, UnitSizeInBytes);
                yield return new FitsUnit(buffer);
            }

        }
    }
}
