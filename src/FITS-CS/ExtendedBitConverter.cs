
using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace FITS_CS
{
    internal static class ExtendedBitConverter
    {
        public static byte[] GetBytes(this Complex c)
        {
            var array = new byte[2 * sizeof(double)];
            c.GetBytes(array);
            return array;
        }

        public static void GetBytes(this Complex c, Span<byte> buffer)
        {
            if (buffer.Length < 2 * sizeof(double))
            {
                throw new ArgumentOutOfRangeException(nameof(buffer));
            }

            Unsafe.WriteUnaligned(ref buffer[0], c.Real);
            Unsafe.WriteUnaligned(ref buffer[sizeof(double)], c.Imaginary);
        }
        
        public static byte[] GetBytes(this ReadOnlySpan<char> s)
        {
            if (s.IsEmpty)
            {
                return Array.Empty<byte>();
            }

            var array = new byte[s.Length * sizeof(char)];
            s.GetBytes(array);
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] GetBytes(this string s) => s.AsSpan().GetBytes();
        
        public static void GetBytes(this ReadOnlySpan<char> s, Span<byte> data)
        {
            if (data.Length < s.Length * sizeof(char))
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }
            MemoryMarshal.AsBytes(s).CopyTo(data);
        }

        public static Complex ToComplex(ReadOnlySpan<byte> data)
        {
            if (data.Length < 2 * sizeof(double))
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            return new Complex(
                Unsafe.ReadUnaligned<double>(ref Unsafe.AsRef(in data[0])),
                Unsafe.ReadUnaligned<double>(ref Unsafe.AsRef(in data[sizeof(double)]))
            );
        }
        
        public static string ToString(ReadOnlySpan<byte> data)
        {
            const int stackAllocLimit = FitsKey.KeySize * sizeof(char);
            var len = data.Length / sizeof(char);
            Span<char> buff = len > stackAllocLimit
                ? new char[len]
                : stackalloc char[len];

            Span<byte> view = MemoryMarshal.AsBytes(buff);
            data.CopyTo(view);

            return buff.ToString();
        }

        public static void GetAsciiChars(ReadOnlySpan<byte> source, Span<char> target)
        {
            if (source.Length != target.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            for (var i = 0; i < source.Length; i++)
            {
                target[i] = (char)source[i];
            }
        }
    }
}
