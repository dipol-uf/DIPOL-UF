using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Parameters;

namespace Benchmark
{
    //[LegacyJitX64Job]
    //[LegacyJitX86Job]
    [CoreJob]
    [RyuJitX64Job]
    //[RPlotExporter]
    public class ArrayCopyBench
    {
        private Random _r;
        private double[] _srcArray;
        private byte[] _target1;
        private byte[] _target2;
        private byte[] _target3;
        private byte[] _target4;

        public IEnumerable<int> SourceOfN =>
            Enumerable.Range(3, 8)
                      .Select(i => (int)Math.Pow(4, i));

        [ParamsSource(nameof(SourceOfN))]
        public int N;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _r = new Random();
            _srcArray = Enumerable.Range(0, N)
                                  .Select(i => _r.NextDouble())
                                  .ToArray();
            _target1 = new byte[N * sizeof(double)];
            _target2 = new byte[N * sizeof(double)];
            _target3 = new byte[N * sizeof(double)];
            _target4 = new byte[N * sizeof(double)];

        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            Array.Clear(_target1, 0, _target1.Length);
            Array.Clear(_target2, 0, _target2.Length);
            Array.Clear(_target3, 0, _target3.Length);
            Array.Clear(_target3, 0, _target4.Length);

        }

        //[Benchmark(Baseline = true)]
        //public void ArrayCopy()
        //{
        //    Array.Copy(_srcArray, _target1, _srcArray.Length);
        //}

        [Benchmark(Baseline = true)]
        public void BufferCopy()
        {
            Buffer.BlockCopy(_srcArray, 0, _target4, 0, N * sizeof(double));
        }

        [Benchmark]
        public void MarshalCopy_1()
        {
            var handle = default(GCHandle);

            try
            {
                handle = GCHandle.Alloc(_srcArray, GCHandleType.Pinned);
                Marshal.Copy(handle.AddrOfPinnedObject(), _target2, 0, _target2.Length);
            }
            finally
            {
                handle.Free();
            }

        }

        [Benchmark]
        public void MarshalCopy_2()
        {
            var handle = default(GCHandle);

            try
            {
                handle = GCHandle.Alloc(_target3, GCHandleType.Pinned);
                Marshal.Copy(_srcArray, 0, handle.AddrOfPinnedObject(), N);
            }
            finally
            {
                handle.Free();
            }

        }

        [Benchmark]
        public unsafe void MarshalCopy_3()
        {
            fixed(double* pSrc = _srcArray)
                fixed (byte* pTar = _target1)
                    Buffer.MemoryCopy(pSrc, pTar, N * sizeof(double), N * sizeof(double));

        }


    }
}
