using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Image = DipolImage.Image;

namespace FITS_CS
{
    public class FITSUnit
    {
        public static readonly int UnitSizeInBytes = 2880;

        private byte[] array = new byte[FITSUnit.UnitSizeInBytes];

        public byte[] Data => array;

        public FITSUnit(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException($"{nameof(data)} is null");
            if (data.Length != UnitSizeInBytes)
                throw new ArgumentException($"{nameof(data)} has wrong length");

            Array.Copy(data, array, data.Length);
        }

        public bool IsKeywords
            => Enumerable.Range(0, UnitSizeInBytes / FITSKey.KeySize)
            .Select(i => FITSKey.IsFITSKey(Data, i * FITSKey.KeySize))
            .Aggregate(true, (old, nv) => old & nv);

        public bool IsData
            => Enumerable.Range(0, UnitSizeInBytes / FITSKey.KeySize)
            .Select(i => FITSKey.IsFITSKey(Data, i * FITSKey.KeySize))
            .Contains(false);

        public bool TryGetKeys(out List<FITSKey> keys)
        {
            keys = null;
            if (IsData)
                return false;

            int n = UnitSizeInBytes / FITSKey.KeySize;

            keys = new List<FITSKey>(n);

            try
            {

                FITSKey currKey = new FITSKey(array, 0);
                FITSKey nextKey = new FITSKey(array, FITSKey.KeySize);
                int i = 1;
                while (i < n - 1)
                {
                    //currKey = new FITSKey(array, i * FITSKey.KeySize);
                    //nextKey = i < n-1 ? new FITSKey(array, (i+1) * FITSKey.KeySize) : null;

                    if (nextKey?.IsExtension ?? false)
                    {
                        currKey.Extension = nextKey.KeyString;
                        keys.Add(currKey);
                        if (i < n - 1)
                            currKey = new FITSKey(array, (++i) * FITSKey.KeySize);
                    }
                    else
                    {
                        keys.Add(currKey);
                        currKey = nextKey;
                    }
                    nextKey = ++i < n ? new FITSKey(array, i * FITSKey.KeySize) : null;
                }
                if (!currKey.IsExtension)
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
            int n = 0;
            int size = 0;
            byte[] mappedArray;
            if (BitConverter.IsLittleEndian)
                mappedArray = array.Reverse().ToArray();
            else mappedArray = array;
            
            if (typeof(T) == typeof(Double))
            {
                size = Math.Abs((short)FITSImageType.Double) / 8;
                n = UnitSizeInBytes / size;
                result = new T[n];
                for (int i = 0; i < n; i++)
                {
                    dynamic val = BitConverter.ToDouble(mappedArray, (n - i - 1) * size);
                    result[i] = val;
                }
            }
            else if (typeof(T) == typeof(Int16))
            {
                size = Math.Abs((short)FITSImageType.Int16) / 8;
                n = UnitSizeInBytes / size;
                result = new T[n];
                for (int i = 0; i < n; i++)
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
            int keysPerUnit = UnitSizeInBytes / FITSKey.KeySize;

            int nUnits = (int)Math.Ceiling(1.0 * keys.Length / keysPerUnit);

            int nEmpty = nUnits * keysPerUnit - keys.Length;

            byte[] buffer = new byte[UnitSizeInBytes];

            for (int iUnit = 0; iUnit < nUnits; iUnit++)
            {

                if (iUnit != nUnits - 1)
                {
                    for (int iKey = 0; iKey < keysPerUnit; iKey++)
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
            int size = Math.Abs((short)type) / 8;
            int n = (int)Math.Ceiling(1.0 * array.Length / UnitSizeInBytes);
            int m = n / size;
            byte[] mappedArray;
            if (BitConverter.IsLittleEndian)
                mappedArray = array.Reverse().ToArray();
            else
                mappedArray = array;

            byte[] buffer = new byte[UnitSizeInBytes];

            for (int iUnit = 0; iUnit < n-1; iUnit++)
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
