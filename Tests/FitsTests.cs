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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Resources;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using DipolImage;
using FITS_CS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using NUnit.Framework.Internal.Commands;
using Assert = NUnit.Framework.Assert;
using CollectionAssert = Microsoft.VisualStudio.TestTools.UnitTesting.CollectionAssert;
using TestContext = NUnit.Framework.TestContext;

namespace Tests
{

    public class FitsTestsData
    {
        // ReSharper disable for class InconsistentNaming
        public static IEnumerable Test_ReadWrite_Data
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
#if X64
                yield return new TestCaseData(4096, 4096);
#endif

            }
        }

        public static IEnumerable Test_IsFitsKey_Data
        {
            get
            {
                yield return new TestCaseData("SIMPLE  = 'some value'", 0, true);
                yield return new TestCaseData("SIMPLE  =                    T  / Some comment string", 0, true);
                yield return new TestCaseData("SIMPLE  =                    T  / Some comment string", 0, true);
                yield return new TestCaseData("TEST-1  = 123  / Some comment string", 0, true);
                yield return new TestCaseData("TEST+2  = 123  / Some comment string", 0, false);
                yield return new TestCaseData("TEST+2  = 123  / Some comment string", 0, false);
                yield return new TestCaseData("HISTORY ", 0, true);
                yield return new TestCaseData("COMMENT ", 0, true);
                yield return new TestCaseData("   SIMPLE  = 'some value'", 3, true);
                yield return new TestCaseData("   SIMPLE  = 'some value'", 5, false);
                yield return new TestCaseData("   SIMPLE  = 'some value'", 1, false);
                yield return new TestCaseData(new string(Encoding.ASCII
                                                       .GetChars(Enumerable.Range(1, 80)
                                                                           .Select(x => (byte)x)
                                                                           .ToArray())), 0, false);
            }
        }

        public static IEnumerable Test_FitsKeyCtor_Data
        {
            get
            {
                return new[]
                    {
                        (H:"SIMPLE", B:"=         T", C:"Comment", O:0, V: true),
                        (H:"NSIMPLE", B:"=         F", C:"", O:0, V: false),
                        (H:"STRSIM", B:"= 'some string'", C:"With comment", O:0, V: "some string" as object),
                        (H:"STRQUO", B:"= 'some string with ''quotes'''", C:"", O:0, V: "some string with 'quotes'" as object),
                        (H:"HISTORY", B:"History entry", C:"", O:0, V: "History entry"),
                        (H:"COMMENT", B:"Comment", C:"", O:0, V: "Comment"),
                        (H:"BLANK", B:"= ", C:" With comment", O:0, V: null),
                        (H:"INTVAL1", B:"=      1234", C:"Comment", O:0, V: 1234),
                        (H:"INTVAL2", B:"=      -1234", C:"Comment", O:0, V: -1234),
                        (H:"DBLVAL1", B:"=      1234.0", C:"Comment", O:0, V: 1234.0),
                        (H:"DBLVAL2", B:"=      -1234.0", C:"Comment", O:0, V: -1234.0),
                        (H:"DBLVAL3", B:"=      1234e30", C:"Comment", O:0, V: 1234e30),
                        (H:"DBLVAL4", B:"=      -1234e-30", C:"Comment", O:0, V: -1234e-30),
                        (H:"DBLVAL4", B:"=                   1223.5", C:"Comment", O:0, V: new System.Numerics.Complex(12, 23.5))
                    }
                    .Select(x => new TestCaseData(
                        new string(' ', x.O) + $"{x.H, -8}{x.B}"+
                            $"{(string.IsNullOrWhiteSpace(x.C) ? "" : " / " + x.C)}", 
                        x.O, x.H, x.V));
            }
        }

        public static IEnumerable Test_FitsKeyCtor_Throws_Data
        {
            get
            {
                yield return new TestCaseData(null, 0, typeof(ArgumentNullException));
                yield return new TestCaseData(new string('+', 13), 30, typeof(ArgumentException));
                yield return new TestCaseData("BITPIX  = slgjslhgskbdksd", 0, typeof(ArgumentException));
            }
        }

        public static IEnumerable Test_CreateNew_Data
        {
            get
            {
                yield return new TestCaseData("SIMPLE", FitsKeywordType.Logical, true, "First keyword");
                yield return new TestCaseData("OBJECT", FitsKeywordType.String, "NGC4151", "String keyword");
                yield return new TestCaseData("BITPIX", FitsKeywordType.Integer, 16, "Integer keyword");
                yield return new TestCaseData("BITPIX", FitsKeywordType.Integer, -64, "Integer keyword");
                yield return new TestCaseData("DOUBLE", FitsKeywordType.Float, -64.0, "Double keyword");
                yield return new TestCaseData("FLOAT", FitsKeywordType.Float, -64.0f, "Double keyword");
                yield return new TestCaseData("COMPLEX", FitsKeywordType.Complex, new Complex(-100, 1e30),
                    "Complex keyword");
                yield return new TestCaseData("COMM", FitsKeywordType.Complex, new Complex(-100, 1e30), 
                    "Extremely long overflowing comment that should exceed the size of the keyword and be trimmed.");
                yield return new TestCaseData("HISTORY", FitsKeywordType.Comment, "Plain content", "");
                yield return new TestCaseData("COMMENT", FitsKeywordType.Comment, "Plain content", "");
                yield return new TestCaseData("", FitsKeywordType.Blank, "Plain content", "");

            }
        }

        public static List<FitsKey> ExtraKeys => new List<FitsKey>(10)
            {
                new FitsKey("LGT", FitsKeywordType.Logical, true, "Logical true"),
                new FitsKey("LGF", FitsKeywordType.Logical, false, "Logical false"),
                new FitsKey("STREMP", FitsKeywordType.String, "", "Empty string"),
                new FitsKey("STRCNT", FitsKeywordType.String, "String with some content and quotes '", "String w content"),
                new FitsKey("INTZRO", FitsKeywordType.Integer, 0, "Zero int"),
                new FitsKey("INTPOS", FitsKeywordType.Integer, 100, "Positive int"),
                new FitsKey("INTNEG", FitsKeywordType.Integer, -100, "Negative int"),
                new FitsKey("INTNEG", FitsKeywordType.Integer, -100, "Negative int"),
                new FitsKey("FLOAT", FitsKeywordType.Float, -100e50),
                new FitsKey("CMPLX", FitsKeywordType.Complex, new Complex(-1.14151645e30, -1e45), ""),
                new FitsKey("HISTORY", FitsKeywordType.Comment, "First history entry")
            };
    }
    [TestFixture]
    public class FitsTests
    {
        private readonly ConcurrentQueue<Action> _cleanActions
            = new ConcurrentQueue<Action>();

        [TearDown]
        public void TearDown()
        {
            while (_cleanActions.TryDequeue(out var action))
                action();
        }

        [Theory]
        [TestCaseSource(typeof(FitsTestsData), nameof(FitsTestsData.Test_ReadWrite_Data))]
        [Parallelizable(ParallelScope.All)]
        public void Test_ReadWriteRaw(int  width, int height)
        {

            var testData = Enumerable.Range(0, width * height)
                                 .Select(i => 32 * i)
                                 .ToList();

            void GenerateAssert<T>(string path, IEnumerable compareTo) where T: struct
            {
                using (var str = new FitsStream(new FileStream(GetPath(path), FileMode.Open)))
                {
                    var bSize = System.Runtime.InteropServices.Marshal.SizeOf<T>();
                    var propSize = (int)(Math.Ceiling(1.0 * width * height * bSize / FitsUnit.UnitSizeInBytes)
                                   * FitsUnit.UnitSizeInBytes
                                   / bSize);
                    var data = new T[propSize];
                    var pos = 0;
                    while (str.TryReadUnit(out var unit))
                        if (unit.IsData)
                        {
                            var buffer = unit.GetData<T>();
                            Array.Copy(buffer, 0, data, pos, buffer.Length);
                            pos += buffer.Length;
                        }

                    Assert.That(data.Take(width * height),
                        Is.EqualTo(compareTo).AsCollection,
                        $"Failed for [{width:0000}x{height:0000}] of type {typeof(T)}.");
                }

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Forced, true, true);
            }

            var file = $"test_dbl_{width:0000}x{height:0000}.fits";
            var doubleData = testData.Select(x => 1.0 * x).ToArray();
            FitsStream.WriteImage(new Image(doubleData, width, height),
                FitsImageType.Double, GetPath(file));
            GenerateAssert<double>(file, doubleData);
            AssumeExistsAndScheduleForCleanup(file);


            file = $"test_sng_{width:0000}x{height:0000}.fits";
            var singleData = testData.Select(x => 1.0f * x).ToArray();
            FitsStream.WriteImage(new Image(singleData, width, height),
                FitsImageType.Single, GetPath(file));
            GenerateAssert<float>(file, singleData);
            AssumeExistsAndScheduleForCleanup(file);


            file = $"test_ui8_{width:0000}x{height:0000}.fits";
            var byteData = testData.Select(x => (byte)x).ToArray();
            FitsStream.WriteImage(new Image(byteData, width, height),
                FitsImageType.UInt8, GetPath(file));
            GenerateAssert<byte>(file, byteData);
            AssumeExistsAndScheduleForCleanup(file);


            file = $"test_i16_{width:0000}x{height:0000}.fits";
            var shortData = testData.Select(x => (short) x).ToArray();
            FitsStream.WriteImage(new Image(shortData, width, height),
                FitsImageType.Int16, GetPath(file));
            GenerateAssert<short>(file, shortData);
            AssumeExistsAndScheduleForCleanup(file);


            file = $"test_i32_{width:0000}x{height:0000}.fits";
            var intData = testData.ToArray();
            FitsStream.WriteImage(new Image(intData, width, height),
                FitsImageType.Int32, GetPath(file));
            GenerateAssert<int>(file, intData);
            AssumeExistsAndScheduleForCleanup(file);


        }

        [Theory]
        [TestCaseSource(typeof(FitsTestsData), nameof(FitsTestsData.Test_ReadWrite_Data))]
        [Parallelizable(ParallelScope.All)]
        public void Test_ReadWriteImage(int width, int height)
        {
            var testData = Enumerable.Range(0, width * height)
                                     .Select(i => 32 * i)
                                     .ToArray();

            var file = $"test_dbl_img_{width:0000}x{height:0000}.fits";
            var dblImage = new Image(testData.Select(x => 1.0 * x).ToArray(), width, height);
            FitsStream.WriteImage(dblImage, FitsImageType.Double, GetPath(file));
            Assert.That(FitsStream.ReadImage(GetPath(file), out _), Is.EqualTo(dblImage));
            AssumeExistsAndScheduleForCleanup(file);

            file = $"test_sng_img_{width:0000}x{height:0000}.fits";
            var sngImage = new Image(testData.Select(x => 1.0f * x).ToArray(), width, height);
            FitsStream.WriteImage(sngImage, FitsImageType.Single, GetPath(file));
            Assert.That(FitsStream.ReadImage(GetPath(file), out _), Is.EqualTo(sngImage));
            AssumeExistsAndScheduleForCleanup(file);

            file = $"test_ui8_img_{width:0000}x{height:0000}.fits";
            var ui8Image = new Image(testData.Select(x => (byte) x).ToArray(), width, height);
            FitsStream.WriteImage(ui8Image, FitsImageType.UInt8, GetPath(file));
            Assert.That(FitsStream.ReadImage(GetPath(file), out _), Is.EqualTo(ui8Image));
            AssumeExistsAndScheduleForCleanup(file);

            file = $"test_i16_img_{width:0000}x{height:0000}.fits";
            var i16Image = new Image(testData.Select(x => (short)x).ToArray(), width, height);
            FitsStream.WriteImage(i16Image, FitsImageType.Int16, GetPath(file));
            Assert.That(FitsStream.ReadImage(GetPath(file), out _), Is.EqualTo(i16Image));
            AssumeExistsAndScheduleForCleanup(file);

            file = $"test_i32_img_{width:0000}x{height:0000}.fits";
            var intImage = new Image(testData, width, height);
            FitsStream.WriteImage(intImage, FitsImageType.Int32, GetPath(file));
            Assert.That(FitsStream.ReadImage(GetPath(file), out _), Is.EqualTo(intImage));
            AssumeExistsAndScheduleForCleanup(file);

        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void Test_WriteExtraKeywords()
        {
            var image = new Image(new [] {1, 2, 3, 4}, 2, 2);
            const string path = "fits_keys_short.fits";
            var extraKeys = FitsTestsData.ExtraKeys;

            FitsStream.WriteImage(image, FitsImageType.Int32, GetPath(path),extraKeys);
            FitsStream.ReadImage(GetPath(path), out var readKeys);

            foreach (var key in extraKeys)
            {
                var readKey = readKeys.FirstOrDefault(x => x.Header == key.Header);
                Assert.That(readKey != FitsKey.Empty, Is.True);
                Assert.That(readKey == key, Is.True);
            }

            AssumeExistsAndScheduleForCleanup(path);
        }

        [Test]
        [TestCaseSource(typeof(FitsTestsData), nameof(FitsTestsData.Test_IsFitsKey_Data))]
        [Parallelizable(ParallelScope.All)]
        public void Test_IsFitsKey(string input, int offset, bool isKey)
        {
            var strBytes = Encoding.ASCII.GetBytes(input);
            var bytes = new byte[Math.Max(FitsKey.KeySize + offset, strBytes.Length)];

            Array.Copy(strBytes, 0, bytes, 0, Math.Min(bytes.Length, strBytes.Length));

            Assert.That(FitsKey.IsFitsKey(bytes, offset), Is.EqualTo(isKey));
        }

       
        [Test]
        [TestCaseSource(typeof(FitsTestsData), nameof(FitsTestsData.Test_FitsKeyCtor_Data))]
        [Parallelizable(ParallelScope.All)]
        public void Test_FitsKeyCtor(string input, int offset, string header, object value)
        {
            var bytes = Encoding.ASCII.GetBytes(input);
            var data =
                Enumerable.Range(0, Math.Max(FitsKey.KeySize, bytes.Length))
                          .Select(i => (byte) 32)
                          .ToArray();
            
            Array.Copy(bytes, 0, data, 0, Math.Min(data.Length, bytes.Length));

            FitsKey key = null;
            Assert.Multiple(() =>
            {
                Assert.That(() => key = new FitsKey(data, offset), Throws.Nothing);
                Assert.That(key.Header, Is.EqualTo(header.Trim()));
                Assert.That(key.RawValue, Is.EqualTo(value));
            });
        }

        [Test]
        [TestCaseSource(typeof(FitsTestsData), nameof(FitsTestsData.Test_FitsKeyCtor_Throws_Data))]
        [Parallelizable(ParallelScope.All)]
        public void Test_FitsKeyCtor_Throws(string input, int offset, Type exceptType)
        {
            byte[] data = null;
            if (!(input is null))
            {
                var bytes = Encoding.ASCII.GetBytes(input);


               data = Enumerable.Range(0, Math.Max(FitsKey.KeySize, bytes.Length))
                                .Select(i => (byte) 32)
                                .ToArray();

                Array.Copy(bytes, 0, data, 0, bytes.Length);
            }

            Assert.That(() => new FitsKey(data, offset), Throws.InstanceOf(exceptType));
        }


        [Test]
        [TestCaseSource(typeof(FitsTestsData), nameof(FitsTestsData.Test_CreateNew_Data))]
        [Parallelizable(ParallelScope.All)]
        public void Test_FitsKeyCtor_2(string header, FitsKeywordType type, object value, string comment)
        {
            FitsKey key = null;

            // ReSharper disable once ImplicitlyCapturedClosure
            Assert.That(() => key = new FitsKey(header, type, new object(), comment),
                Throws.InstanceOf<ArgumentException>());
            Assert.Multiple(() =>
            {
                Assert.That(() => key = new FitsKey(header, type, value, comment), Throws.Nothing,
                    $"Fails for {header}");
                Assert.That(key.Header, Is.EqualTo(header.Trim()),
                    $"Fails for {header}");
                Assert.That(key.RawValue, Is.EqualTo(value),
                    $"Fails for {header}");
                Assert.That(comment.StartsWith(key.Comment) || key.Comment.Length == 0, Is.True,
                    $"Fails for {header}");
                Assert.That(key.Type, Is.EqualTo(type),
                    $"Fails for {type}");
                Assert.That(key.KeyString, Has.Length.EqualTo(FitsKey.KeySize));
            });
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void Test_FitsKet_CreateNew_Throws()
        {
            Assert.Multiple(() =>
            {

                Assert.That(() => new FitsKey(null, FitsKeywordType.Blank, null),
                    Throws.ArgumentNullException);
                Assert.That(() => new FitsKey("EXTREMELYLONGHEADER", FitsKeywordType.Blank, null),
                    Throws.ArgumentException);
                Assert.That(() => new FitsKey("DOUBLE", FitsKeywordType.Float, double.MaxValue),
                    Throws.InstanceOf<OverflowException>());
                Assert.That(
                    () => new FitsKey("COMPLEX", FitsKeywordType.Complex, new Complex(double.MaxValue, 0)),
                    Throws.InstanceOf<OverflowException>());
                Assert.That(() => new FitsKey("STRING", FitsKeywordType.String,
                        new string('+', 123)),
                    Throws.ArgumentException);
                Assert.That(() => new FitsKey("NOTCOMM", FitsKeywordType.Comment,
                        null),
                    Throws.ArgumentException);
                Assert.That(() => new FitsKey("NOTBLNK", (FitsKeywordType)123,
                        null),
                    Throws.InstanceOf<NotSupportedException>());
            });
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void Test_IsFitsKey_Throws()
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => FitsKey.IsFitsKey(null), Throws.InstanceOf<ArgumentNullException>());
                Assert.That(() => FitsKey.IsFitsKey(new[] { (byte)0 }),
                    Throws.InstanceOf<ArgumentException>()
                          .With
                          .Message.Length.GreaterThan(0));
                Assert.That(() => FitsKey.IsFitsKey(Enumerable.Range(1, FitsKey.KeySize - 10)
                                                              .Select(x => (byte)x)
                                                              .ToArray()),
                    Throws.InstanceOf<ArgumentException>()
                          .With
                          .Message.Length.GreaterThan(0));
                Assert.That(() => FitsKey.IsFitsKey(Enumerable.Range(1, FitsKey.KeySize)
                                                              .Select(x => (byte)x)
                                                              .ToArray(), 10),
                    Throws.InstanceOf<ArgumentException>()
                          .With
                          .Message.Length.GreaterThan(0));
            });
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void Test_Empty()
            => Assert.That(FitsKey.Empty.IsEmpty, Is.True,
                "\"Empty\" keyword is not empty.");


        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void Test_Equals()
        {
            Assert.That(FitsKey.Empty.Equals(null), Is.False);
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void Test_ToString_GetHashCode()
        {
            foreach (var key in FitsTestsData.ExtraKeys)
            {
                Assert.That(key.ToString(), Is.EqualTo(key.KeyString));
                foreach (var key2 in FitsTestsData.ExtraKeys)
                {
                    var areEqual = key == key2;
                    var areHashEqual = key.GetHashCode() == key2.GetHashCode();
                    Assert.That(areEqual ^ areHashEqual, Is.False);
                }
            }
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void Test_FitsKey_GetValue()
        {
            void Test<T>(T input, FitsKeywordType type)
            {
                var key = new FitsKey("TSTKEY", type, input);
                Assert.That(key.GetValue<T>(), Is.EqualTo(input),
                    $"Failed to get {typeof(T)} from key of type {type}.");
            }

            Assert.Multiple(() =>
            {
                Test(float.MaxValue, FitsKeywordType.Float);
                Test(1.0 * float.MaxValue, FitsKeywordType.Float);
                Test("String", FitsKeywordType.String);
                Test(true, FitsKeywordType.Logical);
                Test(false, FitsKeywordType.Logical);
                Test(100, FitsKeywordType.Integer);
                Test(-100, FitsKeywordType.Integer);
                Test(int.MaxValue, FitsKeywordType.Integer);
                Test(new Complex(float.MaxValue, float.MinValue), FitsKeywordType.Complex);

                Assert.That(() => new FitsKey("THROWS", FitsKeywordType.String, "TESTSTR").GetValue<double>(),
                    Throws.InstanceOf<TypeAccessException>(),
                    "Should throw if underlying and provided type mismatch.");
            });

        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void Test_FitsKey_FactoryMethods()
        {
            const string cStr = "Comment str";
            const string hStr = "History entry";
            var comment = FitsKey.CreateComment(cStr);
            var history = FitsKey.CreateHistory(hStr);

            Assert.That(comment.Header == "COMMENT" && 
                        comment.Type == FitsKeywordType.Comment && 
                        comment.RawValue is string s1 && 
                        s1 == cStr &&
                        comment.GetValue<string>() == cStr);

            Assert.That(history.Header == "HISTORY" &&
                        history.Type == FitsKeywordType.Comment &&
                        history.RawValue is string s2 &&
                        s2 == hStr &&
                        history.GetValue<string>() == hStr);
        }


        [Theory]
        [DeployItem(
            "../../../Test inputs/UITfuv2582gc.fits", 
            CopyTo = "_dispose_twice_and_close.fits",
            ForceOverwrite = true)]
        [Parallelizable(ParallelScope.Self)]
        public void Test_Fits_DisposeTwiceAndClose()
        {
            var path = GetPath("_dispose_twice_and_close.fits");

            FitsStream str = null;
            Assume.That(() => str = new FitsStream(new FileStream(path, FileMode.Open)), 
                Throws.Nothing);

            Assert.That(str, Is.Not.Null);
            str.Dispose();
            Assert.That(str, Has.Property(nameof(str.IsDisposed)).True);
            Assert.That(str.Dispose,
                Throws.Nothing);
            Assert.That(str, Has.Property(nameof(str.IsDisposed)).True);
            Assert.That(str.Close,
                Throws.Nothing);
            Assert.That(str, Has.Property(nameof(str.IsDisposed)).True);
            
            Assert.That(str.ReadUnit, 
                Throws.InstanceOf<ObjectDisposedException>());
            AssumeExistsAndScheduleForCleanup("_dispose_twice_and_close.fits");
            
        }

        [Test]
        [DeployItem("../../../Test inputs/UITfuv2582gc_unsupp_bitpix.fits")]
        [Parallelizable(ParallelScope.Self)]
        public void Test_Fits_ReadImage_Unsupported_Format()
        {
            Assert.That(() => FitsStream.ReadImage(GetPath("UITfuv2582gc_unsupp_bitpix.fits"), out _),
                Throws.InstanceOf<NotSupportedException>());
            AssumeExistsAndScheduleForCleanup("UITfuv2582gc_unsupp_bitpix.fits");
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void Test_Fits_CannotWrite()
        {
            using (var fstr = new FitsStream(new MemoryStream(new byte[1], false)))
                Assert.Multiple(() =>
                {
                    Assert.That(fstr.CanWrite, Is.False);
                    // ReSharper disable once AccessToDisposedClosure
                    Assert.That(() => fstr.Write(new byte[] {1}, 0, 1),
                        Throws.InstanceOf<NotSupportedException>());
                });
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        public void Test_Fits_Seeking()
        {
            using (var fstr = new FitsStream(new MemoryStream(new byte[2880], true)))
                Assert.Multiple(() =>
                {
                    Assert.That(fstr.CanRead, Is.True);
                    Assert.That(fstr.CanSeek, Is.True);

                    Assert.That(fstr.Position, Is.EqualTo(0));

                    fstr.Seek(1440, SeekOrigin.Current);
                    Assert.That(fstr.Position, Is.EqualTo(1440));

                    fstr.Position /= 2;
                    Assert.That(fstr.Position, Is.EqualTo(720));
                });
        }

        [Test]
        [Parallelizable(ParallelScope.Self)]
        public void Test_SetLong_NotImplemented()
        {
            using (var fstr = new FitsStream(new MemoryStream(new byte[1], true)))
            {
                // ReSharper disable once AccessToDisposedClosure
                Assert.That(() => fstr.SetLength(0),
                    Throws.InstanceOf<NotSupportedException>());
            }
        }

        [Theory]
        [Parallelizable(ParallelScope.Self)]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        public void Test_Fits_CannotRead()
        {
            var path = "write_only.fits";

            using (var str = new FitsStream(new FileStream(GetPath(path), 
                FileMode.Create, FileAccess.Write)))
            {
                var buff = new byte[1];
                Assume.That(str.CanWrite && !str.CanRead, Is.True);
                Assert.That(() => str.Write(new byte[] {128}, 0, 1),
                    Throws.Nothing);
                str.Flush();
                Assert.That(() => str.Read(buff, 0, 1),
                    Throws.InstanceOf<NotSupportedException>());

                Assert.That(() => str.ReadUnit(),
                    Throws.InstanceOf<NotSupportedException>());
            }

            AssumeExistsAndScheduleForCleanup(path);
        }

        private void AssumeExistsAndScheduleForCleanup(string path)
        {
            AssumeExists(path);
            _cleanActions.Enqueue(() => CleanupFile(path));
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
