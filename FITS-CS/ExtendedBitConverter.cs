//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018-2019 Ilia Kosenkov
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
using System.Numerics;
using System.Text;

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
