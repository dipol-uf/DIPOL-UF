using System.Runtime.CompilerServices;
using MathNet.Numerics;

namespace DipolImage
{
    internal static class Ops
    {
        private const int ByteSize = 8;
        // x - y < 0 -> 31st bit is 1, otherwise 0
        // x - y >> 31 -> either all 1 or all 0
        // x ^ y & (x - y >> 31) is either x ^ y or 0
        // x ^ (...) is either x ^ x ^ y or x ^ 0
        // if x < y then y else x
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Max(int x, int y) => (int) (x ^ ((x ^ y) & (((long) x - y) >> (sizeof(long) * 8 - 1))));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Max(uint x, uint y) => (uint)(x ^ ((x ^ y) & (((long)x - y) >> (sizeof(long) * 8 - 1))));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Max(short x, short y) => (short)(x ^ ((x ^ y) & ((x - y) >> (sizeof(int) * 8 - 1))));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Max(ushort x, ushort y) => (ushort)(x ^ ((x ^ y) & ((x - y) >> (sizeof(int) * 8 - 1))));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Max(byte x, byte y) => (byte)(x ^ ((x ^ y) & ((x - y) >> (sizeof(int) * 8 - 1))));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float x, float y) =>
            (float.IsNaN(x), float.IsNaN(y)) switch
            {
                (false, false) => System.Math.Max(x, y),
                (true, true) => float.MinValue,
                (true, _) => y,
                (_, true) => x
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Max(double x, double y) =>
            (double.IsNaN(x), double.IsNaN(y)) switch
            {
                (false, false) => System.Math.Max(x, y),
                (true, true) => double.MinValue,
                (true, _) => y,
                (_, true) => x
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Min(int x, int y) => (int)(y ^ ((x ^ y) & (((long)x - y) >> (sizeof(long) * 8 - 1))));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Min(uint x, uint y) => (uint)(y ^ ((x ^ y) & (((long)x - y) >> (sizeof(long) * 8 - 1))));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Min(short x, short y) => (short)(y ^ ((x ^ y) & ((x - y) >> (sizeof(int) * 8 - 1))));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Min(ushort x, ushort y) => (ushort)(y ^ ((x ^ y) & ((x - y) >> (sizeof(int) * 8 - 1))));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Min(byte x, byte y) => (byte)(y ^ ((x ^ y) & ((x - y) >> (sizeof(int) * 8 - 1))));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equal(double x, double y) => x.AlmostEqual(y) || double.IsNaN(x) && double.IsNaN(y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equal(float x, float y) => x.AlmostEqual(y) || float.IsNaN(x) && float.IsNaN(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float x, float y) =>
            (float.IsNaN(x), float.IsNaN(y)) switch
            {
                (false, false) => System.Math.Min(x, y),
                (true, true) => float.MaxValue,
                (true, _) => y,
                (_, true) => x
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Min(double x, double y) =>
            (double.IsNaN(x), double.IsNaN(y)) switch
            {
                (false, false) => System.Math.Min(x, y),
                (true, true) => double.MaxValue,
                (true, _) => y,
                (_, true) => x
            };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Clamp(byte x, byte low, byte high) => Min(high, Max(low, x));
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        public static ushort Clamp(ushort x, ushort low, ushort high) => Min(high, Max(low, x));
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        public static short Clamp(short x, short low, short high) => Min(high, Max(low, x));
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        public static uint Clamp(uint x, uint low, uint high) => Min(high, Max(low, x));
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        public static int Clamp(int x, int low, int high) => Min(high, Max(low, x));
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        public static float Clamp(float x, float low, float high) => Min(high, Max(low, x));
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        public static double Clamp(double x, double low, double high) => Min(high, Max(low, x));







    }
}
