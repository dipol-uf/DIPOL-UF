using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using DipolImage;

namespace Tests
{
    [TestClass]
    public class DipolImageTests
    {
        public Random R;

        [TestInitialize]
        public void Test_Initialize()
        {
            R = new Random();
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
            var bytes = BitConverter.GetBytes(value);

            var image = new Image(bytes, 1, 1, TypeCode.UInt16);

            Assert.AreEqual(value, image.Get<ushort>(0, 0));
        }

        [TestMethod]
        public void Test_GetBytes()
        {
            var initArray = new ushort[] {1, 123};
            var image = new Image(initArray, 2, 1);

            var bytes = image.GetBytes();

            var reconstructed = BitConverter.ToUInt16(bytes, 2);

            Assert.AreEqual(initArray[1], reconstructed);

        }
    }
}
