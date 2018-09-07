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
    public class FITSUnit
    {
        public static readonly int UnitSizeInBytes = 2880;

        public byte[] Data { get; } = new byte[UnitSizeInBytes];

        public FITSUnit(byte[] data)
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
            T[] result;
            int n;
            int size;
            byte[] mappedArray;
            if (BitConverter.IsLittleEndian)
                mappedArray = Data.Reverse().ToArray();
            else mappedArray = Data;
            
            if (typeof(T) == typeof(double))
            {
                size = Math.Abs((short)FITSImageType.Double) / 8;
                n = UnitSizeInBytes / size;
                result = new T[n];
                for (var i = 0; i < n; i++)
                {
                    dynamic val = BitConverter.ToDouble(mappedArray, (n - i - 1) * size);
                    result[i] = val;
                }
            }
            else if (typeof(T) == typeof(short))
            {
                size = Math.Abs((short)FITSImageType.Int16) / 8;
                n = UnitSizeInBytes / size;
                result = new T[n];
                for (var i = 0; i < n; i++)
                {
                    dynamic val = BitConverter.ToInt16(mappedArray, (n - i - 1) * size);
                    result[i] = val;
                }
            }
            else throw new Exception();

            
            return result;
        }

        public static IEnumerable<T> JoinData<T>(params FITSUnit[] units) where T: struct
        {
            foreach (var unit in units)
            {
                if (!unit.IsData)
                    throw new ArgumentException($"One of the {nameof(FITSUnit)} is not data.");
                foreach (var item in unit.GetData<T>())
                    yield return item;
            }
        }

        public static IEnumerable<FITSUnit> GenerateFromKeywords(params FITSKey[] keys)
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

                    yield return new FITSUnit(buffer);
                }
                else
                {
                    for (int iKey = 0; iKey < keysPerUnit - nEmpty; iKey++)
                        Array.Copy(keys[iUnit * keysPerUnit + iKey].Data, 0, buffer, iKey * FITSKey.KeySize, FITSKey.KeySize);
                    for (int iKey = keysPerUnit - nEmpty; iKey < keysPerUnit; iKey++)
                        Array.Copy(FITSKey.Empty.Data, 0, buffer, iKey * FITSKey.KeySize, FITSKey.KeySize);

                    yield return new FITSUnit(buffer);

                }
            }


        }

        public static IEnumerable<FITSUnit> GenerateFromArray(byte[] array, FITSImageType type)
        {
            var size = Math.Abs((short)type) / 8;
            var n = (int)Math.Ceiling(1.0 * array.Length / UnitSizeInBytes);
            var m = n / size;
            byte[] mappedArray;
            if (BitConverter.IsLittleEndian)
                mappedArray = array.Reverse().ToArray();
            else
                mappedArray = array;

            var buffer = new byte[UnitSizeInBytes];

            for (var iUnit = 0; iUnit < n-1; iUnit++)
            {
                Array.Copy(mappedArray, iUnit * UnitSizeInBytes, buffer, 0, UnitSizeInBytes);
                yield return new FITSUnit(buffer);
            }

            Array.Clear(buffer, 0, UnitSizeInBytes);
            Array.Copy(mappedArray, (n-1) * UnitSizeInBytes, buffer, 0, mappedArray.Length - (n-1) * UnitSizeInBytes);

           yield return new FITSUnit(buffer);
        }
    }
}
