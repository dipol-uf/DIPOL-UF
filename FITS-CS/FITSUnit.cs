using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace FITS_CS
{
    public class FitsUnit
    {
        public const int UnitSizeInBytes = 2880;

        // ReSharper disable once InconsistentNaming
        internal readonly byte[] _data = new byte[UnitSizeInBytes];

        public IReadOnlyList<byte> Data => _data;

        private FitsUnit(ReadOnlySpan<byte> data, bool isKeywords)
        {
            if (data.Length != UnitSizeInBytes)
                throw new ArgumentException($"{nameof(data)} has wrong length");

            data.CopyTo(_data);

            IsKeywords = isKeywords;
        }

        public FitsUnit(ReadOnlySpan<byte> data)
        {
            if (data.Length != UnitSizeInBytes)
                throw new ArgumentException($"{nameof(data)} has wrong length");

            data.CopyTo(_data);

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
            {
                return false;
            }

            const int n = UnitSizeInBytes / FitsKey.KeySize;

            keys = new List<FitsKey>(n);


            for (var i = 0; i < n; i++)
                keys.Add(new FitsKey(_data, i * FitsKey.KeySize));

            keys = keys.Where(x => !x.IsEmpty).ToList();
            return true;

        }

        public T[] GetData<T>() where T : unmanaged
        {
            var size = Marshal.SizeOf<T>();
            // Hardcoded values 
            if (size != 1 &&
                size != 2 &&
                size != 4 &&
                size != 8)
                throw new ArgumentException(
                    $"{typeof(T)} is not compatible with allowed {nameof(FitsImageType)} types.");

            // This should have no rounding errors
            var n = UnitSizeInBytes / size;
            var result = new T[n];
            // Buffer should have `UnitSizeInBytes` size or `n` elements of type `T`
            Span<byte> buffer = MemoryMarshal.AsBytes(result.AsSpan());
            _data.CopyTo(buffer);

            if (BitConverter.IsLittleEndian && size > 1)
            {
                switch (size)
                {
                    case 2:
                    {
                        for (var i = 0; i < n; i++)
                        {
                            ReverseBy2(buffer.Slice(i * 2));
                        }

                        break;
                    }
                    case 4:
                    {
                        for (var i = 0; i < n; i++)
                        {
                            ReverseBy4(buffer.Slice(i * 4));
                        }

                        break;
                    }
                    case 8:
                    {
                        for (var i = 0; i < n; i++)
                        {
                            ReverseBy8(buffer.Slice(i * 8));
                        }
                        break;
                    }
                }
            }

            return result;
        }

        public static List<FitsUnit> GenerateFromKeywords(params FitsKey[] keys)
        {
            const int keysPerUnit = UnitSizeInBytes / FitsKey.KeySize;
            var result = new List<FitsUnit>(keys.Length / keysPerUnit + 1);

            var nUnits = (int)Math.Ceiling(1.0 * keys.Length / keysPerUnit);

            var nEmpty = nUnits * keysPerUnit - keys.Length;

            var pooledArray = ArrayPool<byte>.Shared.Rent(UnitSizeInBytes);
            Span<byte> buffer = pooledArray.AsSpan(0, UnitSizeInBytes);

            try
            {
                for (var iUnit = 0; iUnit < nUnits; iUnit++)
                {
                    buffer.Fill((byte)' ');
                    if (iUnit != nUnits - 1)
                    {
                        for (var iKey = 0; iKey < keysPerUnit; iKey++)
                        {
                            keys[iUnit * keysPerUnit + iKey]
                                .Data
                                .CopyTo(buffer.Slice(iKey * FitsKey.KeySize));
                        }

                        result.Add(new FitsUnit(buffer));
                    }
                    else
                    {
                        for (var iKey = 0; iKey < keysPerUnit - nEmpty; iKey++)
                        {
                            keys[iUnit * keysPerUnit + iKey]
                                .Data
                                .CopyTo(buffer.Slice(iKey * FitsKey.KeySize));
                        }

                        // TODO: Maybe not needed
                        for (var iKey = keysPerUnit - nEmpty; iKey < keysPerUnit; iKey++)
                        {
                            FitsKey.Empty.Data.CopyTo(buffer.Slice(iKey * FitsKey.KeySize));
                        }

                        result.Add(new FitsUnit(buffer));

                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pooledArray);
            }

            return result;
        }

        public static List<FitsUnit> GenerateFromDataArray(byte[] array, FitsImageType type)
        {

            var size = Math.Abs((short)type) / 8;
            var n = (int)Math.Ceiling(1.0 * array.Length / UnitSizeInBytes);
            var result = new List<FitsUnit>(n);

            var pooledArray = ArrayPool<byte>.Shared.Rent(UnitSizeInBytes);
            Span<byte> buffer = pooledArray.AsSpan(0, UnitSizeInBytes);

            ReadOnlySpan<byte> arrayView = array.AsSpan();
            try
            {
                for (var iUnit = 0; iUnit < n; iUnit++)
                {
                    buffer.Clear();
                    var cpSize = Math.Min(UnitSizeInBytes, array.Length - iUnit * UnitSizeInBytes);
                    arrayView.Slice(iUnit * UnitSizeInBytes, cpSize).CopyTo(buffer);
                    if (BitConverter.IsLittleEndian && size > 1)
                    {
                        switch (size)
                        {
                            case 2:
                            {
                                for (var i = 0; i < buffer.Length / 2; i++)
                                {
                                    ReverseBy2(buffer.Slice(i * 2));
                                }

                                break;
                            }
                            case 4:
                            {
                                for (var i = 0; i < buffer.Length / 4; i++)
                                {
                                    ReverseBy4(buffer.Slice(i * 4));
                                }

                                break;
                            }
                            case 8:
                            {
                                for (var i = 0; i < buffer.Length / 8; i++)
                                {
                                    ReverseBy8(buffer.Slice(i * 8));
                                }

                                break;
                            }
                        }

                        // As [size] is determined based on the [FitsImageType] enum,
                        // it can never be anything other than (8, 16, 32, 64) / 8,
                        // therefore other options are not considered
                    }

                    result.Add(new FitsUnit(buffer, false));
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pooledArray);
            }

            return result;
        }

        private static void ReverseBy2(Span<byte> data)
        {
            var temp = data[0];
            data[0] = data[1];
            data[1] = temp;
        }

        private static void ReverseBy4(Span<byte> data)
        {
            var temp = data[3];
            data[3] = data[0];
            data[0] = temp;

            temp = data[2];
            data[2] = data[1];
            data[1] = temp;
        }

        private static void ReverseBy8(Span<byte> data)
        {
            var temp = data[7];
            data[7] = data[0];
            data[0] = temp;

            temp = data[6];
            data[6] = data[1];
            data[1] = temp;

            temp = data[5];
            data[5] = data[2];
            data[2] = temp;

            temp = data[4];
            data[4] = data[3];
            data[3] = temp;
        }
        

    }
}
