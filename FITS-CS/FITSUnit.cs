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
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;

namespace FITS_CS
{
    public class FitsUnit
    {
        private byte[] _data = new byte[UnitSizeInBytes];

        public static readonly int UnitSizeInBytes = 2880;

        public byte[] Data => _data;

        public FitsUnit(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException($"{nameof(data)} is null");
            if (data.Length != UnitSizeInBytes)
                throw new ArgumentException($"{nameof(data)} has wrong length");

            Array.Copy(data, _data, data.Length);

            IsKeywords = Enumerable.Range(0, UnitSizeInBytes / FitsKey.KeySize)
                                   .All(i => FitsKey.IsFitsKey(_data, i * FitsKey.KeySize) ||
                                             FitsKey.IsEmptyKey(_data, i * FitsKey.KeySize));

            IsData = !IsKeywords;
        }

        public bool IsKeywords { get; }

        public bool IsData { get; }

        public bool TryGetKeys(out List<FitsKey> keys)
        {
            keys = null;
            if (IsData)
                return false;

            var n = UnitSizeInBytes / FitsKey.KeySize;

            keys = new List<FitsKey>(n);

            try
            {
                for(var i = 0; i < n; i++)
                    keys.Add(new FitsKey(Data, i * FitsKey.KeySize));

                keys = keys.Where(x => !x.IsEmpty).ToList();
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
            var workData = new byte[_data.Length];
            Array.Copy(_data, workData, _data.Length);
            var size = Marshal.SizeOf<T>();
            var n = UnitSizeInBytes / size;
            var result = new T[n];

            if(BitConverter.IsLittleEndian)
                for (var i = 0; i < n; i++)
                    Array.Reverse(workData, i * size, size);

            var handle = GCHandle.Alloc(result, GCHandleType.Pinned);
            Marshal.Copy(workData, 0, handle.AddrOfPinnedObject(), workData.Length);
            handle.Free();

            return result;
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

        public static IEnumerable<FitsUnit> GenerateFromKeywords(params FitsKey[] keys)
        {
            var keysPerUnit = UnitSizeInBytes / FitsKey.KeySize;

            var nUnits = (int)Math.Ceiling(1.0 * keys.Length / keysPerUnit);

            var nEmpty = nUnits * keysPerUnit - keys.Length;

            var buffer = new byte[UnitSizeInBytes];

            for (var iUnit = 0; iUnit < nUnits; iUnit++)
            {

                if (iUnit != nUnits - 1)
                {
                    for (var iKey = 0; iKey < keysPerUnit; iKey++)
                        Array.Copy(keys[iUnit * keysPerUnit + iKey].Data, 0, buffer, iKey * FitsKey.KeySize, FitsKey.KeySize);

                    yield return new FitsUnit(buffer);
                }
                else
                {
                    for (var iKey = 0; iKey < keysPerUnit - nEmpty; iKey++)
                        Array.Copy(keys[iUnit * keysPerUnit + iKey].Data, 0, buffer, iKey * FitsKey.KeySize, FitsKey.KeySize);
                    for (var iKey = keysPerUnit - nEmpty; iKey < keysPerUnit; iKey++)
                        Array.Copy(FitsKey.Empty.Data, 0, buffer, iKey * FitsKey.KeySize, FitsKey.KeySize);

                    yield return new FitsUnit(buffer);

                }
            }


        }

        public static List<FitsUnit> GenerateFromArray(byte[] array, FitsImageType type)
        {

            var size = Math.Abs((short)type) / 8;
            var n = (int)Math.Ceiling(1.0 * array.Length / UnitSizeInBytes);
            var result = new List<FitsUnit>(n);

            //if (BitConverter.IsLittleEndian)
            //for (var i = 0; i < array.Length / size; i++)
            //    Array.Reverse(mappedArray, i * size, size);


            var buffer = new byte[UnitSizeInBytes];

            for (var iUnit = 0; iUnit < n; iUnit++)
            {
                var cpSize = Math.Min(UnitSizeInBytes, array.Length - iUnit * UnitSizeInBytes);
                Array.Copy(array, iUnit * UnitSizeInBytes, buffer, 0, cpSize);
                if(BitConverter.IsLittleEndian)
                    for (var i = 0; i < buffer.Length / size; i++)
                        Array.Reverse(buffer, i * size, size);
                result.Add(new FitsUnit(buffer));
            }

            return result;
        }
    }
}
