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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DipolImage;
using FITS_CS;
using NUnit.Framework;

namespace Tests
{

    public class FitsTestsData
    {
        // ReSharper disable once InconsistentNaming
        public static IEnumerable Test_ReadWriteData
        {
            get
            {
                yield return new TestCaseData(4, 8);
                yield return new TestCaseData(8, 4);
                yield return new TestCaseData(256, 1);
                yield return new TestCaseData(1, 256);
                yield return new TestCaseData(431, 197);
                yield return new TestCaseData(512, 512);
                yield return new TestCaseData(1024, 1024);

            }
        }
    }
    [TestFixture]
    public class FitsTests
    {
        private readonly List<Action> _cleanupActions = new List<Action>();

        [TearDown]
        public void TearDown()
        {
            foreach (var action in _cleanupActions)
            {
                action();
            }

            _cleanupActions.Clear();
        }

        [Theory]
        [TestCaseSource(typeof(FitsTestsData), nameof(FitsTestsData.Test_ReadWriteData))]
        [Parallelizable(ParallelScope.Self)]
        public void Test_ReadWrite(int  width, int height)
        {

            var testData = Enumerable.Range(0, width * height)
                                 .Select(i => 32 * i)
                                 .ToList();

            void GenerateAssert<T>(string path, IEnumerable compareTo) where T: struct
            {
                using (var str = new FitsStream(new FileStream(GetPath(path), FileMode.Open)))
                {
                    var data = new List<T>(width * height);
                    while (str.TryReadUnit(out var unit))
                    {
                        if (unit.IsData)
                            data.AddRange(unit.GetData<T>());
                    }
                    Assert.That(data.Take(width * height), Is.EqualTo(compareTo).AsCollection);
                }
            }

            var file = $"test_dbl_{width:0000}x{height:0000}.fits";
            var doubleData = testData.Select(x => 1.0 * x).ToArray();
            FitsStream.WriteImage(new Image(doubleData, width, height),
                FITSImageType.Double, GetPath(file));
            AssumeExistsAndScheduleForCleanup(file);
            GenerateAssert<double>(file, doubleData);


            file = $"test_sng_{width:0000}x{height:0000}.fits";
            var singleData = testData.Select(x => 1.0f * x).ToArray();
            FitsStream.WriteImage(new Image(singleData, width, height),
                FITSImageType.Single, GetPath(file));
            AssumeExistsAndScheduleForCleanup(file);
            GenerateAssert<float>(file, singleData);


            file = $"test_ui8_{width:0000}x{height:0000}.fits";
            var byteData = testData.Select(x => (byte)x).ToArray();
            FitsStream.WriteImage(new Image(byteData, width, height),
                FITSImageType.UInt8, GetPath(file));
            AssumeExistsAndScheduleForCleanup(file);
            GenerateAssert<byte>(file, byteData);


            file = $"test_i16_{width:0000}x{height:0000}.fits";
            var shortData = testData.Select(x => (short) x).ToArray();
            FitsStream.WriteImage(new Image(shortData, width, height),
                FITSImageType.Int16, GetPath(file));
            AssumeExistsAndScheduleForCleanup(file);
            GenerateAssert<short>(file, shortData);


            file = $"test_i32_{width:0000}x{height:0000}.fits";
            var intData = testData.ToArray();
            FitsStream.WriteImage(new Image(intData, width, height),
                FITSImageType.Int32, GetPath(file));
            AssumeExistsAndScheduleForCleanup(file);
            GenerateAssert<int>(file, intData);


        }

        private void AssumeExistsAndScheduleForCleanup(string path)
        {
            AssumeExists(path);
            _cleanupActions.Add(() => CleanupFile(path));
        }

        private static string GetPath(in string path)
            => Path.GetFullPath(Path.IsPathRooted(path)
                ? path
                : Path.Combine(TestContext.CurrentContext.TestDirectory, path));

        private static void AssumeExists(in string path)
            => Assume.That(GetPath(path), Does.Exist,
                $"File \"{Path.GetFileName(path)}\" was not created.");

        private static void CleanupFile(in string path)
        {
            if (File.Exists(GetPath(path)))
                File.Delete(GetPath(path));
        }
            

    }
}
