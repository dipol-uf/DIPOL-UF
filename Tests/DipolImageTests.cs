using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using DipolImage;

namespace Tests
{
    [TestClass]
    public class DipolImageTests
    {
        public Random R;
        public int[] TestArray;
        public byte[] TestByteArray;
        [TestInitialize]
        public void Test_Initialize()
        {
            R = new Random();
            TestArray = new int[32];
            const int stride = sizeof(int);
            TestByteArray= new byte[TestArray.Length * stride];
            for (var i = 0; i < TestArray.Length; i++)
            {
                TestArray[i] = R.Next();

            }
        }

        [TestMethod]
        public void Test_ConstructorThrows()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new Image(null, 2, 3));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Image(TestArray, 0, 3));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Image(TestArray, 10, 0));
            Assert.ThrowsException<ArgumentException>(() => new Image(TestByteArray, 1, 1));

            Assert.ThrowsException<ArgumentNullException>(() => new Image(null, 1, 1, TypeCode.Int16));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Image(TestByteArray, 0, 3, TypeCode.Int32));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Image(TestByteArray, 10, 0, TypeCode.Int32));
            Assert.ThrowsException<ArgumentException>(() => new Image(TestByteArray, 1, 1, TypeCode.Byte));
            Assert.ThrowsException<ArgumentException>(() => new Image(TestByteArray, 1, 1, (TypeCode) 45500));

        }

        [TestMethod]
        public void Test_ImageEqualsToArray()
        {
            var initArray = new[] {1, 2, 3, 4, 5, 6};

            var image = new Image(initArray, 2, 3);

            Assert.IsTrue(
                initArray[0] == image.Get<int>(0, 0) &&
                initArray[1] == image.Get<int>(0, 1) &&
                initArray[2] == image.Get<int>(1, 0) &&
                initArray[3] == image.Get<int>(1, 1) &&
                initArray[4] == image.Get<int>(2, 0) &&
                initArray[5] == image.Get<int>(2, 1));
        }

        [TestMethod]
        public void Test_ImageInitializedFromBytes()
        {
            const ushort value = 23;

            foreach (var code in Image.AllowedPixelTypes)
            {
                var temp = (Convert.ChangeType(value, code)) ?? new object();
                var mi = typeof(BitConverter)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == "GetBytes" &&
                                m.GetParameters().Length == 1 &&
                                m.GetParameters().First().ParameterType == temp.GetType());
                var bytes = (byte[])mi.Invoke(null, new [] {temp});

                var image = new Image(bytes, 1, 1, code);

                Assert.AreEqual(temp, image[0,0]);
                Assert.AreEqual(image.UnderlyingType, code);
            }

        }

        [TestMethod]
        public void Test_GetBytes()
        {
            const int val1 = 1;
            const int val2 = 123;
            foreach (var code in Image.AllowedPixelTypes)
            {
                var type = Type.GetType("System." + code) ?? typeof(void);
                var initArray = Array.CreateInstance(type, 2);
                initArray.SetValue(Convert.ChangeType(val1, code), 0);
                initArray.SetValue(Convert.ChangeType(val2, code), 1);


                var image = new Image(initArray, 2, 1);

                var bytes = image.GetBytes();

                var mi = typeof(BitConverter)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == "GetBytes" &&
                                m.GetParameters().Length == 1 &&
                                m.GetParameters().First().ParameterType == type);
                var size = System.Runtime.InteropServices.Marshal.SizeOf(type);
                var reconstructed = new byte[2 * size];
                Array.Copy((byte[])mi.Invoke(null, new object[]{ initArray.GetValue(0)}), 0, reconstructed, 0, size);
                Array.Copy((byte[])mi.Invoke(null, new object[] { initArray.GetValue(1) }), 0, reconstructed, size, size);

                CollectionAssert.AreEqual(reconstructed, bytes);
            }
        }

        [TestMethod]
        public void Test_Equals()
        {
            var tempArr = new byte[TestByteArray.Length];
            Array.Copy(TestByteArray, tempArr, tempArr.Length);
            tempArr[0] = (byte) (tempArr[0] == 0 ? 127 : 0);

            foreach (var code in Image.AllowedPixelTypes)
            {
                var image1 = new Image(TestByteArray, 2, 2, code);
                var image2 = new Image(TestByteArray, 2, 2, code);

                var wrImage1 = new Image(TestByteArray, 2, 1, code);
                var wrImage2 = new Image(TestByteArray, 1, 2, code);
                var wrImage3 = new Image(TestByteArray, 2, 2,
                    code == TypeCode.Int16 ? TypeCode.UInt16 : TypeCode.Int16);
                var wrImage4 = new Image(tempArr, 2, 2, code);

                Assert.IsTrue(image1.Equals(image2));
                Assert.IsTrue(image2.Equals(image1));
                Assert.IsTrue(image1.Equals((object) image2));
                Assert.IsTrue(image1.Equals(image1, image2));

                Assert.IsFalse(image1.Equals(null));
                Assert.IsFalse(image1.Equals(wrImage1));
                Assert.IsFalse(image1.Equals(wrImage2));
                Assert.IsFalse(image1.Equals(wrImage3));
                Assert.IsFalse(image1.Equals(wrImage4));
                Assert.IsFalse(image1.Equals((object) null));
                Assert.IsFalse(image1.Equals(image1, wrImage1));
                Assert.IsFalse(image1.Equals(image1, null));
                Assert.IsFalse(image1.Equals(null, image1));

            }
        }

        [TestMethod]
        public void Test_Copy()
        {
            var array = new byte[1024];
            R.NextBytes(array);

            var img = new Image(array, 32, 16, TypeCode.Int16);

            Assert.IsTrue(img.Equals(img.Copy()));
        }

        [TestMethod]
        public void Test_ThisAccessor()
        {
            var initArray = new[] {1, 2, 3, 4};
            var image = new Image(initArray, 2, 2);

            Assert.AreEqual(initArray[1], image[0, 1]);
            Assert.AreEqual(initArray[2], image[1, 0]);

            image[0, 0] = 430;

            Assert.AreEqual(430, image[0,0]);
        }

        [TestMethod]
        public void Test_GetHashCode()
        {
            var tempArr = new byte[TestByteArray.Length];
            Array.Copy(TestByteArray, tempArr, tempArr.Length);
            tempArr[0] = (byte)(tempArr[0] == 0 ? 127 : 0);

            foreach (var code in Image.AllowedPixelTypes)
            {
                var image1 = new Image(TestByteArray, 2, 2, code);
                var image2 = new Image(TestByteArray, 2, 2, code);

                var wrImage1 = new Image(tempArr, 2, 2, code);

                Assert.AreEqual(image1.GetHashCode(), image2.GetHashCode());
                Assert.AreEqual(image1.GetHashCode(image1), image1.GetHashCode(image2));
                Assert.AreNotEqual(image1.GetHashCode(), wrImage1.GetHashCode());
                Assert.AreNotEqual(image1.GetHashCode(image1), image1.GetHashCode(wrImage1));

            }
        }

        [TestMethod]
        public void Test_Max()
        {
            foreach (var code in Image.AllowedPixelTypes)
            {
                var type = Type.GetType("System." + code) ?? typeof(byte);
                var size = System.Runtime.InteropServices.Marshal.SizeOf(type);
                var buffer = new byte[size];

                var val = Activator.CreateInstance(type);

                var image = new Image(TestByteArray, TestByteArray.Length / size, 1, code);

                for(var i = 0; i < image.Width; i++)
                    if ((image[0, i] is IComparable comp) && comp.CompareTo(val) > 0)
                        val = comp;

                val = Convert.ChangeType(val, code);

                Assert.AreEqual(val, image.Max());
            }
        }

        [TestMethod]
        public void Test_Min()
        {
            foreach (var code in Image.AllowedPixelTypes)
            {
                var type = Type.GetType("System." + code) ?? typeof(byte);
                var size = System.Runtime.InteropServices.Marshal.SizeOf(type);
                var buffer = new byte[size];

                var val = Activator.CreateInstance(type);

                var image = new Image(TestByteArray, TestByteArray.Length / size, 1, code);

                for (var i = 0; i < image.Width; i++)
                    if ((image[0, i] is IComparable comp) && comp.CompareTo(val) < 0)
                        val = comp;

                val = Convert.ChangeType(val, code);

                Assert.AreEqual(val, image.Min());
            }
        }
    }
}
