using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using DipolImage;
using FITS_CS;

namespace Benchmark
{
    public class Config : ManualConfig
    {
        public Config()
        {
            Add(Job.LegacyJitX64);
            Add(Job.RyuJitX64);
            Add(Job.RyuJitX86);

            Add(RPlotExporter.Default);
            Add(AsciiDocExporter.Default);

            Add(TargetMethodColumn.Method);
            Add(StatisticColumn.Max);
            Add(StatisticColumn.Min);
            Add(StatisticColumn.OperationsPerSecond);

        }
    }

    [Config(typeof(Config))]
    [MemoryDiagnoser]
    public class IOBench
    {

        private string _path;
        private byte[] _data;
        private byte[] _imgData;

        //[Params(8_192, 16_384)]
        [Params(512)]
        public int size;
        public int width => size;
        public int height => size;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _data = new byte[width * height * sizeof(int)];
            new Random().NextBytes(_data);

            var image = new Image(_data, width, height, TypeCode.Int32);

            _path = Path.GetRandomFileName() + ".fits";

            FitsStream.WriteImage(image, FitsImageType.Int32, _path);

            using (var memStr = new MemoryStream())
            {
                FitsStream.WriteImage(image, FitsImageType.Int32, memStr);
                _imgData = memStr.ToArray();
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            File.Delete(_path);
        }

        
        [Benchmark(Description = "Mem <-", OperationsPerInvoke = 96)]
        public void WriteToMemory()
        {
            var image = new Image(_data, width, height, TypeCode.Int32);

            using (var str = new MemoryStream())
            {
                FitsStream.WriteImage(image, FitsImageType.Int32, str);
            }
        }

        [Benchmark(Description = "Mem ->", OperationsPerInvoke = 96)]
        public void ReadFromMemory()
        {
            using (var str = new MemoryStream(_imgData))
            {
                var image = FitsStream.ReadImage(str, out _);
            }
        }
        

        [Benchmark(Description = "Disk <-", OperationsPerInvoke = 96)]
        public void WriteToDisk()
        {
            var image = new Image(_data, width, height, TypeCode.Int32);
            var path = Path.GetRandomFileName() + ".fits";

            FitsStream.WriteImage(image, FitsImageType.Int32, path);

            File.Delete(path);
        }

        [Benchmark(Description = "Disk ->", OperationsPerInvoke = 96)]
        public void ReadFromDisk()
        {
            var image = FitsStream.ReadImage(_path, out _);
        }


    }

}
