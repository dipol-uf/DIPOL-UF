using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Toolchains.InProcess;
using FITS_CS;

namespace Benchmark
{
    [SimpleJob]
    [RPlotExporter]
    public class KeyWordAccess
    {
        private FitsKey _key;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _key = new FitsKey("TEST", FitsKeywordType.Float, 1e30);
        }

        [Benchmark(OperationsPerInvoke = 256, Baseline = true)]
        public void RawCast()
        {
            var result = (double) _key.RawValue;
        }

        [Benchmark(OperationsPerInvoke = 256)]
        public void GetValue()
        {
            var result = _key.GetValue<double>();
        }

    }
}
