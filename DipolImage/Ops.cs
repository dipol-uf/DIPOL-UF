﻿using System.Runtime.CompilerServices;

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
        public static int Min(int x, int y) => (int)(y ^ ((x ^ y) & (((long)x - y) >> (sizeof(long) * 8 - 1))));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Min(uint x, uint y) => (uint)(y ^ ((x ^ y) & (((long)x - y) >> (sizeof(long) * 8 - 1))));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Min(short x, short y) => (short)(y ^ ((x ^ y) & ((x - y) >> (sizeof(int) * 8 - 1))));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Min(ushort x, ushort y) => (ushort)(y ^ ((x ^ y) & ((x - y) >> (sizeof(int) * 8 - 1))));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Min(byte x, byte y) => (byte)(y ^ ((x ^ y) & ((x - y) >> (sizeof(int) * 8 - 1))));


    }
}
