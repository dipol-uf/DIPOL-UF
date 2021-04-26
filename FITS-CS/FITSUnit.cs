using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;

namespace FITS_CS
{
    public class FitsUnit
    {
        public static readonly int UnitSizeInBytes = 2880;

        // ReSharper disable once InconsistentNaming
        internal readonly byte[] _data = new byte[UnitSizeInBytes];

        public IReadOnlyList<byte> Data => _data;

        private FitsUnit(byte[] data, bool isKeywords = false)
        {
            Array.Copy(data, _data, data.Length);

            IsKeywords = isKeywords;
        }

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
        }

        public bool IsKeywords { get; }

        public bool IsData => !IsKeywords;

        public bool TryGetKeys(
            [MaybeNullWhen(false)] out List<FitsKey> keys
        )
        {
            keys = null;
            if (IsData)
                return false;

            var n = UnitSizeInBytes / FitsKey.KeySize;

            keys = new List<FitsKey>(n);


            for (var i = 0; i < n; i++)
                keys.Add(new FitsKey(_data, i * FitsKey.KeySize));

            keys = keys.Where(x => !x.IsEmpty).ToList();
            return true;

        }

        public T[] GetData<T>() where T : struct
        {
            var size = Marshal.SizeOf<T>();
            // Hardcoded values 
            if (size != 1 &&
                size != 2 &&
                size != 4 &&
                size != 8)
                throw new ArgumentException(
                    $"{typeof(T)} is not compatible with allowed {nameof(FitsImageType)} types.");

            var workData = new byte[_data.Length];
            Array.Copy(_data, workData, _data.Length);
            var n = UnitSizeInBytes / size;
            var result = new T[n];

            if (BitConverter.IsLittleEndian && size > 1)
            {
                if(size == 2)
                    for (var i = 0; i < n; i++)
                        ArrayReverseBy2(workData, i * 2);
                else if(size == 4)
                    for (var i = 0; i < n; i++)
                        ArrayReverseBy4(workData, i * 4);
                else if (size == 8)
                    for (var i = 0; i < n; i++)
                        ArrayReverseBy8(workData, i * 8);

                // [size]
                //else
                //    for (var i = 0; i < n; i++)
                //        ArrayReverse(workData, i * size, size);
            }

            //var handle = GCHandle.Alloc(result, GCHandleType.Pinned);
            //Marshal.Copy(workData, 0, handle.AddrOfPinnedObject(), workData.Length);
            //handle.Free();
            Buffer.BlockCopy(workData, 0, result, 0, workData.Length);

            return result;
        }

        public static List<FitsUnit> GenerateFromKeywords(params FitsKey[] keys)
        {
            var keysPerUnit = UnitSizeInBytes / FitsKey.KeySize;
            var result = new List<FitsUnit>(keys.Length / keysPerUnit + 1);

            var nUnits = (int)Math.Ceiling(1.0 * keys.Length / keysPerUnit);

            var nEmpty = nUnits * keysPerUnit - keys.Length;

            var buffer = new byte[UnitSizeInBytes];

            for (var iUnit = 0; iUnit < nUnits; iUnit++)
            {

                if (iUnit != nUnits - 1)
                {
                    for (var iKey = 0; iKey < keysPerUnit; iKey++)
                        Array.Copy(keys[iUnit * keysPerUnit + iKey].Data, 0, buffer, iKey * FitsKey.KeySize, FitsKey.KeySize);

                    result.Add(new FitsUnit(buffer)); 
                }
                else
                {
                    for (var iKey = 0; iKey < keysPerUnit - nEmpty; iKey++)
                        Array.Copy(keys[iUnit * keysPerUnit + iKey].Data, 0, buffer, iKey * FitsKey.KeySize, FitsKey.KeySize);
                    for (var iKey = keysPerUnit - nEmpty; iKey < keysPerUnit; iKey++)
                        Array.Copy(FitsKey.Empty.Data, 0, buffer, iKey * FitsKey.KeySize, FitsKey.KeySize);

                    result.Add(new FitsUnit(buffer));

                }
            }

            return result;
        }

        public static List<FitsUnit> GenerateFromDataArray(byte[] array, FitsImageType type)
        {

            var size = Math.Abs((short)type) / 8;
            var n = (int)Math.Ceiling(1.0 * array.Length / UnitSizeInBytes);
            var result = new List<FitsUnit>(n);

            var buffer = new byte[UnitSizeInBytes];

            for (var iUnit = 0; iUnit < n; iUnit++)
            {
                var cpSize = Math.Min(UnitSizeInBytes, array.Length - iUnit * UnitSizeInBytes);
                Array.Copy(array, iUnit * UnitSizeInBytes, buffer, 0, cpSize);
                if(BitConverter.IsLittleEndian && size > 1)
                {
                    if(size == 2)
                        for (var i = 0; i < buffer.Length / 2; i++)
                            ArrayReverseBy2(buffer, i * 2);
                    else if (size == 4)
                        for (var i = 0; i < buffer.Length / 4; i++)
                            ArrayReverseBy4(buffer, i * 4);
                    else if (size == 8)
                        for (var i = 0; i < buffer.Length / 8; i++)
                            ArrayReverseBy8(buffer, i * 8);
                    
                    // As [size] is determined based on the [FitsImageType] enum,
                    // it can never be anything other than (8, 16, 32, 64) / 8,
                    // therefore other options are not considered
                    //else
                    //    for (var i = 0; i < buffer.Length / size; i++)
                    //        ArrayReverse(buffer, i * size, size);

                }
                result.Add(new FitsUnit(buffer, false));
            }

            return result;
        }

        //protected internal static void ArrayReverse(byte[] array, int start, int count)
        //{
        //    if (count <= 1)
        //        return;
           
        //    for (var i = 0; i < count / 2; i++)
        //    {
        //        var buff = array[start + i];
        //        array[start + i] = array[start + count - 1 -i];
        //        array[start + count - 1 - i] = buff;
        //    }
        //}

        protected internal static void ArrayReverseBy2(byte[] array, int start)
        {
            var buff = array[start + 1];
            array[start + 1] = array[start];
            array[start] = buff;
        }

        protected internal static void ArrayReverseBy4(byte[] array, int start)
        {
            var buff = array[start + 3];
            array[start + 3] = array[start];
            array[start] = buff;

            buff = array[start + 2];
            array[start + 2] = array[start + 1];
            array[start + 1] = buff;
        }

        protected internal static void ArrayReverseBy8(byte[] array, int start)
        {
            var buff = array[start + 7];
            array[start + 7] = array[start];
            array[start] = buff;

            buff = array[start + 6];
            array[start + 6] = array[start + 1];
            array[start + 1] = buff;

            buff = array[start + 5];
            array[start + 5] = array[start + 2];
            array[start + 2] = buff;

            buff = array[start + 4];
            array[start + 4] = array[start + 3];
            array[start + 3] = buff;
        }

    }
}
