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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DipolImage;
using FITS_CS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class FitsTests
    {
        private List<double> _testData;

        [TestInitialize]
        public void Initialzie()
        {
            var r = new Random();

            _testData = Enumerable.Range(0, 1024 * 512)
                      .Select((i) => ((100 * Math.Sqrt(i % 256) + r.Next(0, 512))))
                      .ToList();

        }

        [TestMethod]
        public void Test()
        {
            FITSStream.WriteImage(new Image(_testData.ToArray(), 1024, 512),
                FITSImageType.Double, "test_dbl.fits");
            FITSStream.WriteImage(new Image(_testData.Select((x) => (float) x).ToArray(), 1024, 512),
                FITSImageType.Single, "test_sng.fits");
            FITSStream.WriteImage(new Image(_testData.Select((x) => (int) x).ToArray(), 1024, 512),
                FITSImageType.Int32, "test_i32.fits");
            FITSStream.WriteImage(new Image(_testData.Select((x) => (short) x).ToArray(), 1024, 512),
                FITSImageType.Int16, "test_i16.fits");
            FITSStream.WriteImage(new Image(_testData.Select((x) => (byte)x).ToArray(), 1024, 512),
                FITSImageType.UInt8, "test_ui8.fits");
        }
    }
}
