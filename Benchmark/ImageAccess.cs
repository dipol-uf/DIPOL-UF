//    This file is part of Dipol-3 Camera Manager.

//     MIT License
//     
//     Copyright(c) 2018 Ilia Kosenkov
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

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DipolImage;

namespace Benchmark
{
    [SimpleJob]
    public class ImageAccess
    {
        private Image _image;
        //private Image _image2;
        //private Image _image3;


        [ParamsSource(nameof(szSource))]
        public (int Width, int Height) size;

        public IEnumerable<(int Width, int Height)> szSource => new[]
        {
            (Width: 640, Height: 480),
            (Width: 1024, Height: 768)
        };

        [GlobalSetup]
        public void GlobalSetup()
        {
            var data = Enumerable.Range(0, size.Width * size.Height)
                                 .Select(i => 1.0 * i)
                                 .ToArray();

            _image = new Image(data, size.Width, size.Height);
            //_image2 = _image.Copy();
            //_image3 = _image.Copy();

        }

        [Benchmark(Baseline = true)]
        public void This()
        {
            var sum = 0.0;

            for(var i = 0; i < _image.Height; i++)
                for (var j = 0; j < _image.Width; j++)
                    sum += (double)_image[i, j];
        }

        [Benchmark]
        public void Get()
        {
            var sum = 0.0;

            for (var i = 0; i < _image.Height; i++)
                for (var j = 0; j < _image.Width; j++)
                    sum += _image.Get<double>(i, j);
        }
    }
}
