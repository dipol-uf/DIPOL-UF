using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ANDOR_CS.Classes;
using ANDOR_CS.Enums;
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
        public void CameraTests_InitializationEnsuredTest()
        {
            Assert.IsNotNull(Camera);
            Assert.IsTrue(Camera.IsInitialized);
            Assert.AreEqual(Camera.CameraIndex, 0);
            Assert.AreNotEqual(Camera.Capabilities, default(DeviceCapabilities));
            Assert.AreNotEqual(Camera.Properties, default(CameraProperties));
            Assert.IsFalse(String.IsNullOrWhiteSpace(Camera.CameraModel));
            Assert.IsFalse(String.IsNullOrWhiteSpace(Camera.SerialNumber));

        }

        [TestMethod]
        public void CameraTests_StatusPollingTest()
        {
            if (Camera.Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
            {
                var tempStatus = Camera.GetCurrentTemperature();
                Assert.AreEqual(tempStatus.Status, TemperatureStatus.Off);
            }

            Assert.AreEqual(Camera.GetStatus(), CameraStatus.Idle);
        }

        [TestMethod]
        public void CameraTests_TemperatureMonitor()
        {
            if (!Camera.Capabilities.GetFunctions.HasFlag(GetFunction.Temperature))
                return;

            Assert.IsFalse(Camera.IsTemperatureMonitored);
            Camera.TemperatureMonitor(Switch.Enabled, 100);
            Assert.IsTrue(Camera.IsTemperatureMonitored);
            System.Threading.SpinWait.SpinUntil(() => false, 1000);
            Camera.TemperatureMonitor(Switch.Disabled);

            Assert.IsFalse(Camera.IsTemperatureMonitored);
        }
    }
}
