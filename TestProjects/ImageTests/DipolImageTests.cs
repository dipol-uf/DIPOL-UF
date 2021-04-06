using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using DipolImage;
using NUnit.Framework;

namespace ImageTests
{

    public class DipolImageTests_DataProvider
    {
        private static IEnumerable<TypeCode> AllowedTypes { get; } = ImageBase.AllowedPixelTypes;

        private static IEnumerable<(int Width, int Height)> TransformSizes { get; } = new[]
        {
            (10, 10),
            (10, 20),
            (20, 10),
            (13, 19),
            (512, 512),
        };

        private static IEnumerable<(RotateBy Left, RotateBy Right)> EqualRotations { get; } = new[]
        {
            (RotateBy.Deg0, RotateBy.PiTimes0),
            (RotateBy.Deg90, RotateBy.PiTimes3Over2),
            (RotateBy.Deg180, RotateBy.Pi),
            (RotateBy.Deg270, RotateBy.PiOver2),
            (RotateBy.Deg360, RotateBy.PiTimes2),

            (RotateBy.Deg90, RotateBy.Deg270),
            (RotateBy.Deg180, RotateBy.Deg180),
            (RotateBy.Deg270, RotateBy.Deg90),
        };

        private static IEnumerable<(RotateBy Roataion, RotationDirection Direction, int N)>
            IdentityRotations { get; } = new[]
        {
            (RotateBy.Deg0, RotationDirection.Left, 0),
            (RotateBy.Deg0, RotationDirection.Right, 0),
            
            (RotateBy.Deg90, RotationDirection.Left, 4),
            (RotateBy.Deg90, RotationDirection.Right, 4),
            
            (RotateBy.Deg180, RotationDirection.Left, 2),
            (RotateBy.Deg180, RotationDirection.Right, 2),
            
            (RotateBy.Deg270, RotationDirection.Left, 4),
            (RotateBy.Deg270, RotationDirection.Right, 4),
        };

        private static IEnumerable<ReflectionDirection> ReflectionDirections { get; } = new[]
        {
            ReflectionDirection.Horizontal,
            ReflectionDirection.Vertical
        };

        public static IEnumerable AllowedTypesSource => AllowedTypes.Select(x => new TestCaseData(x));

        public static IEnumerable ReflectionSource =>
            from tp in AllowedTypes
            from sizes in TransformSizes
            from refDir in ReflectionDirections
            select new TestCaseData(sizes.Width, sizes.Height, tp, refDir);

        public static IEnumerable RotationSource =>
            from tp in AllowedTypes
            from sizes in TransformSizes
            from rots in EqualRotations
            select new TestCaseData(sizes.Width, sizes.Height, tp, rots.Left, rots.Right);

        public static IEnumerable RotationRepSource =>
            from tp in AllowedTypes
            from sizes in TransformSizes
            from rots in IdentityRotations
            select new TestCaseData(sizes.Width, sizes.Height, tp, rots.Roataion, rots.Direction, rots.N);
    }

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class DipolImageTests
    {
        private Random _r = null!;
        private int[] _testArray = null!;
        private byte[] _testByteArray = null!;
        private byte[] _veryLargeByteArray = null!;

        [SetUp]
        public void Test_Initialize()
        {
            _r = new Random();
            _testArray = new int[32];
            for (var i = 0; i < _testArray.Length; i++)
            {
                _testArray[i] = _r.Next();
            }

            _testByteArray = new byte[512];
            _r.NextBytes(_testByteArray);

            _veryLargeByteArray = new byte[1024 * 1024 * 8];
            _r.NextBytes(_veryLargeByteArray);
        }

        [Test]
        public void Test_ConstructorThrows()
        {
            Assert.Multiple(() =>
            {
                // ReSharper disable for method ObjectCreationAsStatement

                Assert.Throws<ArgumentNullException>(() => new Image(null!, 2, 3));
                Assert.Throws<ArgumentOutOfRangeException>(() => new Image(_testArray, 0, 3));
                Assert.Throws<ArgumentOutOfRangeException>(() => new Image(_testArray, 10, 0));
                Assert.Throws<ArgumentException>(() => new Image(new[] {"s"}, 1, 1));

                Assert.Throws<ArgumentNullException>(() => new Image(null!, 1, 1, TypeCode.Int16));
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new Image(_testByteArray, 0, 3, TypeCode.Int32));
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    new Image(_testByteArray, 10, 0, TypeCode.Int32));
                Assert.Throws<ArgumentException>(() => new Image(_testByteArray, 1, 1, TypeCode.Char));
                Assert.Throws<ArgumentException>(() => new Image(_testByteArray, 1, 1, (TypeCode) 45500));
            });
        }

        [Test]
        public void Test_ImageEqualsToArray()
        {
            var initArray = new[] {1, 2, 3, 4, 5, 6};

            var image = new Image(initArray, 2, 3);

            Assert.That(
                initArray[0] == image.Get<int>(0, 0) &&
                initArray[1] == image.Get<int>(0, 1) &&
                initArray[2] == image.Get<int>(1, 0) &&
                initArray[3] == image.Get<int>(1, 1) &&
                initArray[4] == image.Get<int>(2, 0) &&
                initArray[5] == image.Get<int>(2, 1),
                Is.True);
        }

        [Test]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.AllowedTypesSource))]
        public void Test_ImageInitializedFromBytes(TypeCode code)
        {
            const byte value = 23;

            var temp = Convert.ChangeType(value, code);
            byte[] bytes;
            if (code is TypeCode.Byte or TypeCode.SByte)
            {
                bytes = new[] {value};
            }
            else
            {
                var mi = typeof(BitConverter)
                         .GetMethods(BindingFlags.Public | BindingFlags.Static)
                         .First(
                             m => m.Name == "GetBytes" &&
                                  m.GetParameters().Length == 1 &&
                                  m.GetParameters().First().ParameterType == temp.GetType()
                         );
                bytes = (byte[]) mi.Invoke(null, new[] {temp})!;
            }

            var image = new Image(bytes, 1, 1, code);

            Assert.Multiple(() =>
            {
                Assert.That(image[0, 0], Is.EqualTo(temp));
                Assert.That(image.UnderlyingType, Is.EqualTo(code));
            });

        }

        [Test]
        [Repeat(4)]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.AllowedTypesSource))]
        public void Test_SpanInit(TypeCode code)
        {

            var type = Type.GetType("System." + code) ?? throw new ArgumentException(nameof(code));
            var size = Marshal.SizeOf(type);
            var arr = _veryLargeByteArray.Take(size * 47 * 31).ToArray();


            var image1 = new Image(arr, 47, 31, code);
            var image2 = new Image(arr.AsSpan(), 47, 31, code);

            Assert.IsTrue(image1.Equals(image2));

        }

        [Test]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.AllowedTypesSource))]
        public void Test_GetBytes(TypeCode code)
        {
            const int val1 = 1;
            const int val2 = 123;
            var type = Type.GetType("System." + code) ?? throw new ArgumentException(nameof(code));
            var initArray = Array.CreateInstance(type, 2);
            initArray.SetValue(Convert.ChangeType(val1, code), 0);
            initArray.SetValue(Convert.ChangeType(val2, code), 1);


            var image = new Image(initArray, 2, 1);

            ReadOnlySpan<byte> bytes = image.ByteView();
            byte[] reconstructed;
            if (code == TypeCode.SByte)
            {
                var size = Marshal.SizeOf(type);
                reconstructed = new byte[2 * size];
                reconstructed[0] = (byte)((sbyte[]) initArray)[0];
                reconstructed[1] = (byte)((sbyte[]) initArray)[1];
            }
            else if (code == TypeCode.Byte)
            {
                var size = Marshal.SizeOf(type);
                reconstructed = new byte[2 * size];
                reconstructed[0] = ((byte[]) initArray)[0];
                reconstructed[1] = ((byte[]) initArray)[1];
            }
            else
            {
                var mi = typeof(BitConverter)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == "GetBytes" &&
                                m.GetParameters().Length == 1 &&
                                m.GetParameters().First().ParameterType == type);
                var size = Marshal.SizeOf(type);
                reconstructed = new byte[2 * size];
                Array.Copy(((byte[]) mi.Invoke(null, new[] {initArray.GetValue(0)})!)!, 0, reconstructed, 0, size);
                Array.Copy(((byte[]) mi.Invoke(null, new[] {initArray.GetValue(1)})!)!, 0, reconstructed, size, size);
            }

            Assert.IsTrue(bytes.SequenceEqual(reconstructed));
        }

        [Test]
        [Repeat(4)]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.AllowedTypesSource))]
        public void Test_Equals(TypeCode code)
        {

            var type = Type.GetType("System." + code) ?? throw new ArgumentException(nameof(code));
            var size = Marshal.SizeOf(type);
            var arr = _testByteArray.Take(size * 2 * 2).ToArray();

            var tempArr = new byte[arr.Length];
            Array.Copy(arr, tempArr, tempArr.Length);
            var modifyAt = Math.Min(tempArr.Length, 8);
            for (var i = 0; i < modifyAt; i++)
            {
                tempArr[i] = 255;
            }


            var image1 = new Image(arr, 2, 2, code);
            var image2 = new Image(arr, 2, 2, code);

            var wrImage1 = new Image(arr.Take(size * 2).ToArray(), 2, 1, code);
            var wrImage2 = new Image(arr.Take(size * 2).ToArray(), 1, 2, code);
            var wrImage3 = new Image(arr, 2, 2,
                code == TypeCode.Int16 ? TypeCode.UInt16 : TypeCode.Int16);
            var wrImage4 = new Image(tempArr, 2, 2, code);

            Assert.Multiple(() =>
                {
                    Assert.That(image1.Equals(image2), Is.True);
                    Assert.That(image2.Equals(image1), Is.True);
                    Assert.That(image1.Equals((object) image2), Is.True);

                    Assert.That(image1.Equals(wrImage1), Is.False);
                    Assert.That(image1.Equals(wrImage2), Is.False);
                    Assert.That(image1.Equals(wrImage3), Is.False);
                    Assert.That(image1.Equals(wrImage4), Is.False);
                    Assert.That(image1.Equals(null), Is.False);
                }
            );
        }

        [Test]
        public void Test_Copy()
        {
            var array = new byte[1024];
            _r.NextBytes(array);

            var img = new Image(array, 32, 16, TypeCode.Int16);
            Assert.That(img.Equals(img.Copy()), Is.True);
        }

    
        [Test]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.AllowedTypesSource))]

        public void Test_GetHashCode(TypeCode code)
        {

            var type = Type.GetType("System." + code) ?? throw new ArgumentException(nameof(code));
            var size = Marshal.SizeOf(type);
            var arr = _testByteArray.Take(size * 2 * 2).ToArray();

            var tempArr = new byte[arr.Length];
            Array.Copy(arr, tempArr, tempArr.Length);
            tempArr[0] = (byte) (tempArr[0] == 0 ? 127 : 0);
            var image1 = new Image(arr, 2, 2, code);
            var image2 = new Image(arr, 2, 2, code);

            var wrImage1 = new Image(tempArr, 2, 2, code);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(image1.GetHashCode(), image2.GetHashCode());
                Assert.AreNotEqual(image1.GetHashCode(), wrImage1.GetHashCode());
            });

        }

        [Test]
        [Repeat(4)]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.AllowedTypesSource))]
        public void Test_Max(TypeCode code)
        {
            var type = Type.GetType($"System.{code}") ?? throw new ArgumentException(nameof(code));
            var size = Marshal.SizeOf(type);

            var max = type
                      .GetFields(BindingFlags.Public | BindingFlags.Static)
                      .FirstOrDefault(fi => fi.Name == "MinValue")
                      ?.GetValue(null)
                      ?? throw new InvalidOperationException("Unable to find `MinValue`.");

            var image = new Image(_testByteArray, _testByteArray.Length / size, 1, code);
            for (var i = 0; i < image.Width; i++)
            {
                var val = image[0, i] as IComparable;
                if (val?.CompareTo(max) > 0)
                    max = Convert.ChangeType(val, code);
            }


            max = Convert.ChangeType(max, code);

            Assert.That(image.Max(), Is.EqualTo(max));

        }

        [Test]
        [Repeat(4)]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.AllowedTypesSource))]
        public void Test_Min(TypeCode code)
        {
            var type = Type.GetType($"System.{code}") ?? throw new ArgumentException(nameof(code));
            var size = Marshal.SizeOf(type);

            var min = type
                      .GetFields(BindingFlags.Public | BindingFlags.Static)
                      .FirstOrDefault(fi => fi.Name == "MaxValue")
                      ?.GetValue(null)
                      ?? throw new InvalidOperationException("Unable to find `MaxValue`.");

            var image = new Image(_testByteArray, _testByteArray.Length / size, 1, code);
            for (var i = 0; i < image.Width; i++)
            {
                var val = image[0, i] as IComparable;
                if (val?.CompareTo(min) < 0)
                {
                    if (type == typeof(float) && !float.IsNaN((float) val))
                        min = Convert.ChangeType(val, code);
                    else if (type == typeof(double) && !double.IsNaN((double) val))
                        min = Convert.ChangeType(val, code);
                    else if (type != typeof(double) && type != typeof(float))
                        min = Convert.ChangeType(val, code);

                }
            }



            min = Convert.ChangeType(min, code);

            Assert.That(image.Min(), Is.EqualTo(min));
        }

        [Test]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.AllowedTypesSource))]
        public void Test_Transpose(TypeCode code)
        {
            var type = Type.GetType("System." + code) ?? throw new ArgumentException(nameof(code));
            var size = Marshal.SizeOf(type);

            var image = new Image(_testByteArray, _testByteArray.Length / 2 / size, 2, code);
            var imageT = image.Transpose();

            Assert.Multiple(() =>
            {

                Assert.That(imageT.Height, Is.EqualTo(image.Width));
                Assert.That(imageT.Width, Is.EqualTo(image.Height));

                Assert.That(Enumerable.Range(0, image.Width * image.Height)
                        .All(i => image[i % 2, i / 2].Equals(imageT[i / 2, i % 2])),
                    Is.True);
            });

        }

        [Test]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.AllowedTypesSource))]
        public void Test_Type(TypeCode code)
        {
            var type = Type.GetType("System." + code) ?? throw new ArgumentException(nameof(code));
            var size = Marshal.SizeOf(type);
            var img = new Image(_testByteArray.Take(size * 2 * 2).ToArray(), 2, 2, code);
            Assert.That(img.Type, Is.EqualTo(type));
        }

        [Test]
        [Repeat(4)]
        public void Test_CastTo()
        {
            var testArray = _testArray.ToArray();
            var image = new Image(testArray, 4, _testArray.Length / 4);
            Assert.Multiple(() =>
            {
                Assert.That(image.Equals(image.CastTo<int, int>(x => x)), Is.True);
                Assert.That(() => image.CastTo<int, char>(x => x.ToString()[0]),
                    Throws.InstanceOf<ArgumentException>());
            });
            var otherArray = testArray.Select(x => (double) x).ToArray();

            var otherImage = new Image(otherArray, 4, otherArray.Length / 4);
            var srcCastImage = image.CastTo<int, double>(x => x);

            Assert.That(otherImage.Equals(srcCastImage, FloatingPointComparisonType.Exact), Is.True);
        }

        [Test]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.AllowedTypesSource))]
        public void Test_Clamp(TypeCode code)
        {
            var type = Type.GetType("System." + code) ?? throw new ArgumentException(nameof(code));
            var size = Marshal.SizeOf(type);
            var image = new Image(_testByteArray, _testByteArray.Length / 4 / size, 4, code);
            var fMx = type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .First(fi => fi.Name == "MaxValue");

            dynamic mMax = fMx.GetValue(null)!;

            var mx = code.ToString().Contains("U") || code.ToString().Contains("Byte") ? mMax / 2 : 5000;
            var mn = code.ToString().Contains("U") || code.ToString().Contains("Byte") ? mMax / 4 : -5000;

            Assert.That(() => image.Clamp(100, 10),
                Throws.InstanceOf<ArgumentException>());

            image.Clamp(mn, mx);

            var min = image.Min();
            var max = image.Max();

            Assert.Multiple(() =>
            {
                Assert.That(min.CompareTo(Convert.ChangeType(mn, code)),
                    Is.GreaterThanOrEqualTo(0));
                Assert.That(max.CompareTo(Convert.ChangeType(mx, code)),
                    Is.LessThanOrEqualTo(0));
            });
        }

        [Test]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.AllowedTypesSource))]
        public void Test_Scale(TypeCode code)
        {

            var type = Type.GetType("System." + code, true) ?? throw new ArgumentException(nameof(code));
            var arr = Array.CreateInstance(type, 4096);

            if (code == TypeCode.Byte)
            {
                for (var i = 0; i < arr.Length; i++)
                {
                    arr.SetValue((byte) (i % 256), i);
                }
            }
            else if (code == TypeCode.SByte)
            {
                for (var i = 0; i < arr.Length; i++)
                {
                    arr.SetValue((sbyte) (i % 128), i);
                }
            }
            else
            {
                for (var i = 0; i < arr.Length; i++)
                {
                    arr.SetValue(Convert.ChangeType(i, code), i);
                }
            }

            var image = new Image(arr, 1024, 4);

            Assert.That(() => image.Scale(100, 10),
                Throws.InstanceOf<ArgumentException>());

            image.Scale(1, 9);

            var min = image.Min();
            var max = image.Max();


            //Assert.Multiple(
            //    () =>
            {
                Assert.That(Math.Abs(min - 1) < double.Epsilon ||
                            Math.Abs(max + min - 10) < double.Epsilon, Is.True);
                Assert.That(Math.Abs(max - 9) < double.Epsilon ||
                            Math.Abs(max + min - 10) < double.Epsilon, Is.True);
            }
            //);
        }

        [Test] 
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.AllowedTypesSource))]
        public void Test_Percentile(TypeCode code)
        {
            var type = Type.GetType("System." + code) ?? throw new ArgumentException(nameof(code));


            const int n = 1024;
            var array = Array.CreateInstance(type, n);
            var dArray = new double[n];

            for (var i = 0; i < n / 4; i++)
            for (var j = 0; j < 4; j++)
            {
                array.SetValue(Convert.ChangeType((i + j) % 128, code), i * 4 + j);
                dArray[i * 4 + j] = i + j;
            }



            var image = new Image(array, 4, n / 4);

            dynamic mn = image.Min();
            dynamic mx = image.Max();

            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => image.Percentile(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => image.Percentile(2));
                Assert.That(image.Percentile(0), Is.EqualTo(mn));
                Assert.That(image.Percentile(1), Is.EqualTo(mx));
            });
        }

        [Test]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.ReflectionSource))]
        public void Test_Reflection(int width, int height, TypeCode typeCode, ReflectionDirection direction)
        {
            var type = Type.GetType($"System.{typeCode}") ?? throw new ArgumentException(nameof(typeCode));

            var array = Array.CreateInstance(type, width * height);

            for (var i = 0; i < width * height; i++)
            {
                array.SetValue(Convert.ChangeType(i % 128, type), i);
            }

            var image = new Image(array, width, height, copy: true);
            var ref1 = image.Reflect(ReflectionDirection.Horizontal);
            var ref2 = ref1.Reflect(ReflectionDirection.Horizontal);


            Assert.IsFalse(image.Equals(ref1, FloatingPointComparisonType.Exact));
            Assert.IsFalse(ref2.Equals(ref1, FloatingPointComparisonType.Exact));
            Assert.IsTrue(image.Equals(ref2, FloatingPointComparisonType.Exact));
        }

        [Test]
        public void Test_Reflection_Direct()
        {
            var image = Image.CreateTyped<int>(stackalloc int[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2}, 3, 4);

            var v1 = image.Reflect(ReflectionDirection.Vertical).TypedView<int>();
            Assert.IsTrue(v1.SequenceEqual(stackalloc int[] {0, 1, 2, 7, 8, 9, 4, 5, 6, 1, 2, 3}));

            var h1 = image.Reflect(ReflectionDirection.Horizontal).TypedView<int>();

            Assert.IsTrue(h1.SequenceEqual(stackalloc int[] {3, 2, 1, 6, 5, 4, 9, 8, 7, 2, 1, 0}));
        }

        [Test]
        public void Test_Rotation_Direct_Int32()
        {
            const int width = 3;
            const int height = 4;
            ReadOnlySpan<int> source = stackalloc int[]
            {
                1, 2, 3, 
                4, 5, 6, 
                7, 8, 9, 
                0, 1, 2,
            };
            ReadOnlySpan<int> by90 = stackalloc int[]
            {
                3, 6, 9, 2, 
                2, 5, 8, 1, 
                1, 4, 7, 0,
            };

            ReadOnlySpan<int> by180 = stackalloc int[]
            {
                2, 1, 0,
                9, 8, 7,
                6, 5, 4,
                3, 2, 1,
            };

            ReadOnlySpan<int> by270 = stackalloc int[]
            {
                0, 7, 4, 1,
                1, 8, 5, 2,
                2, 9, 6, 3,
            };

            var image = Image.CreateTyped(source, width, height);
            var directBy90Left = Image.CreateTyped(by90, height, width);
            var directBy180Left = Image.CreateTyped(by180, width, height);
            var directBy270Left = Image.CreateTyped(by270, height, width);

            var by90Left = image.Rotate(RotateBy.Deg90, RotationDirection.Left);
            var by90Right = image.Rotate(RotateBy.Deg90, RotationDirection.Right);

            var by180Left = image.Rotate(RotateBy.Deg180, RotationDirection.Left);
            var by180Right = image.Rotate(RotateBy.Deg180, RotationDirection.Right);

            var by270Left = image.Rotate(RotateBy.Deg270, RotationDirection.Left);
            var by270Right = image.Rotate(RotateBy.Deg270, RotationDirection.Right);

            Assert.Multiple(
                () =>
                {
                    Assert.IsFalse(image.Equals(by90Left), "Source != Left90");
                    Assert.IsFalse(image.Equals(by90Right), "Source != Right90");

                    Assert.IsFalse(image.Equals(by180Left), "Source != Left180");
                    Assert.IsFalse(image.Equals(by180Right), "Source != Right180");

                    Assert.IsFalse(image.Equals(by270Left), "Source != Left270");
                    Assert.IsFalse(image.Equals(by270Right), "Source != Right270");

                    Assert.IsTrue(by90Left.Equals(by270Right), "Left90 == Right270");
                    Assert.IsTrue(by180Left.Equals(by180Right), "Left180 == Right180");
                    Assert.IsTrue(by270Left.Equals(by90Right), "Left270 == Right90");
                    
                    Assert.IsTrue(by90Left.Equals(directBy90Left), "Left90 == ByHandLeft90");
                    Assert.IsTrue(by270Right.Equals(directBy90Left), "Right270 ==  ByHandLeft90");
                    
                    Assert.IsTrue(by180Left.Equals(directBy180Left), "Left180 == ByHandLeft180");
                    Assert.IsTrue(by180Right.Equals(directBy180Left), "Right180 == ByHandRight180");
                    
                    Assert.IsTrue(by270Left.Equals(directBy270Left), "Left270 == ByHandLeft270");
                    Assert.IsTrue(by90Right.Equals(directBy270Left), "Right90 == ByHandLeft270");
                }
            );
        }
        
        [Test]
        public void Test_Rotation_Direct_Double()
        {
            const int width = 3;
            const int height = 4;
            ReadOnlySpan<double> source = stackalloc double[]
            {
                1, 2, 3, 
                4, 5, 6, 
                7, 8, 9, 
                0, 1, 2,
            };
            ReadOnlySpan<double> by90 = stackalloc double[]
            {
                3, 6, 9, 2, 
                2, 5, 8, 1, 
                1, 4, 7, 0,
            };

            ReadOnlySpan<double> by180 = stackalloc double[]
            {
                2, 1, 0,
                9, 8, 7,
                6, 5, 4,
                3, 2, 1,
            };

            ReadOnlySpan<double> by270 = stackalloc double[]
            {
                0, 7, 4, 1,
                1, 8, 5, 2,
                2, 9, 6, 3,
            };

            var image = Image.CreateTyped(source, width, height);
            var directBy90Left = Image.CreateTyped(by90, height, width);
            var directBy180Left = Image.CreateTyped(by180, width, height);
            var directBy270Left = Image.CreateTyped(by270, height, width);

            var by90Left = image.Rotate(RotateBy.Deg90, RotationDirection.Left);
            var by90Right = image.Rotate(RotateBy.Deg90, RotationDirection.Right);

            var by180Left = image.Rotate(RotateBy.Deg180, RotationDirection.Left);
            var by180Right = image.Rotate(RotateBy.Deg180, RotationDirection.Right);

            var by270Left = image.Rotate(RotateBy.Deg270, RotationDirection.Left);
            var by270Right = image.Rotate(RotateBy.Deg270, RotationDirection.Right);

            Assert.Multiple(
                () =>
                {
                    Assert.IsFalse(image.Equals(by90Left), "Source != Left90");
                    Assert.IsFalse(image.Equals(by90Right), "Source != Right90");

                    Assert.IsFalse(image.Equals(by180Left), "Source != Left180");
                    Assert.IsFalse(image.Equals(by180Right), "Source != Right180");

                    Assert.IsFalse(image.Equals(by270Left), "Source != Left270");
                    Assert.IsFalse(image.Equals(by270Right), "Source != Right270");

                    Assert.IsTrue(by90Left.Equals(by270Right), "Left90 == Right270");
                    Assert.IsTrue(by180Left.Equals(by180Right), "Left180 == Right180");
                    Assert.IsTrue(by270Left.Equals(by90Right), "Left270 == Right90");
                    
                    Assert.IsTrue(by90Left.Equals(directBy90Left), "Left90 == ByHandLeft90");
                    Assert.IsTrue(by270Right.Equals(directBy90Left), "Right270 ==  ByHandLeft90");
                    
                    Assert.IsTrue(by180Left.Equals(directBy180Left), "Left180 == ByHandLeft180");
                    Assert.IsTrue(by180Right.Equals(directBy180Left), "Right180 == ByHandRight180");
                    
                    Assert.IsTrue(by270Left.Equals(directBy270Left), "Left270 == ByHandLeft270");
                    Assert.IsTrue(by90Right.Equals(directBy270Left), "Right90 == ByHandLeft270");
                }
            );
        }

         [Test]
        public void Test_Rotation_Direct_SByte()
        {
            const int width = 3;
            const int height = 4;
            ReadOnlySpan<sbyte> source = stackalloc sbyte[]
            {
                1, 2, 3, 
                4, 5, 6, 
                7, 8, 9, 
                0, 1, 2,
            };
            ReadOnlySpan<sbyte> by90 = stackalloc sbyte[]
            {
                3, 6, 9, 2, 
                2, 5, 8, 1, 
                1, 4, 7, 0,
            };

            ReadOnlySpan<sbyte> by180 = stackalloc sbyte[]
            {
                2, 1, 0,
                9, 8, 7,
                6, 5, 4,
                3, 2, 1,
            };

            ReadOnlySpan<sbyte> by270 = stackalloc sbyte[]
            {
                0, 7, 4, 1,
                1, 8, 5, 2,
                2, 9, 6, 3,
            };

            var image = Image.CreateTyped(source, width, height);
            var directBy90Left = Image.CreateTyped(by90, height, width);
            var directBy180Left = Image.CreateTyped(by180, width, height);
            var directBy270Left = Image.CreateTyped(by270, height, width);

            var by90Left = image.Rotate(RotateBy.Deg90, RotationDirection.Left);
            var by90Right = image.Rotate(RotateBy.Deg90, RotationDirection.Right);

            var by180Left = image.Rotate(RotateBy.Deg180, RotationDirection.Left);
            var by180Right = image.Rotate(RotateBy.Deg180, RotationDirection.Right);

            var by270Left = image.Rotate(RotateBy.Deg270, RotationDirection.Left);
            var by270Right = image.Rotate(RotateBy.Deg270, RotationDirection.Right);

            Assert.Multiple(
                () =>
                {
                    Assert.IsFalse(image.Equals(by90Left), "Source != Left90");
                    Assert.IsFalse(image.Equals(by90Right), "Source != Right90");

                    Assert.IsFalse(image.Equals(by180Left), "Source != Left180");
                    Assert.IsFalse(image.Equals(by180Right), "Source != Right180");

                    Assert.IsFalse(image.Equals(by270Left), "Source != Left270");
                    Assert.IsFalse(image.Equals(by270Right), "Source != Right270");

                    Assert.IsTrue(by90Left.Equals(by270Right), "Left90 == Right270");
                    Assert.IsTrue(by180Left.Equals(by180Right), "Left180 == Right180");
                    Assert.IsTrue(by270Left.Equals(by90Right), "Left270 == Right90");
                    
                    Assert.IsTrue(by90Left.Equals(directBy90Left), "Left90 == ByHandLeft90");
                    Assert.IsTrue(by270Right.Equals(directBy90Left), "Right270 ==  ByHandLeft90");
                    
                    Assert.IsTrue(by180Left.Equals(directBy180Left), "Left180 == ByHandLeft180");
                    Assert.IsTrue(by180Right.Equals(directBy180Left), "Right180 == ByHandRight180");
                    
                    Assert.IsTrue(by270Left.Equals(directBy270Left), "Left270 == ByHandLeft270");
                    Assert.IsTrue(by90Right.Equals(directBy270Left), "Right90 == ByHandLeft270");
                }
            );
        }
        
        [Test]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.RotationSource))]
        public void Test_Rotation(
            int width,
            int height,
            TypeCode typeCode, 
            RotateBy leftRot,
            RotateBy rightRot
        )
        {
            var type = Type.GetType($"System.{typeCode}")!;

            var array = Array.CreateInstance(type, width * height);

            for (var i = 0; i < width * height; i++)
            {
                array.SetValue(Convert.ChangeType(i % 128, type), i);
            }

            var image = new Image(array, width, height, copy: true);

            var left = image.Rotate(leftRot, RotationDirection.Left);
            var right = image.Rotate(rightRot, RotationDirection.Right);
            Assert.IsTrue(left.Equals(right, FloatingPointComparisonType.Exact));
        }

        [Test]
        [TestCaseSource(typeof(DipolImageTests_DataProvider), nameof(DipolImageTests_DataProvider.RotationRepSource))]
        public void Test_Rotation_Rep(
            int width,
            int height,
            TypeCode typeCode,
            RotateBy rotation,
            RotationDirection direction,
            int nRep
        )
        {
            var type = Type.GetType($"System.{typeCode}") ?? throw new ArgumentException(nameof(typeCode));

            var array = Array.CreateInstance(type!, width * height);

            for (var i = 0; i < width * height; i++)
            {
                array.SetValue(Convert.ChangeType(i % 128, type), i);
            }

            ImageBase image = new Image(array, width, height, copy: true);
            var otherImage = image;
            for (var i = 0; i < nRep; i++)
            {
                otherImage = otherImage.Rotate(rotation, direction);
            }

            Assert.IsTrue(image.Equals(otherImage, FloatingPointComparisonType.Exact));
        }
    }
}
