using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FITS_CS
{
    internal static class ExtendedBitConverter
    {
        public static byte[] GetBytes(Complex c)
        {
            const int size = sizeof(double);
            var array = new byte[size * 2];
            Buffer.BlockCopy(BitConverter.GetBytes(c.Real), 0, array, 0, size);
            Buffer.BlockCopy(BitConverter.GetBytes(c.Imaginary), 0, array, size, size);

            return array;
        }

        public static byte[] GetBytes(string s)
        {
            if (s is null)
                throw new ArgumentNullException(nameof(s));

            const int size = sizeof(char);
            var array = new byte[size * s.Length];
            for (var i = 0; i < s.Length; i++)
                Buffer.BlockCopy(BitConverter.GetBytes(s[i]), 0, array, size * i, size);

            return array;
        }

        public static Complex ToComplex(byte[] array, int startIndex)
        {
            const int size = sizeof(double);

            if(array is null)
                throw  new ArgumentNullException(nameof(array));
            if(array.Length < 2 * size)
                throw new ArgumentException("Array is too small.");
            if (startIndex < 0 || array.Length - startIndex < 2 * size)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            return new Complex(BitConverter.ToDouble(array, startIndex), BitConverter.ToDouble(array, startIndex + size));
        }

        public static string ToString(byte[] array, int startIndex)
        {
            const int size = sizeof(char);

            if (array is null)
                throw new ArgumentNullException(nameof(array));
            if (array.Length < size)
                throw new ArgumentException("Array is too small.");
            if (array.Length % size != 0)
                throw new ArgumentException("Array does not contain a whole number of characters.");
            if (startIndex < 0 || array.Length - startIndex < size)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            
            var builder = new StringBuilder(array.Length / size);

            for (var i = 0; i < array.Length / size; i++)
                builder.Append(BitConverter.ToChar(array, startIndex + size * i));

            return builder.ToString();
        }
    }
}
