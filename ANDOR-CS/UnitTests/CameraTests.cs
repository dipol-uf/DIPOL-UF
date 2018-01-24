using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ANDOR_CS.Classes;
using ANDOR_CS.DataStructures;

namespace ANDOR_CS.UnitTests
{
    [TestClass]
    public class CameraTests
    {
        public CameraBase Camera;

        [TestInitialize]
        public void CameraTests_Initialize()
        {
            Camera = new Camera();
        }

        [TestCleanup]
        public void CameraTests_Cleanup()
        {
            Camera?.Dispose();
            Camera = null;
        }

        [TestMethod]
        public void InitializationEnsured()
        {
            Assert.IsNotNull(Camera);
            Assert.IsTrue(Camera.IsInitialized);
            Assert.AreEqual(Camera.CameraIndex, 0);
            Assert.AreNotEqual(Camera.Capabilities, default(DeviceCapabilities));
        }
    }
}
